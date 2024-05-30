using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Electra.Common.Web.Middleware;

public enum RateLimitType
{
    Fixed,
    None,
    Sliding
}
public record RateLimitOptions
{
    public const string SectionName = "RateLimitOptions";
    public RateLimitType LimitType { get; init; } = RateLimitType.Fixed;
    public int PermitLimit { get; init; } = 100;
    public int Window { get; init; } = 10;
    public int ReplenishmentPeriod { get; init; } = 2;
    public int QueueLimit { get; init; } = 2;
    public int SegmentsPerWindow { get; init; } = 8;
    public int TokenLimit { get; init; } = 10;
    public int TokenLimit2 { get; init; } = 20;
    public int TokensPerPeriod { get; init; } = 4;
    public bool AutoReplenishment { get; init; } = false;
}

// todo - implement sliding window rate limiter
// https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-8.0
public static class RateLimiterMiddleware
{
    public static IServiceCollection AddElectraRateLimiter(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RateLimitOptions>(
            config.GetSection(RateLimitOptions.SectionName));

        var opts = new RateLimitOptions();
        config.GetSection(RateLimitOptions.SectionName).Bind(opts);

        services = opts.LimitType switch
        {
            RateLimitType.Sliding =>
                services.AddRateLimiter(_ => _
                    .AddFixedWindowLimiter(policyName: nameof(opts.LimitType), options =>
                    {
                        options.PermitLimit = opts.PermitLimit;
                        options.Window = TimeSpan.FromSeconds(opts.Window);
                        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        options.QueueLimit = opts.QueueLimit;
                    })),
            RateLimitType.Fixed =>
                services.AddRateLimiter(_ => _
                    .AddSlidingWindowLimiter(policyName: nameof(opts.LimitType), options =>
                    {
                        options.PermitLimit = opts.PermitLimit;
                        options.Window = TimeSpan.FromSeconds(opts.Window);
                        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                        options.QueueLimit = opts.QueueLimit;
                    })),
            _ => services
        };

        return services;
    }
}