using System;
using System.Linq;
using Duende.IdentityServer.EntityFramework.Interfaces;
using Electra.Common.Web.Exceptions;
using Electra.Common.Web.Middleware;
using Electra.Models;
using Electra.Services;
using Elasticsearch.Net;
using Mapster;
using Marten;
using Electra.Common.Caching.Decorators;
using Electra.Common.Extensions;
using Electra.Common.Web.Logging.Electra.AspNetCore.Middleware.Logging;
using Electra.Common.Web.Options;
using Electra.Common.Web.Performance;
using Electra.Common.Web.Services;
using Electra.Core.Identity;
using Electra.Persistence;
using Electra.Services.Geo;
using Electra.Services.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Nest;
using Weasel.Core;

namespace Electra.Common.Web.Extensions;

public static class WebExtensions
{
    public static IServiceCollection AddElectraDefaultServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, string connString)
    {
        services.AddElectraCoreServices<ElectraUser, ElectraRole>(config, host);
        services.AddElectraIdentityDefaults<ElectraUser, ElectraIdentityContext>(connString);
        
        return services;
    }
    
    public static IServiceCollection AddElectraDefaultServices<TUser, TRole>(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, string connString)
        where TUser : ElectraUser, new() where TRole : ElectraRole
    {
        services.AddElectraCoreServices<TUser, TRole>(config, host);
        services.AddElectraIdentityDefaults<TUser, ElectraIdentityContext>(connString);
        
        return services;
    }
    
    public static IServiceCollection AddElectraDefaultServices<TUser, TRole, TContext>(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, string connString)
        where TUser : ElectraUser, new() where TRole : ElectraRole where TContext : DbContext, IPersistedGrantDbContext
    {
        services.AddElectraCoreServices<TUser, TRole>(config, host);
        services.AddElectraIdentityDefaults<TUser, TContext>(connString);
        
        return services;
    }
    
    public static IServiceCollection AddElectraCoreServices<TUser, TRole>(
        this IServiceCollection services, 
        IConfiguration config, 
        IWebHostEnvironment host, 
        bool enableAntiForgeryProtection = false) where TUser : ElectraUser, new() where TRole : ElectraRole
    {
        services.AddMapster();
        if(enableAntiForgeryProtection)
            services.ConfigureAntiForgeryOptions();
        
        services.AddScoped<ITokenValidationService, ElectraTokenValidationService>();
        services.AddScoped<IRoleService<TRole>, RoleService<TRole>>();
        services.AddScoped<UserManager<TUser>>();
        services.AddScoped<SignInManager<TUser>>();
        services.AddScoped<IElectraIdentityService, ElectraIdentityService>();
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();
        services.AddScoped<IElectraUserProfileServiceRepository, ElectraUserProfileServiceRepository>();
        services.AddScoped<SignInManager<ElectraUser>>(); // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddScoped<UserManager<ElectraUser>>();  // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddScoped<UserStore<ElectraUser>>(); // for some reason the DI container expecting this - probably registered the generic Electra user service - fix later
        services.AddDbContext<ElectraDbContext>();
        services.AddScoped<DbContext, ElectraDbContext>();
        services.AddScoped<RoleManager<TRole>>();
        services.ConfigureEmail(config, host);
        services.AddMiniProfilerEx();
        services.ConfigureAppSettings(config, host);
        services.AddElectraCaching();
        services.AddElectraMiddleware();
        //services.AddDataLayerPersistence(config, host);
        //services.AddElectraAuthentication();
        var apiKey = config["AppSettings:SendGrid:Key"];
        var replyEmail = config["AppSettings:SendGrid:From"];
        services
            .AddFluentEmail(replyEmail)
            .AddRazorRenderer()
            .AddSendGridSender(apiKey);
        services.AddScoped<IElectraUserService, ElectraUserService>();
        services.AddScoped<ISmsService, TwilioSmsService>();
        services.AddTransient<IEmailSender, SendGridMailer>();
        services.AddTransient<IPasswordService, PasswordService>();
        services.AddScoped<IZipApiService, ZipApiService>();
        services.AddScoped(typeof(IElectraUserService<>), typeof(ElectraUserServiceBase<>));
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();
        services.AddScoped(typeof(IUserProfileService<>), typeof(UserProfileService<>));

        return services;
    }
    
    
    public static IServiceCollection AddElectraAuthentication(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<IConfiguration>();

        services.AddAuthentication().AddFacebook(opts =>
            {
                opts.AppId = config["Authentication:Facebook:AppId"];
                opts.AppSecret = config["Authentication:Facebook:AppSecret"];
                opts.AccessDeniedPath = "/AccessDeniedPathInfo";
            })
            .AddGoogle(opts =>
            {
                var googleAuthNSection =
                    config.GetSection("Authentication:Google");

                opts.ClientId = googleAuthNSection["ClientId"];
                opts.ClientSecret = googleAuthNSection["ClientSecret"];
            });
            // .AddInstagram(ig =>
            // {
            //     var igConfig = config.GetSection("Authentication:Instagram");
            //     ig.ClientId = igConfig["AppId"];
            //     ig.ClientSecret = igConfig["AppSecret"];
            // })
            // .AddVkontakte(vk =>
            // {
            //     var vkConfig = config.GetSection("Authentication:VKontakte");
            //     vk.ClientId = vkConfig["ClientId"];
            //     vk.ClientSecret = vkConfig["ClientSecret"];
            // });

        return services;
    }
    
    public static IApplicationBuilder UseElectraMiddleware(this IApplicationBuilder app)
    {
        app.UseMiniProfiler();
        app.ConfigureExceptionMiddleware();
        app.UsePerfLogging();
        app.UseSerilogRequestLogging();
        app.UseXssMiddleware();
        
        return app;
    }

    public static IServiceCollection AddDataLayerPersistence<T>(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, bool enableElastic = true, bool enableMarten = true)
        where T : DbContext
    {
        var sp = services.BuildServiceProvider();
        var log = sp.GetRequiredService<ILogger<object>>();
        
        log.LogInformation($"Adding data later persistence rules");
        // services.AddDbContext<T>(options =>   // todo - create db specific registration services for postgres, sqlserver, etc...
        //     options.UseNpgsql(configuration.GetConnectionString("DefaultConnectionString"), x =>
        //     {
        //         x.MigrationsAssembly("Electra.Persistence");
        //     }));
        
        if(enableMarten)
            services.ConfigureMarten(config, host);
        if(enableElastic)
            services.ConfigureElasticsearch();
        
        log.LogInformation($"configuring generic repositories");
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<>), typeof(GenericEntityFrameworkRepository<>));
        services.AddScoped(typeof(IGenericEntityFrameworkRepository<,>), typeof(GenericEntityFrameworkRepository<,>));
        services.AddScoped(typeof(ICachingRepositoryDecorator<,>), typeof(CachingRepository<,>));
        services.AddScoped(typeof(ICachingRepositoryDecorator<>), typeof(CachingRepository<>));
        
        return services;
    }

    public static IServiceCollection AddDataLayerPersistence(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment host)
        => AddDataLayerPersistence<ElectraDbContext>(services, configuration, host);

    public static IServiceCollection ConfigureElasticsearch(this IServiceCollection services)
    {
        services.AddScoped<IElasticClient>(sp =>
        {
            var log = sp.GetRequiredService<ILogger<ElasticClient>>();
            var config = sp.GetRequiredService<IOptions<AppSettings>>();
            var settings = config.Value;

            log.LogInformation($"configuring elastic search client");
            log.LogInformation($"elastic urls: {settings.ElasticsearchUrls.ToJson()}");
            var pool = new SniffingConnectionPool(settings.ElasticsearchUrls.Select(uri => new Uri(uri)));
            var client = new ElasticClient(new ConnectionSettings(pool).DefaultIndex("defaultIndex"));
            return client;
        });

        services.AddScoped(typeof(IElasticsearchRepository<>), 
            typeof(ElasticsearchRepository<>));
        
        return services;
    }

    public static IServiceCollection ConfigureMarten(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host)
    {
        var connString = config.GetConnectionString("Postgres");
        
        var marten = services.AddMarten(opts =>
        {
            opts.Connection(connString);
            if (host.IsDevelopment())
            {
                opts.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
            }
        });
        
        services.AddScoped<IDynamicMartenRepository, DynamicMartinRepository>();
        services.AddScoped(typeof(IGenericMartenRepository<>), typeof(GenericMartenRepository<>));

        //if (host.IsDevelopment())
            //marten.InitializeStore();
            marten.InitializeWith();
        
        return services;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection services)
    {

        return services;
    }
    
    [Obsolete("no longer supported", true)]
    public static IServiceCollection AddSwaggerEx(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var config = sp.GetRequiredService<IConfiguration>();
        var opts = new SwaggerOptions();
        
        config.GetSection("SwaggerOptions").Bind(opts);
        
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc(opts.Version, new OpenApiInfo
            {
                Version = opts.Version,
                Title = opts.Title,
                Description = opts.Description,
                TermsOfService = new Uri(opts.TermsOfServiceUrl),
                Contact = new OpenApiContact
                {
                    Name = opts.ContactName,
                    Email = opts.ContactEmail,
                    Url = new Uri(opts.ContactUrl),
                },
                License = new OpenApiLicense
                {
                    Name = opts.LicenseName,
                    Url = new Uri(opts.LicenseUrl),
                }
            });
        });
        
        return services;
    }

    public static IServiceCollection AddElectraMiddleware(this IServiceCollection services)
    {
        services.AddScoped<PerfLoggingMiddleware>(); // todo - verify why this is need (think its because constructor injection instead of method)
        return services;
    }

    public static IServiceCollection AddElectraCaching(this IServiceCollection services)
    {
        // todo - implement method to add Caching for ElectraX
        
        return services;
    }

    public static IServiceCollection AddMessageQueing(this IServiceCollection services)
    {
        //todo - implement method to add MessageQueuing to Electra

        return services;
    }

    public static IServiceCollection AddMiniProfilerEx(this IServiceCollection services)
    {
        services.AddMiniProfiler(options =>
        {
            // All of this is optional. You can simply call .AddMiniProfiler() for all defaults

            // (Optional) Path to use for profiler URLs, default is /mini-profiler-resources
            options.RouteBasePath = "/profiler";

            // (Optional) Control storage
            // (default is 30 minutes in MemoryCacheStorage)
            // Note: MiniProfiler will not work if a SizeLimit is set on MemoryCache!
            //   See: https://github.com/MiniProfiler/dotnet/issues/501 for details
            //(options.Storage as MemoryCacheStorage).CacheDuration = TimeSpan.FromMinutes(60);

            // (Optional) Control which SQL formatter to use, InlineFormatter is the default
            options.SqlFormatter = new StackExchange.Profiling.SqlFormatters.InlineFormatter();

            // (Optional) To control authorization, you can use the Func<HttpRequest, bool> options:
            // (default is everyone can access profilers)
            //options.ResultsAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
            //options.ResultsListAuthorize = request => MyGetUserFunction(request).CanSeeMiniProfiler;
            // Or, there are async versions available:
            //options.ResultsAuthorizeAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfiler;
            //options.ResultsAuthorizeListAsync = async request => (await MyGetUserFunctionAsync(request)).CanSeeMiniProfilerLists;

            // (Optional)  To control which requests are profiled, use the Func<HttpRequest, bool> option:
            // (default is everything should be profiled)
            //options.ShouldProfile = request => MyShouldThisBeProfiledFunction(request);

            // (Optional) Profiles are stored under a user ID, function to get it:
            // (default is null, since above methods don't use it by default)
            //options.UserIdProvider =  request => MyGetUserIdFunction(request);

            // (Optional) Swap out the entire profiler provider, if you want
            // (default handles async and works fine for almost all applications)
            //options.ProfilerProvider = new MyProfilerProvider();

            // (Optional) You can disable "Connection Open()", "Connection Close()" (and async variant) tracking.
            // (defaults to true, and connection opening/closing is tracked)
            options.TrackConnectionOpenClose = true;

            // (Optional) Use something other than the "light" color scheme.
            // (defaults to "light")
            options.ColorScheme = StackExchange.Profiling.ColorScheme.Auto;

            // The below are newer options, available in .NET Core 3.0 and above:

            // (Optional) You can disable MVC filter profiling
            // (defaults to true, and filters are profiled)
            options.EnableMvcFilterProfiling = true;
            // ...or only save filters that take over a certain millisecond duration (including their children)
            // (defaults to null, and all filters are profiled)
            // options.MvcFilterMinimumSaveMs = 1.0m;

            // (Optional) You can disable MVC view profiling
            // (defaults to true, and views are profiled)
            options.EnableMvcViewProfiling = true;
            // ...or only save views that take over a certain millisecond duration (including their children)
            // (defaults to null, and all views are profiled)
            // options.MvcViewMinimumSaveMs = 1.0m;

            // (Optional) listen to any errors that occur within MiniProfiler itself
            // options.OnInternalError = e => MyExceptionLogger(e);

            // (Optional - not recommended) You can enable a heavy debug mode with stacks and tooltips when using memory storage
            // It has a lot of overhead vs. normal profiling and should only be used with that in mind
            // (defaults to false, debug/heavy mode is off)
            //options.EnableDebugMode = true;
        });
        
        return services;
    }
}