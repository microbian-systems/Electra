using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Electra.Common.Web.Middleware;

public record RateLimitOptions
{
    public const string MyRateLimit = "MyRateLimit";
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
    public static WebApplicationBuilder UseRateLimiter(this WebApplicationBuilder app)
    {
        //app.UseRateLimiter();
        var _ = AddRateLimiter(app.Services, app.Configuration);
        return app;
    }

    public static IServiceCollection AddRateLimiter(this IServiceCollection services, IConfiguration config)
    {
        services.Configure<RateLimitOptions>(
            config.GetSection(RateLimitOptions.MyRateLimit));

        var myOptions = new RateLimitOptions();
        config.GetSection(RateLimitOptions.MyRateLimit).Bind(myOptions);
        var fixedPolicy = "fixed";

        services.AddRateLimiter(_ => _
            .AddFixedWindowLimiter(policyName: fixedPolicy, options =>
            {
                options.PermitLimit = myOptions.PermitLimit;
                options.Window = TimeSpan.FromSeconds(myOptions.Window);
                options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
                options.QueueLimit = myOptions.QueueLimit;
            }));
        return services;
    }
}