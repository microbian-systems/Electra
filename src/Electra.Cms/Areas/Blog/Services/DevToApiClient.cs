using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Electra.Cms.Areas.Blog.Services;


public interface IDevToApiClient : IForemApiClientBase; 

public sealed class DevToApiClient(HttpClient httpClient, ILogger<DevToApiClient> log) 
    : ForemApiClientBase(httpClient, log), IDevToApiClient ;

public static class DevToApiExtensions
{
    public static IServiceCollection AddDevToApiClient(this IServiceCollection services, IConfiguration config)
    {
        // Configure options
        services.Configure<DevToApiOptions>(config.GetSection(DevToApiOptions.SectionName));
        
        // Register the delegating handler
        services.AddTransient<DevToApiHandler>();
        
        // Configure HttpClient with resilience
        services.AddHttpClient<IDevToApiClient, DevToApiClient>(client =>
        {
            // Basic client configuration - detailed config handled in the handler
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<DevToApiHandler>()
        .AddStandardResilienceHandler(options =>
        {
            // Configure retry policy
            options.Retry.MaxRetryAttempts = 5;
            options.Retry.Delay = TimeSpan.FromSeconds(1);
            options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
            
            // Configure circuit breaker
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 10;
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            
            // Configure timeout
            options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}