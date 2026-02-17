# Aero.Core.Ai

AI integration with Microsoft SemanticKernel for the Aero framework.

## Overview

`Aero.Core.Ai` provides integration with Large Language Models (LLMs) using Microsoft's SemanticKernel. It enables AI-powered features, prompt engineering, and AI usage tracking.

## Key Components

### AI Provider

Abstraction over different LLM providers:

```csharp
public enum AiProvider
{
    OpenAI,
    AzureOpenAI,
    Anthropic,
    Ollama,
    Custom
}

public class AiProvider
{
    public AiProviderType Type { get; set; }
    public string Endpoint { get; set; }
    public string ApiKey { get; set; }
    public string Model { get; set; }
    public Dictionary<string, object> Options { get; set; }
}
```

### SmartKernelFactory

Factory for creating SemanticKernel instances:

```csharp
public class SmartKernelFactory
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SmartKernelFactory> _logger;

    public SmartKernelFactory(
        IServiceProvider serviceProvider,
        ILogger<SmartKernelFactory> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public Kernel CreateKernel(AiProvider provider)
    {
        var builder = Kernel.CreateBuilder();

        switch (provider.Type)
        {
            case AiProviderType.OpenAI:
                builder.AddOpenAIChatCompletion(
                    provider.Model,
                    provider.ApiKey);
                break;

            case AiProviderType.AzureOpenAI:
                builder.AddAzureOpenAIChatCompletion(
                    provider.Model,
                    provider.Endpoint,
                    provider.ApiKey);
                break;

            case AiProviderType.Ollama:
                builder.AddOllamaChatCompletion(
                    provider.Model,
                    new Uri(provider.Endpoint));
                break;
        }

        // Add plugins
        builder.Plugins.AddFromType<SearchPlugin>();
        builder.Plugins.AddFromType<MathPlugin>();

        return builder.Build();
    }
}
```

### User Settings

Per-user AI configuration:

```csharp
public class UserAiSettings
{
    public string UserId { get; set; }
    public AiProvider PreferredProvider { get; set; }
    public string DefaultModel { get; set; }
    public decimal? MaxMonthlyBudget { get; set; }
    public int? MaxTokensPerRequest { get; set; }
    public Dictionary<string, object> Preferences { get; set; }
    
    // Usage tracking
    public decimal CurrentMonthSpend { get; set; }
    public int CurrentMonthRequests { get; set; }
}
```

## Usage

### Basic Chat Completion

```csharp
public class AiService
{
    private readonly SmartKernelFactory _kernelFactory;
    private readonly IAiUsageRepository _usageRepository;

    public async Task<string> GetChatCompletionAsync(
        string userId,
        string prompt,
        string? systemMessage = null)
    {
        // Get user's preferred provider
        var settings = await GetUserSettingsAsync(userId);
        
        // Check budget
        if (settings.MaxMonthlyBudget.HasValue && 
            settings.CurrentMonthSpend >= settings.MaxMonthlyBudget.Value)
        {
            throw new InvalidOperationException("Monthly AI budget exceeded");
        }

        // Create kernel
        var kernel = _kernelFactory.CreateKernel(settings.PreferredProvider);

        // Configure chat
        var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
        var chatHistory = new ChatHistory();

        if (!string.IsNullOrEmpty(systemMessage))
        {
            chatHistory.AddSystemMessage(systemMessage);
        }

        chatHistory.AddUserMessage(prompt);

        // Get response
        var response = await chatCompletionService.GetChatMessageContentAsync(
            chatHistory,
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = settings.MaxTokensPerRequest ?? 2000,
                Temperature = 0.7
            });

        // Log usage
        await LogUsageAsync(userId, settings.PreferredProvider, prompt, response.Content!);

        return response.Content!;
    }
}
```

### Streaming Responses

```csharp
public async IAsyncEnumerable<string> StreamChatCompletionAsync(
    string userId,
    string prompt,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    var settings = await GetUserSettingsAsync(userId);
    var kernel = _kernelFactory.CreateKernel(settings.PreferredProvider);
    var chatService = kernel.GetRequiredService<IChatCompletionService>();

    var chatHistory = new ChatHistory();
    chatHistory.AddUserMessage(prompt);

    await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(
        chatHistory,
        cancellationToken: cancellationToken))
    {
        if (!string.IsNullOrEmpty(chunk.Content))
        {
            yield return chunk.Content;
        }
    }
}
```

### Using Plugins

```csharp
// Define a plugin
public class SearchPlugin
{
    private readonly ISearchService _searchService;

    [KernelFunction("search")]
    [Description("Search for information on the web")]
    public async Task<string> SearchAsync(
        [Description("The search query")] string query)
    {
        var results = await _searchService.SearchAsync(query);
        return string.Join("\n", results.Select(r => $"- {r.Title}: {r.Snippet}"));
    }
}

// Use in conversation
public async Task<string> ResearchTopicAsync(string topic)
{
    var kernel = _kernelFactory.CreateKernel(_defaultProvider);
    
    var planner = new FunctionCallingStepwisePlanner(
        new FunctionCallingStepwisePlannerOptions
        {
            MaxIterations = 5
        });

    var result = await planner.ExecuteAsync(
        kernel,
        $"Research the topic '{topic}' and provide a comprehensive summary");

    return result.FinalAnswer;
}
```

### Prompt Templates

```csharp
public class PromptTemplates
{
    public const string Summarize = """
        Summarize the following text in a clear and concise manner:
        
        {{$input}}
        
        Summary:
        """;

    public const string AnalyzeCode = """
        Analyze the following code and provide:
        1. A brief explanation of what it does
        2. Potential issues or bugs
        3. Suggestions for improvement
        
        ```{{$language}}
        {{$code}}
        ```
        """;

    public const string GenerateDocumentation = """
        Generate XML documentation for the following C# method:
        
        {{$method}}
        
        Provide only the documentation comments, no explanations.
        """;
}

// Usage
public async Task<string> SummarizeTextAsync(string text)
{
    var kernel = _kernelFactory.CreateKernel(_defaultProvider);
    
    var promptTemplate = kernel.CreateFunctionFromPrompt(
        PromptTemplates.Summarize,
        new OpenAIPromptExecutionSettings { MaxTokens = 500 });

    var result = await kernel.InvokeAsync(promptTemplate, new KernelArguments
    {
        ["input"] = text
    });

    return result.GetValue<string>()!;
}
```

## Usage Tracking

### AI Usage Log Entity

```csharp
public class AiUsageLog : Entity
{
    public string UserId { get; set; }
    public AiProvider Provider { get; set; }
    public string Model { get; set; }
    public string Operation { get; set; }
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Duration { get; set; }
    public string? PromptPreview { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Usage Repository

```csharp
public interface IAiUsageRepository : IGenericRepository<AiUsageLog>
{
    Task<UsageSummary> GetUserUsageSummaryAsync(string userId, DateTime month);
    Task<UsageSummary> GetGlobalUsageSummaryAsync(DateTime month);
    Task<IEnumerable<AiUsageLog>> GetRecentUsageAsync(string userId, int count);
}

public class AiUsageRepository : GenericEntityFrameworkRepository<AiUsageLog>, IAiUsageRepository
{
    public async Task<UsageSummary> GetUserUsageSummaryAsync(string userId, DateTime month)
    {
        var startOfMonth = new DateTime(month.Year, month.Month, 1);
        var endOfMonth = startOfMonth.AddMonths(1);

        var usage = await Context.AiUsageLogs
            .Where(u => u.UserId == userId)
            .Where(u => u.CreatedOn >= startOfMonth && u.CreatedOn < endOfMonth)
            .GroupBy(_ => 1)
            .Select(g => new UsageSummary
            {
                TotalRequests = g.Count(),
                TotalTokens = g.Sum(u => u.InputTokens + u.OutputTokens),
                TotalCost = g.Sum(u => u.Cost),
                SuccessfulRequests = g.Count(u => u.Success)
            })
            .FirstOrDefaultAsync();

        return usage ?? new UsageSummary();
    }
}
```

## Configuration

### appsettings.json

```json
{
  "Ai": {
    "DefaultProvider": "OpenAI",
    "Providers": {
      "OpenAI": {
        "ApiKey": "sk-...",
        "Model": "gpt-4"
      },
      "AzureOpenAI": {
        "Endpoint": "https://your-resource.openai.azure.com",
        "ApiKey": "...",
        "Model": "gpt-4"
      },
      "Ollama": {
        "Endpoint": "http://localhost:11434",
        "Model": "llama2"
      }
    },
    "Limits": {
      "MaxTokensPerRequest": 4000,
      "DefaultMonthlyBudget": 50.00
    }
  }
}
```

### Dependency Injection

```csharp
builder.Services.AddAiServices(builder.Configuration);

public static class AiServiceExtensions
{
    public static IServiceCollection AddAiServices(
        this IServiceCollection services,
        IConfiguration config)
    {
        services.Configure<AiSettings>(config.GetSection("Ai"));
        
        services.AddSingleton<SmartKernelFactory>();
        services.AddScoped<AiService>();
        services.AddScoped<IAiUsageRepository, AiUsageRepository>();

        return services;
    }
}
```

## Best Practices

1. **Track Usage** - Always log AI usage for cost monitoring
2. **Set Budgets** - Implement per-user spending limits
3. **Handle Failures** - Gracefully handle API errors and timeouts
4. **Cache Results** - Cache common AI responses when appropriate
5. **Stream Responses** - Use streaming for better UX with long responses
6. **Validate Inputs** - Sanitize prompts to prevent prompt injection
7. **Rate Limit** - Apply rate limiting to AI endpoints

## Related Packages

- `Aero.Core` - Entity definitions
- `Aero.EfCore` - Usage log storage
- `Aero.Events` - AI event publishing
