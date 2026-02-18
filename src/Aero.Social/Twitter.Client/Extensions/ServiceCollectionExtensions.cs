using Microsoft.Extensions.DependencyInjection;
using Polly;

namespace Aero.Social.Twitter.Client.Extensions;

/// <summary>
/// Extension methods for registering Twitter client services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Twitter client services to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Action to configure the client options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTwitterClient(
        this IServiceCollection services,
        Action<Configuration.TwitterClientOptions> configureOptions)
    {
        services.Configure(configureOptions);

        services.AddHttpClient<Clients.ITwitterClient, Clients.TwitterClient>("TwitterClient")
            .AddStandardResilienceHandler(options =>
            {
                // Retry configuration
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.Delay = TimeSpan.FromSeconds(1);
                options.Retry.BackoffType = DelayBackoffType.Exponential;

                // Circuit breaker configuration
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(60);
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);

                // Timeout configuration
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(30);
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
            });

        return services;
    }
}