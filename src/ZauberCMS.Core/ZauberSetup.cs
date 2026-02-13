using System.Reflection;
using System.Threading.RateLimiting;
using Blazored.Modal;
using Electra.Auth.Extensions;
using Electra.Common.Web.Extensions;
using ImageResize.Core.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Radzen;
using Raven.Client.Documents.Session;
using Serilog;
using ZauberCMS.Core.Content.ContentFinders;
using ZauberCMS.Core.Data.Interfaces;
using ZauberCMS.Core.Email;
using ZauberCMS.Core.Extensions;
using ZauberCMS.Core.Content.Interfaces;
using ZauberCMS.Core.Content.Services;
using ZauberCMS.Core.Membership.Interfaces;
using ZauberCMS.Core.Membership.Services;
using ZauberCMS.Core.Media.Interfaces;
using ZauberCMS.Core.Media.Services;
using ZauberCMS.Core.Languages.Interfaces;
using ZauberCMS.Core.Languages.Services;
using ZauberCMS.Core.Tags.Interfaces;
using ZauberCMS.Core.Tags.Services;
using ZauberCMS.Core.Audit.Interfaces;
using ZauberCMS.Core.Audit.Services;
using ZauberCMS.Core.Data.Services;
using ZauberCMS.Core.Seo.Interfaces;
using ZauberCMS.Core.Seo.Services;
using ZauberCMS.Core.Email.Interfaces;
using ZauberCMS.Core.Email.Services;
using ZauberCMS.Core.Jobs;
using ZauberCMS.Core.Languages.Parameters;
using ZauberCMS.Core.Media.Middleware;
using ZauberCMS.Core.Membership;
using ZauberCMS.Core.Middleware;
using ZauberCMS.Core.Membership.Claims;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Membership.Stores;
using ZauberCMS.Core.Plugins;
using ZauberCMS.Core.Plugins.Interfaces;
using ZauberCMS.Core.Providers;
using ZauberCMS.Core.Settings;
using ZauberCMS.Core.Shared;
using ZauberCMS.Core.Shared.Services;
using ZauberCMS.RTE.Services;
using Electra.Persistence.RavenDB.Extensions;
using Electra.Persistence.RavenDB.Identity;

namespace ZauberCMS.Core;

public static class ZauberSetup
{
    public static void AddZauberCms(this WebApplicationBuilder builder, params Assembly[] additionalAssemblies)
    {
        var services = builder.Services;
        var config = builder.Configuration;
        var env = builder.Environment;

        builder.Host.UseSerilog((context, configuration) =>
            configuration.ReadFrom.Configuration(context.Configuration));

        // Enable static web assets from NuGet RCL packages for Kestrel
        builder.WebHost.UseStaticWebAssets();

        // Bind configuration to ZauberSettings instance
        var zauberSettings = new ZauberSettings();
        builder.Configuration.GetSection(Constants.SettingsConfigName).Bind(zauberSettings);
        services.Configure<ZauberSettings>(builder.Configuration.GetSection(Constants.SettingsConfigName));

        // Configure ImageResize with settings from ZauberSettings
        services.AddImageResize(o =>
        {
            o.EnableMiddleware = zauberSettings.ImageResize.EnableMiddleware;
            o.ContentRoots = zauberSettings.ImageResize.ContentRoots;
            o.WebRoot = zauberSettings.ImageResize.WebRoot ?? builder.Environment.WebRootPath;
            o.CacheRoot = zauberSettings.ImageResize.CacheRoot ??
                          Path.Combine(builder.Environment.WebRootPath, "_mediacache");
            o.AllowUpscale = zauberSettings.ImageResize.AllowUpscale;
            o.DefaultQuality = zauberSettings.ImageResize.DefaultQuality;
            o.PngCompressionLevel = zauberSettings.ImageResize.PngCompressionLevel;
            o.HashOriginalContent = zauberSettings.ImageResize.HashOriginalContent;
            o.Cache.FolderSharding = zauberSettings.ImageResize.Cache.FolderSharding;
            o.Cache.PruneOnStartup = zauberSettings.ImageResize.Cache.PruneOnStartup;
            o.Cache.MaxCacheBytes = zauberSettings.ImageResize.Cache.MaxCacheBytes;
            o.ResponseCache.ClientCacheSeconds = zauberSettings.ImageResize.ResponseCache.ClientCacheSeconds;
            o.ResponseCache.SendETag = zauberSettings.ImageResize.ResponseCache.SendETag;
            o.ResponseCache.SendLastModified = zauberSettings.ImageResize.ResponseCache.SendLastModified;
        });

        services.AddHttpClient();

        services.AddScoped(sp =>
        {
            var navigationManager = sp.GetRequiredService<NavigationManager>();
            return new HttpClient { BaseAddress = new Uri(navigationManager.BaseUri) };
        });

        services.AddCascadingAuthenticationState();
        services.AddScoped<IdentityUserAccessor>();
        services.AddScoped<IdentityRedirectManager>();
        services.AddScoped<AuthenticationStateProvider, IdentityRevalidatingAuthenticationStateProvider>();

        // Register Service Interfaces and Implementations
        services.AddScoped<IContentService, ContentService>();
        services.AddScoped<IContentVersioningService, ContentVersioningService>();
        services.AddScoped<IMembershipService, MembershipService>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddScoped<ILanguageService, LanguageService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<ISeoService, SeoService>();
        services.AddScoped<IDataService, DataService>();
        services.AddScoped<IEmailService, EmailService>();

        services.AddScoped<ZauberRouteValueTransformer>();
        services.AddScoped<MediaValidationService>();

        services.AddHostedService<DailyJob>();

        services.AddRadzenComponents();

        if (!zauberSettings.RedisConnectionString.IsNullOrWhiteSpace())
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = zauberSettings.RedisConnectionString;
            });
        }

        var databaseProvider = zauberSettings.DatabaseProvider;
        services.AddRavenPersistence(builder.Configuration);
        if (databaseProvider != null)
        {
            var identityBuilder = services.AddIdentityCore<CmsUser>(options =>
                {
                    // Password settings.
                    options.Password.RequireDigit = zauberSettings.Identity.PasswordRequireDigit;
                    options.Password.RequireLowercase = zauberSettings.Identity.PasswordRequireLowercase;
                    options.Password.RequireNonAlphanumeric = zauberSettings.Identity.PasswordRequireNonAlphanumeric;
                    options.Password.RequireUppercase = zauberSettings.Identity.PasswordRequireUppercase;
                    options.Password.RequiredLength = zauberSettings.Identity.PasswordRequiredLength;
                    options.Password.RequiredUniqueChars = zauberSettings.Identity.PasswordRequiredUniqueChars;

                    // Lockout settings.
                    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
                    options.Lockout.MaxFailedAccessAttempts = 6;
                    options.Lockout.AllowedForNewUsers = true;

                    // User settings.
                    options.User.RequireUniqueEmail = true;
                    options.User.AllowedUserNameCharacters += " ";

                    // Email
                    options.SignIn.RequireConfirmedAccount = zauberSettings.Identity.SignInRequireConfirmedAccount;
                })
                .AddRavenDbIdentityStores<CmsUser>()
                .AddSignInManager()
                .AddRoles<Role>()
                .AddRoleManager<RoleManager<Role>>()
                .AddClaimsPrincipalFactory<ZauberUserClaimsPrincipalFactory>()
                .AddDefaultTokenProviders();
            services.AddScoped<IRoleStore<Role>, RoleStore<Role>>();
            //
            // identityBuilder.AddRoles<Role>();
            //
            // identityBuilder
            //     .AddClaimsPrincipalFactory<ZauberUserClaimsPrincipalFactory>()
            //     .AddSignInManager()
            //     .AddDefaultTokenProviders();

            services.AddScoped<IUserEmailStore<CmsUser>, UserEmailStore>();
        }
        else
        {
            throw new Exception("Unable to find database provider in appSettings");
        }

        services.AddHttpContextAccessor();
        services.AddAntiforgery();
        services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
        services.AddBlazoredModal();

        // Add rate limiting for authentication endpoints
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Sliding window rate limiter for login attempts
            options.AddPolicy("login", httpContext =>
                RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 10, // 10 attempts
                        Window = TimeSpan.FromMinutes(5), // per 5 minutes
                        SegmentsPerWindow = 5,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0 // No queueing
                    }));
        });

        services.AddScoped<ExtensionManager>();
        services.AddScoped<ProviderService>();
        services.AddScoped(typeof(ValidateService<>));
        services.AddScoped<ICacheService, DefaultCacheService>();
        services.AddScoped<IHtmlSanitizerService, DefaultHtmlSanitizerService>();
        services.AddScoped<SignInManager<CmsUser>, ZauberSignInManager>();
        services.AddScoped<IEmailSender<CmsUser>, IdentityEmailSender>();
        services.AddScoped<TreeState>();
        services.AddScoped<ContentFinderPipeline>();

        services.AddSingleton<LayoutResolverService>();
        services.AddSingleton<AppState>();

        // Add Authentication + Authorization
        // var authBuilder = services.AddAuthentication(options =>
        // {
        //     options.DefaultScheme = IdentityConstants.ApplicationScheme;
        //     options.DefaultSignInScheme = IdentityConstants.ExternalScheme;
        // });
        //authBuilder.AddIdentityCookies();
        /*services.AddAuthorizationBuilder()
                    .AddPolicy("AdminOnly", policy => policy.RequireRole(Constants.Roles.AdminRoleName));*/

        #region "aero"

        var authBuilder = services.AddElectraAuthentication(env, config);
        var authorizationBuilder = services.AddAuthorizationBuilder()
            //.AddPolicy("AdminOnly", policy => policy.RequireRole(Constants.Roles.AdminRoleName))
            ;

        #endregion

        // Build explicit assembly list - only scan assemblies we explicitly register
        var assembliesToScan = new List<Assembly>
        {
            // Core Zauber assemblies
            typeof(ZauberSetup).Assembly, // ZauberCMS.Core (using typeof since it's this assembly)
            Assembly.Load(new AssemblyName("ZauberCMS.Components")), // Required
        };

        // Add entry assembly (the host application)
        var entryAssembly = Assembly.GetEntryAssembly();
        if (entryAssembly != null && !assembliesToScan.Contains(entryAssembly))
        {
            assembliesToScan.Add(entryAssembly);
        }

        // Add any additional assemblies passed by the user
        if (additionalAssemblies.Length > 0)
        {
            assembliesToScan.AddRange(additionalAssemblies.Where(a => !assembliesToScan.Contains(a)));
        }

        // Add Routing last (critical for proper component discovery order - required for Blazor routing)
        var zauberRouting = Assembly.Load(new AssemblyName("ZauberCMS.Routing"));
        if (!assembliesToScan.Contains(zauberRouting))
        {
            assembliesToScan.Add(zauberRouting);
        }

        var discoverAssemblies = assembliesToScan.ToArray();
        AssemblyManager.SetAssemblies(discoverAssemblies);

        // Add Zauber RTE services
        services.AddZauberRte(discoverAssemblies);

        // Build the service provider and get the extension manager
        var serviceProvider = services.BuildServiceProvider();
        var extensionManager = serviceProvider.GetRequiredService<ExtensionManager>();

        // Detailed errors have been enabled
        if (zauberSettings.ShowDetailedErrors)
        {
            services
                .AddRazorComponents(c => c.DetailedErrors = true)
                .AddInteractiveServerComponents(c => c.DetailedErrors = true);
        }
        else
        {
            services.AddRazorComponents()
                .AddInteractiveServerComponents();
        }

        services.AddControllersWithViews()
            .AddRazorOptions(options =>
            {
                // This adds another search path that looks for views in the root Views folder.
                options.ViewLocationFormats.Add("/Views/{0}.cshtml");
            });


        // Start up items
        var startUpItems = extensionManager.GetInstances<IStartupPlugin>();
        foreach (var startUpItem in startUpItems)
        {
            startUpItem.Value.Register(services, builder.Configuration);
        }


        // Add external authentication providers
        var providers = extensionManager.GetInstances<IExternalAuthenticationProvider>();
        foreach (var provider in providers)
        {
            provider.Value.Add(services, authBuilder, builder.Configuration);
        }

        // Add localization services
        services.AddLocalization(opts => { opts.ResourcesPath = "Resources"; });
    }

    public static void AddZauberCms<T>(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var db = sp.GetRequiredService<IDocumentSession>();
        var extensionManager = sp.GetRequiredService<ExtensionManager>();
        var languageService = sp.GetRequiredService<ILanguageService>();
        var settings = sp.GetRequiredService<IOptions<ZauberSettings>>();

        try
        {
            // todo - use a ravendb construct here for seeding the db
            // Get any seed data
            var seedData = extensionManager.GetInstances<ISeedData>();
            foreach (var data in seedData)
            {
                data.Value.Initialise(db);
            }

            // Is this ok to use the awaiter and result here?
            var langs = languageService
                .QueryLanguage(new QueryLanguageParameters { AmountPerPage = 200 })
                ;

            // en-US must be the default culture as that's what the backoffice resource is
            var supportedCultures = new List<string> { settings.Value.AdminDefaultLanguage };

            foreach (var langsItem in langs.Items)
            {
                if (langsItem.LanguageIsoCode != null) supportedCultures.Add(langsItem.LanguageIsoCode);
            }

            var supportedCulturesArray = supportedCultures.Distinct().ToArray();
            var localizationOptions = new RequestLocalizationOptions()
                .SetDefaultCulture(settings.Value.AdminDefaultLanguage)
                .AddSupportedCultures(supportedCulturesArray)
                .AddSupportedUICultures(supportedCulturesArray);
            app.UseRequestLocalization(localizationOptions);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error during startup trying to do Db migrations");
        }

        app.UseSerilogRequestLogging();
        app.UseHttpsRedirection();

        app.UseAuthentication();

        // Our middleware must run before ImageResize to check restricted media
        app.UseMiddleware<RestrictedMediaMiddleware>();

        // ImageResize must run before UseStaticFiles to intercept image requests from media folders
        app.UseImageResize();

        // Serve static files before routing to prevent catch-all route from intercepting non-image media files
        app.UseStaticFiles();

        app.UseRouting();

        // Redirect middleware must run after routing to process SEO redirects with proper HTTP status codes
        app.UseMiddleware<RedirectMiddleware>();

        // Culture middleware must run after routing so we have access to route values
        app.UseMiddleware<CultureMiddleware>();

        app.UseRateLimiter();

        app.UseAuthorization();

        app.UseAntiforgery();
        app.MapControllers();

        app.MapStaticAssets();

        app.MapRazorComponents<T>()
            .AddInteractiveServerRenderMode(o => o.ContentSecurityFrameAncestorsPolicy = "'none'")
            .AddAdditionalAssemblies(ExtensionManager.GetFilteredAssemblies(null).ToArray()!);

        // Add additional endpoints required by the Identity /Account Razor components.
        app.MapAdditionalIdentityEndpoints();

        //app.MapBlazorHub();

        //app.MapDynamicControllerRoute<ZauberRouteValueTransformer>("{**slug}");

        /*app.MapControllerRoute(
                name: "default",
                pattern: "{controller=ZauberRender}/{action=Index}/{id?}")
            .WithMetadata(new RouteOptions { LowercaseUrls = true }) // Lowercase URLs for better SEO
            .WithStaticAssets(); // Ensures static files load before hitting controllers; */

        //app.MapFallbackToController("Index", "ZauberRender");
    }
}