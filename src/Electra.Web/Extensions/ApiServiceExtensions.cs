using Electra.Common.Web.Infrastructure;
using Electra.Common.Web.Jwt;
using Electra.Persistence;

namespace Electra.Common.Web.Extensions;

public static class ApiServiceExtensions
{
    public static WebApplicationBuilder AddDefaultApiServices(this WebApplicationBuilder builder)
    {
        builder.Services
            .AddDefaultApiServices(builder.Configuration);
        return builder;
    }
    
    public static IServiceCollection AddDefaultApiServices(this IServiceCollection services, IConfiguration config)
    {
        services.AddSwaggerEndpoint(config);
        services.AddTransient<IJwtFactory, JwtFactory>();
        services.AddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        services.AddScoped<IApiAuthRepository, ApiAuthRepository>();
        
        return services;
    }

    public static WebApplication UseDefaultApi(this WebApplication app)
    {
        var env = app.Environment;
        app.UseSwaggerEx();
        return app;
    }
}