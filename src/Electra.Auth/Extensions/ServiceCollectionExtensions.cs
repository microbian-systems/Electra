using Electra.Auth.Passkey;
using Electra.Auth.Services.Abstractions.AuthenticationCeremonyHandle;
using Electra.Auth.Services.Abstractions.RegistrationCeremonyHandle;
using Electra.Auth.Services.Abstractions.User;
using Electra.Auth.Services.Implementation;
using Electra.Core.Identity;
using Electra.Models.Entities;
using Electra.Persistence;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using ThrowGuard;
using WebAuthn.Net.Configuration.DependencyInjection;
using WebAuthn.Net.Storage.InMemory.Models;
using WebAuthn.Net.Storage.InMemory.Services.ContextFactory;
using WebAuthn.Net.Storage.PostgreSql.Models;
using WebAuthn.Net.Storage.PostgreSql.Services.ContextFactory;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Electra.Persistence.RavenDB;

namespace Electra.Auth.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds comprehensive authentication services including ASP.NET Core Identity, 
    /// Passkeys/WebAuthn, OpenIddict, and social authentication providers.
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="env">The hosting environment</param>
    /// <param name="config">Configuration</param>
    /// <returns>The service collection for chaining</returns> `
    public static IServiceCollection AddElectraAuthentication(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config)
    {
        var useRavenDb = config.GetValue<bool>("Identity:UseRavenDB");

        // Configure database context with conditional registration
        services.AddDbContext<ElectraDbContext>(opts =>
        {
            if (env.IsDevelopment())
            {
                // Use in-memory database for development/testing
                opts.UseInMemoryDatabase("ElectraAuthDb");
            }
            else
            {
                // Use PostgreSQL for production
                var connectionString = config.GetConnectionString("DefaultConnection");
                Throw.InvalidOpIfNullOrEmpty(connectionString, "Connection string 'DefaultConnection' null or empty.");
                opts.UseNpgsql(connectionString);
            }

            // Register the entity sets needed by OpenIddict
            opts.UseOpenIddict();
        });

        // Configure ASP.NET Core Identity
        var identityBuilder = services.AddIdentity<ElectraUser, ElectraRole>(opts =>
            {
                opts.Password.RequireDigit = true;
                opts.Password.RequireLowercase = true;
                opts.Password.RequireNonAlphanumeric = true;
                opts.Password.RequireUppercase = true;
                opts.Password.RequiredLength = 8;

                opts.User.RequireUniqueEmail = true;
                opts.SignIn.RequireConfirmedEmail = false; // Set to true if email confirmation is implemented
            });

        if (useRavenDb)
        {
            identityBuilder.AddRavenDbIdentityStores<ElectraUser, ElectraRole>(options =>
            {
                options.AutoSaveChanges = true;
            });
        }
        else
        {
            identityBuilder.AddEntityFrameworkStores<ElectraDbContext>();
        }

        identityBuilder.AddDefaultTokenProviders()
            .AddPasswordlessLoginProvider(); // Add passkey support

        // Add JWT Authentication with OpenIddict
        services.AddJwtAuthentication(config);

        // Register WebAuthn/Passkey services
        if (env.IsDevelopment())
        {
            services.AddWebAuthnCore<DefaultInMemoryContext>()
                .AddDefaultStorages()
                .AddContextFactory<DefaultInMemoryContext, DefaultInMemoryContextFactory>()
                .AddCredentialStorage<DefaultInMemoryContext, DefaultCookieCredentialStorage<DefaultInMemoryContext>>();
        }
        else
        {
            services.AddWebAuthnCore<DefaultPostgreSqlContext>()
                .AddDefaultStorages()
                .AddContextFactory<DefaultPostgreSqlContext, DefaultPostgreSqlContextFactory>()
                .AddCredentialStorage<DefaultPostgreSqlContext, DefaultCookieCredentialStorage<DefaultPostgreSqlContext>>();
        }
        
        // services.AddOpenTelemetry()
        //     .WithMetrics(metrics =>
        //     {
        //         metrics.AddWebAuthnNet();
        //         metrics.AddPrometheusExporter();
        //     });
        services.AddSingleton<IRegistrationCeremonyHandleService, DefaultRegistrationCeremonyHandleService>();
        services.AddSingleton<IAuthenticationCeremonyHandleService, DefaultAuthenticationCeremonyHandleService>();
        services.AddSingleton<IUserService, DefaultUserService>();

        // Register Passkey/WebAuthn service implementations
        services.AddScoped<IUserService, DefaultUserService>();
        services.AddScoped<IRegistrationCeremonyHandleService, DefaultRegistrationCeremonyHandleService>();
        services.AddScoped<IAuthenticationCeremonyHandleService, DefaultAuthenticationCeremonyHandleService>();

        // Add Data Protection for cookie encryption
        services.AddDataProtection();
        
        
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, static options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = false;
                options.Cookie.SameSite = SameSiteMode.None;
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.LoginPath = "/passwordless";
                options.LogoutPath = "/account/logout";
            })
            .AddJwtBearer(options => {
                var jwtSettings = config.GetSection("JwtSettings");
                var secretKey = jwtSettings["SecretKey"] ?? "mysupersecretkey13246";
                                //?? throw new InvalidOperationException("JWT SecretKey not configured.");
                var key = Encoding.ASCII.GetBytes(secretKey);
    
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"] ?? "localhost",
                    ValidAudience = jwtSettings["Audience"] ?? "localhost",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });
        services.Configure<RouteOptions>(options => options.LowercaseUrls = true);

        // Configure authentication
        // services.AddAuthentication(options => {
        //         // Default scheme for web pages is Cookies
        //         options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //         options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        //         // API requests use JWT
        //         options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        //     })
        //     .AddCookie(options => {
        //         options.LoginPath = "/auth/login";
        //         options.LogoutPath = "/auth/logout";
        //         options.AccessDeniedPath = "/auth/access-denied";
        //     })
        //     .AddJwtBearer(options => {
        //         var jwtSettings = config.GetSection("JwtSettings");
        //         var secretKey = jwtSettings["SecretKey"] 
        //             ?? throw new InvalidOperationException("JWT SecretKey not configured.");
        //         var key = Encoding.ASCII.GetBytes(secretKey);
        //
        //         options.TokenValidationParameters = new TokenValidationParameters
        //         {
        //             ValidateIssuer = true,
        //             ValidateAudience = true,
        //             ValidateLifetime = true,
        //             ValidateIssuerSigningKey = true,
        //             ValidIssuer = jwtSettings["Issuer"],
        //             ValidAudience = jwtSettings["Audience"],
        //             IssuerSigningKey = new SymmetricSecurityKey(key)
        //         };
        //     });

        // Add social authentication providers (optional)
        services.AddSocialAuthentication(config);

        return services;
    }

    /// <summary>
    /// Legacy method for backward compatibility. Use AddElectraAuthentication instead.
    /// </summary>
    [Obsolete("Use AddElectraAuthentication instead for comprehensive authentication setup.")]
    public static IServiceCollection AddAuthenticationServices(
        this IServiceCollection services,
        IHostEnvironment env,
        IConfiguration config)
    {
        return services.AddElectraAuthentication(env, config);
    }

    /// <summary>
    /// Adds social authentication providers (Google, Twitter, etc.)
    /// </summary>
    /// <param name="services">The service collection</param>
    /// <param name="config">Configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static IServiceCollection AddSocialAuthentication(
        this IServiceCollection services,
        IConfiguration config)
    {
        var authBuilder = services.AddAuthentication();

        // Google OAuth
        var googleClientId = config["Authentication:Google:ClientId"];
        var googleClientSecret = config["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            authBuilder.AddGoogle(options => {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/auth/signin-google";
            });
        }

        // Twitter OAuth
        var twitterConsumerKey = config["Authentication:Twitter:ConsumerKey"];
        var twitterConsumerSecret = config["Authentication:Twitter:ConsumerSecret"];
        if (!string.IsNullOrEmpty(twitterConsumerKey) && !string.IsNullOrEmpty(twitterConsumerSecret))
        {
            authBuilder.AddTwitter(options => {
                options.ConsumerKey = twitterConsumerKey;
                options.ConsumerSecret = twitterConsumerSecret;
                options.CallbackPath = "/auth/signin-twitter";
            });
        }

        // Microsoft OAuth
        var microsoftClientId = config["Authentication:Microsoft:ClientId"];
        var microsoftClientSecret = config["Authentication:Microsoft:ClientSecret"];
        if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
        {
            authBuilder.AddMicrosoftAccount(options => {
                options.ClientId = microsoftClientId;
                options.ClientSecret = microsoftClientSecret;
                options.CallbackPath = "/auth/signin-microsoft";
            });
        }

        // Facebook OAuth
        var facebookAppId = config["Authentication:Facebook:AppId"];
        var facebookAppSecret = config["Authentication:Facebook:AppSecret"];
        if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
        {
            authBuilder.AddFacebook(options => {
                options.AppId = facebookAppId;
                options.AppSecret = facebookAppSecret;
                options.CallbackPath = "/auth/signin-facebook";
            });
        }

        // Note: Apple authentication requires additional setup and certificate management
        // Uncomment and configure when Apple authentication is properly set up
        /*
        var appleClientId = config["Authentication:Apple:ClientId"];
        var appleKeyId = config["Authentication:Apple:KeyId"];
        var appleTeamId = config["Authentication:Apple:TeamId"];
        var applePrivateKey = config["Authentication:Apple:PrivateKey"];
        if (!string.IsNullOrEmpty(appleClientId) && !string.IsNullOrEmpty(appleKeyId) && 
            !string.IsNullOrEmpty(appleTeamId) && !string.IsNullOrEmpty(applePrivateKey))
        {
            authBuilder.AddApple(options => {
                options.ClientId = appleClientId;
                options.KeyId = appleKeyId;
                options.TeamId = appleTeamId;
                options.PrivateKey = applePrivateKey;
                options.CallbackPath = "/auth/signin-apple";
            });
        }
        */

        return services;
    }
}