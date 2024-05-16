using Electra.Common.Web.Infrastructure;
using Electra.Common.Web.Jwt;
using Electra.Persistence;

namespace Electra.Common.Web.Extensions;

public static class ApiServiceExtensions
{
    public static WebApplicationBuilder AddDefaultApiServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddDefaultApiServices();
        return builder;
    }
    
    public static IServiceCollection AddDefaultApiServices(this IServiceCollection services)
    {
        services.AddTransient<IJwtFactory, JwtFactory>();
        services.AddTransient<IClaimsPrincipalFactory, ClaimsPrincipalFactory>();
        services.AddScoped<IApiAuthRepository, ApiAuthRepository>();
        
        return services;
    }
}