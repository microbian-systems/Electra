using Electra.Core.Identity;
using Electra.Persistence;
using Electra.Auth.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;
using ThrowGuard;
using Electra.Persistence.RavenDB.Identity;
using Microsoft.EntityFrameworkCore;

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
                //opts.UseInMemoryDatabase("ElectraAuthDb");
                opts.UseSqlServer(config.GetConnectionString("localdb"));
            }
            else
            {
                // Use PostgreSQL for production
                var connectionString = config.GetConnectionString("DefaultConnection");
                Throw.InvalidOpIfNullOrEmpty(connectionString, "Connection string 'DefaultConnection' null or empty.");
                opts.UseNpgsql(connectionString);
            }
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

        // services.AddOpenTelemetry()
        //     .WithMetrics(metrics =>
        //     {
        //         metrics.AddWebAuthnNet();
        //         metrics.AddPrometheusExporter();
        //     });
        // services.AddSingleton<IRegistrationCeremonyHandleService, DefaultRegistrationCeremonyHandleService>();
        // services.AddSingleton<IAuthenticationCeremonyHandleService, DefaultAuthenticationCeremonyHandleService>();
        // services.AddSingleton<IUserService, DefaultUserService>();

        // Register Passkey/WebAuthn service implementations
        // services.AddScoped<IUserService, DefaultUserService>();
        // services.AddScoped<IRegistrationCeremonyHandleService, DefaultRegistrationCeremonyHandleService>();
        // services.AddScoped<IAuthenticationCeremonyHandleService, DefaultAuthenticationCeremonyHandleService>();

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
                options.LoginPath = "/cms/admin/login";
                options.LogoutPath = "/cms/admin/logout";
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

        // Add production-grade token services
        // Register persistence based on configuration
        if (useRavenDb)
        {
            services.AddScoped<IJwtSigningKeyPersistence, RavenDbJwtSigningKeyPersistence>();
        }
        else
        {
            // For now, use a fallback in-memory or config-based implementation
            // This will be replaced with EF Core implementation when created
            services.AddScoped<IJwtSigningKeyPersistence>(provider =>
            {
                // Temporary: Use in-memory JWT key store
                // TODO: Replace with EF Core implementation
                var logger = provider.GetRequiredService<ILogger<JwtSigningKeyStore>>();
                var cache = provider.GetRequiredService<IMemoryCache>();
                return new InMemoryJwtSigningKeyPersistence();
            });
        }

        services.AddScoped<IJwtSigningKeyStore, JwtSigningKeyStore>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Add DbContextFactory<DbContext> for EF Core based services (RefreshTokenService)
        services.AddScoped<IDbContextFactory<DbContext>>(provider => 
        {
            var context = provider.GetRequiredService<ElectraDbContext>();
            return new DbContextFactoryAdapter(context);
        });

        // Add memory cache for token store caching
        services.AddMemoryCache();

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

        // Passkey Authentication
        // authBuilder.AddPasskey(options =>
        // {
        //     options.RelyingPartyId = config["Passkey:RelyingPartyId"] ?? "localhost";
        //     options.RelyingPartyName = config["Passkey:RelyingPartyName"] ?? "Microbians.io";
        //     // options.UserVerificationRequirement = UserVerificationRequirement.Required;
        //     // options.AuthenticatorAttachment = AuthenticatorAttachment.Platform;
        //     // options.RequireResidentKey = true;
        // });

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