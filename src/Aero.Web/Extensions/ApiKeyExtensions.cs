using Aero.Common.Web.Infrastructure;
using Aero.Common.Web.Services;

namespace Aero.Common.Web.Extensions;

public static class ApiKeyExtensions
{
    public static WebApplicationBuilder AddApiKeyGenerator(this WebApplicationBuilder builder)
    {
        var config = builder.Configuration;
        builder.Services.AddApiKeyGenerator(config);
        return builder;
    }
    
    public static IServiceCollection AddApiKeyGenerator(
        this IServiceCollection services,
        IConfiguration config)
    {
        const string apiKeySection = "apiKeyOptions";
        var apiKeyOptions = new ApiKeyOptions();
        config.GetSection(apiKeySection).Bind(apiKeyOptions);
        services.Configure<ApiKeyOptions>(config.GetSection(apiKeySection));
        
        services.AddTransient<IApiKeyService, ApiKeyService>();
        services.AddTransient<IApiKeyFactory, DefaultApiKeyFactory>();
        
        return services;
    }
}