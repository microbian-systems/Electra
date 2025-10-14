using Electra.Persistence;

namespace Electra.Auth.Extensions;

public static class OpenIddictExtensions
{
    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Configure JWT Token settings
        var jwtSettings = configuration.GetSection("JwtSettings");
        // todo - pull openiddict settings from appsettings.json for secret key
        var secretKey = Encoding.ASCII.GetBytes(jwtSettings["SecretKey"]);

        // Add OpenIddict with EF Core stores
        services.AddOpenIddict()
            // Register the OpenIddict core components
            .AddCore(options =>
            {
                // Configure OpenIddict to use the Entity Framework Core stores and models
                options.UseEntityFrameworkCore()
                    .UseDbContext<ElectraDbContext>();
            })

            // Register the OpenIddict server components
            .AddServer(options =>
            {
                // Enable the token endpoint
                options.SetTokenEndpointUris("/connect/token");
                // todo - enable userinfo endpoint uris for openiddict
                //options.SetUserinfoEndpointUris("/connect/userinfo")
                ;

                // Enable the password and refresh token flows
                options.AllowPasswordFlow()
                    .AllowRefreshTokenFlow();

                // Accept anonymous clients (i.e., clients that don't send a client_id)
                options.AcceptAnonymousClients();

                // Register the signing and encryption credentials
                options.AddDevelopmentEncryptionCertificate()
                    .AddDevelopmentSigningCertificate();

                // Register the ASP.NET Core host and configure the ASP.NET Core options
                options.UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    // todo - enable userinfo endpoint passthrough for openiddict
                    //.EnableUserinfoEndpointPassthrough()
                    ;

                // Configure token lifetime
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15));
                options.SetRefreshTokenLifetime(TimeSpan.FromMinutes(5));

                // Register scopes
                options.RegisterScopes("api", "offline_access");
            })

            // Register the OpenIddict validation components
            .AddValidation(options =>
            {
                // Import the configuration from the local OpenIddict server instance
                options.UseLocalServer();

                // Register the ASP.NET Core host
                options.UseAspNetCore();
            });

        return services;
    }
}