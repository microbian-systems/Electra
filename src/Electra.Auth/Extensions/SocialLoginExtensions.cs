using System.IO;
using Electra.Core.Identity;
using Electra.Persistence;
using Electra.Auth.Services;
using Electra.Persistence.RavenDB.Extensions;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Routing;
using ThrowGuard;
using Electra.Persistence.RavenDB.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;

namespace Electra.Auth.Extensions;

public static class SocialLoginExtensions
{
    /// <param name="services">The service collection</param>
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Adds comprehensive authentication services including ASP.NET Core Identity, 
        /// Passkeys/WebAuthn, OpenIddict, and social authentication providers.
        /// </summary>
        /// <param name="env">The hosting environment</param>
        /// <param name="config">Configuration</param>
        /// <returns>The service collection for chaining</returns> `
        public IServiceCollection AddElectraAuthentication(IHostEnvironment env,
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
                    Throw.InvalidOpIfNullOrEmpty(connectionString,
                        "Connection string 'DefaultConnection' null or empty.");
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

            // Configure cookie settings in Program.cs
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax; // or None if using HTTPS
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".microbians.Auth";
                options.ExpireTimeSpan = TimeSpan.FromDays(30);
                options.SlidingExpiration = true;
            });

            services.ConfigureExternalCookie(options =>
            {
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.Name = ".microbians.ExternalAuth";
            });

            services.AddDataProtection()
                .PersistKeysToFileSystem(new DirectoryInfo("/keys"))
                //.PersistKeysToAzureBlobStorage(connectionString, containerName, blobName)
                //.PersistKeysToRegistry(Registry.CurrentUser)
                .SetApplicationName("microbians");

            services.AddAuthentication()
                .AddCookie(static options =>
                {
                    options.ExpireTimeSpan = TimeSpan.FromDays(1);
                    options.SlidingExpiration = false;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.LoginPath = "/login";
                    options.LogoutPath = "/logout";
                })
                .AddJwtBearer(options =>
                {
                    var jwtSettings = config.GetSection("JwtSettings");
                    var secretKey = jwtSettings["SecretKey"]
                                    ?? throw new InvalidOperationException("JWT SecretKey not configured.");
                    ;
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
                }).AddSocialAuthentication(config);


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

            return services;
        }
    }

    /// <summary>
    /// Adds social authentication providers (Google, Twitter, etc.)
    /// </summary>
    /// <param name="authBuilder">The authentication builder returned after calling .AddAuthentication()</param>
    /// <param name="config">Configuration</param>
    /// <returns>The service collection for chaining</returns>
    public static AuthenticationBuilder AddSocialAuthentication(
        this AuthenticationBuilder authBuilder,
        IConfiguration config)
    {
        // Google OAuth
        var googleClientId = config["Authentication:Google:ClientId"];
        var googleClientSecret = config["Authentication:Google:ClientSecret"];
        if (!string.IsNullOrEmpty(googleClientId) && !string.IsNullOrEmpty(googleClientSecret))
        {
            authBuilder.AddGoogle(options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/api/auth/external/callback";
                options.SignInScheme = IdentityConstants.ExternalScheme; // ✅ CRITICAL

                // Configure correlation cookie to prevent state errors
                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.IsEssential = true;
            });
        }

        // Twitter OAuth
        var twitterConsumerKey = config["Authentication:Twitter:ConsumerKey"];
        var twitterConsumerSecret = config["Authentication:Twitter:ConsumerSecret"];
        if (!string.IsNullOrEmpty(twitterConsumerKey) && !string.IsNullOrEmpty(twitterConsumerSecret))
        {
            authBuilder.AddTwitter(options =>
            {
                options.ConsumerKey = twitterConsumerKey;
                options.ConsumerSecret = twitterConsumerSecret;
                options.CallbackPath = "/api/auth/external/callback";
                options.SignInScheme = IdentityConstants.ExternalScheme; // ✅ CRITICAL

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.IsEssential = true;
            });
        }

        // Microsoft OAuth
        var microsoftClientId = config["Authentication:Microsoft:ClientId"];
        var microsoftClientSecret = config["Authentication:Microsoft:ClientSecret"];
        if (!string.IsNullOrEmpty(microsoftClientId) && !string.IsNullOrEmpty(microsoftClientSecret))
        {
            authBuilder.AddMicrosoftAccount(options =>
            {
                options.ClientId = microsoftClientId;
                options.ClientSecret = microsoftClientSecret;
                options.CallbackPath = "/api/auth/external/callback";
                options.SignInScheme = IdentityConstants.ExternalScheme; // ✅ CRITICAL

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.IsEssential = true;
            });
        }

        // Facebook OAuth
        var facebookAppId = config["Authentication:Facebook:AppId"];
        var facebookAppSecret = config["Authentication:Facebook:AppSecret"];
        if (!string.IsNullOrEmpty(facebookAppId) && !string.IsNullOrEmpty(facebookAppSecret))
        {
            authBuilder.AddFacebook(options =>
            {
                options.AppId = facebookAppId;
                options.AppSecret = facebookAppSecret;
                options.CallbackPath = "/api/auth/external/callback";
                options.SignInScheme = IdentityConstants.ExternalScheme; // ✅ CRITICAL

                options.CorrelationCookie.SameSite = SameSiteMode.Lax;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.IsEssential = true;
            });
        }

        return authBuilder;
    }
}