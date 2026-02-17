using Microsoft.Extensions.DependencyInjection;
using ThrowGuard;

namespace Aero.Social.Forem;

public static class ForemExtensions
{
    public static IServiceCollection AddForem(this IServiceCollection services, string url = "https://dev.to")
    {
        var apiKey = Environment.GetEnvironmentVariable("FOREM_API_KEY");
        Throw.IfNullOrEmpty(apiKey, "FOREM_API_KEY cannot be null or empty.");
            
        services.AddHttpClient<ForemArticleService>(client =>
            {
                client.BaseAddress = new Uri(url); // or your Forem instance
                client.DefaultRequestHeaders.Add("Accept", "application/json");
                client.DefaultRequestHeaders.Add("api-key", apiKey);
            }).AddHttpMessageHandler<ForemApiKeyHandler>();

        return services;
    }
}