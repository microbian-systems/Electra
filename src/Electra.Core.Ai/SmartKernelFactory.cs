using Electra.Core.Encryption;
using Electra.Models.Entities;
using Electra.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Electra.Core.Ai;

public class SmartKernelFactory
{
    private readonly IConfiguration _config;
    private readonly ElectraDbContext _dbContext;
    private readonly IEncryptor _encryption;
    private readonly ILogger<SmartKernelFactory> _logger;
    private readonly UserSettings userSettings;
    private readonly IElectraUnitOfWork uow;


    public SmartKernelFactory(IElectraUnitOfWork uow, IEncryptor encryptor, IOptionsMonitor<UserSettings> userSettings, ILogger<SmartKernelFactory> log, IConfiguration config, ElectraDbContext dbContext)
    {
        _logger = log;
        this.uow = uow;
        _encryption = encryptor;
        _config = config;
        _dbContext = dbContext;
        userSettings.OnChange(x => _logger.LogInformation("User settings changed"));
        this.userSettings = userSettings.CurrentValue;
    }

    public async Task<Kernel> CreateKernelForUserAsync(long userId)
    {
        var user = await uow.User.FindByIdAsync(userId);
        var settings = user?.UserSettings;

        var builder = Kernel.CreateBuilder();

        // TIER 1: User's Premium Keys (if they want best quality)
        if (settings != null)
        {
            if (settings.PreferredProvider == AiProvider.OpenAI && 
                TryAddOpenAI(builder, settings))
            {
                _logger.LogInformation("Using OpenAI (user's key) for {UserId}", userId);
                return builder.Build();
            }

            if (settings.PreferredProvider == AiProvider.Anthropic && 
                TryAddAnthropic(builder, settings))
            {
                _logger.LogInformation("Using Anthropic (user's key) for {UserId}", userId);
                return builder.Build();
            }

            if (settings.PreferredProvider == AiProvider.DeepSeek && 
                TryAddDeepSeek(builder, settings))
            {
                _logger.LogInformation("Using DeepSeek (user's key) for {UserId}", userId);
                return builder.Build();
            }
        }

        // TIER 2: Groq (FREE - 14,400/day)
        if (await CheckProviderAvailabilityAsync(userId, "Groq", 100))
        {
            AddGroq(builder);
            await LogUsageAsync(userId, "Groq");
            _logger.LogInformation("Using Groq (free) for {UserId}", userId);
            return builder.Build();
        }

        // TIER 3: Google Gemini (FREE - 1,500/day)
        if (await CheckProviderAvailabilityAsync(userId, "Gemini", 50))
        {
            AddGemini(builder);
            await LogUsageAsync(userId, "Gemini");
            _logger.LogInformation("Using Gemini (free) for {UserId}", userId);
            return builder.Build();
        }

        // TIER 4: DeepSeek (VERY CHEAP - $0.002/match)
        if (!string.IsNullOrEmpty(_config["AI:DeepSeek:ApiKey"]))
        {
            AddDeepSeek(builder);
            await LogUsageAsync(userId, "DeepSeek");
            _logger.LogInformation("Using DeepSeek (cheap paid fallback) for {UserId}", userId);
            return builder.Build();
        }

        // TIER 5: OpenRouter free (last resort)
        AddOpenRouterFree(builder);
        await LogUsageAsync(userId, "OpenRouter");
        _logger.LogWarning("Using OpenRouter free tier (all other providers exhausted) for {UserId}", userId);
        return builder.Build();
    }

    private void AddGroq(IKernelBuilder builder)
    {
        builder.AddOpenAIChatCompletion(
            modelId: "llama-3.1-8b-instant",
            apiKey: _config["AI:Groq:ApiKey"] ?? throw new InvalidOperationException("Groq API key missing"),
            endpoint: new Uri("https://api.groq.com/openai/v1"));
    }

    private void AddGemini(IKernelBuilder builder)
    {
        builder.AddGoogleAIGeminiChatCompletion(
            modelId: "gemini-1.5-flash",
            apiKey: _config["AI:Google:ApiKey"] ?? throw new InvalidOperationException("Google API key missing"));
    }

    private void AddDeepSeek(IKernelBuilder builder)
    {
        builder.AddOpenAIChatCompletion(
            modelId: "deepseek-chat",
            apiKey: _config["AI:DeepSeek:ApiKey"] ?? throw new InvalidOperationException("DeepSeek API key missing"),
            endpoint: new Uri("https://api.deepseek.com/v1"));
    }

    private bool TryAddDeepSeek(IKernelBuilder builder, UserSettings settings)
    {
        if (string.IsNullOrEmpty(settings.EncryptedDeepSeekKey))
            return false;

        try
        {
            var apiKey = _encryption.DecryptString(settings.EncryptedDeepSeekKey);
            builder.AddOpenAIChatCompletion(
                modelId: "deepseek-chat",
                apiKey: apiKey,
                endpoint: new Uri("https://api.deepseek.com/v1"));
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure DeepSeek");
            return false;
        }
    }

    private async Task<bool> CheckProviderAvailabilityAsync(long userId, string provider, long dailyLimit)
    {
        var today = DateTime.UtcNow.Date;
        var count = await _dbContext.AiUsageLogs
            .CountAsync(log => 
                log.UserId == userId && 
                log.Provider == provider && 
                log.Timestamp >= today);

        return count < dailyLimit;
    }

    private async Task LogUsageAsync(long userId, string provider)
    {
        _dbContext.AiUsageLogs.Add(new AiUsageLog
        {
            UserId = userId,
            Provider = provider,
            Timestamp = DateTimeOffset.UtcNow
        });
        await _dbContext.SaveChangesAsync();
    }

    private bool TryAddOpenAI(IKernelBuilder builder, UserSettings settings)
    {
        if (string.IsNullOrEmpty(settings.EncryptedOpenAIKey))
            return false;

        try
        {
            var apiKey = _encryption.DecryptString(settings.EncryptedOpenAIKey);
            builder.AddOpenAIChatCompletion(
                modelId: "gpt-4o-mini",
                apiKey: apiKey);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure OpenAI");
            return false;
        }
    }

    private bool TryAddAnthropic(IKernelBuilder builder, UserSettings settings)
    {
        if (string.IsNullOrEmpty(settings.EncryptedAnthropicKey))
            return false;

        try
        {
            var apiKey = _encryption.DecryptString(settings.EncryptedAnthropicKey);
            // Note: Anthropic support would need to be added via appropriate NuGet package
            _logger.LogWarning("Anthropic support not yet implemented");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to configure Anthropic");
            return false;
        }
    }

    private void AddOpenRouterFree(IKernelBuilder builder)
    {
        var apiKey = _config["AI:OpenRouter:ApiKey"];
        if (string.IsNullOrEmpty(apiKey))
            throw new InvalidOperationException("OpenRouter API key missing");

        builder.AddOpenAIChatCompletion(
            modelId: "meta-llama/llama-3.1-8b-instruct:free",
            apiKey: apiKey,
            endpoint: new Uri("https://openrouter.ai/api/v1"));
    }
}