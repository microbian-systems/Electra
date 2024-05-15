using Electra.Common.Web.Exceptions;
using Electra.Common.Web.Middleware;
using Electra.Services;
using Electra.Common.Web.Logging.Electra.AspNetCore.Middleware.Logging;
using Electra.Common.Web.Performance;
using Electra.Common.Web.Services;
using Electra.Services.Geo;
using Electra.Services.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;

namespace Electra.Common.Web.Extensions;

public static class ElectraWebExtensions
{
    public static IServiceCollection AddElectraDefaultServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, string connString)
    {
        if(string.IsNullOrEmpty(connString))
            connString = config.GetConnectionString("DefaultConnection")
                         ?? throw new ArgumentNullException(nameof(connString), "Connection string is required for Electra services");
        services.AddDataLayerPersistence(config, host);
        services.AddAspNetIdentityEx(config, host);
        //services.AddElectraIdentity<ElectraUser, ElectraRole>();
        services.AddElectraCoreServices(config, host);
        services.AddDataLayerPersistence(config, host);
        //services.AddElectraIdentityDefaults<TUser, ElectraIdentityContext>(connString);

        return services;
    }
    
    public static IServiceCollection AddElectraCoreServices(
        this IServiceCollection services, 
        IConfiguration config, 
        IWebHostEnvironment host, 
        bool enableAntiForgeryProtection = false)
    {
        services.AddMapster();
        if (enableAntiForgeryProtection)
            services.ConfigureAntiForgeryOptions();
        services.AddHttpContextAccessor();
        services.AddSerilogLogging(config);
        services.AddScoped<ITokenValidationService, ElectraJwtValidationService>();
        services.AddEmailServies(config, host);
        services.AddMiniProfilerEx();
        services.ConfigureAppSettings(config, host);
        services.AddElectraCaching(config);
        services.AddElectraMiddleware();
        services.ConfigureEmailServices(config);
        services.AddScoped<IElectraUserService, ElectraUserService>();
        services.AddScoped<ISmsService, TwilioSmsService>();
        services.AddTransient<IEmailSender, SendGridMailer>();
        services.AddTransient<IPasswordService, PasswordService>();
        services.AddScoped<IZipApiService, ZipApiService>();
        services.AddScoped(typeof(IElectraUserService<>), typeof(ElectraUserServiceBase<>));
        services.AddScoped<IElectraUserProfileService, ElectraUserProfileService>();
        services.AddScoped(typeof(IUserProfileService<>), typeof(UserProfileService<>));
        services.AddEmailServies(config, host);
        services.ConfigureAppSettings(config, host);
        services.ConfigureEmailServices(config);
        services.AddTransient<IEmailSender, SendGridMailer>();
        services.AddTransient<IPasswordService, PasswordService>();
        services.AddTransient<IZipApiService, ZipApiService>();
        services.AddEmailServies(config, host);

        return services;
    }

    public static IServiceCollection AddElectraMiddleware(this IServiceCollection services)
    {
        services.AddScoped<PerfLoggingMiddleware>(); // todo - verify why this is need (think its because constructor injection instead of method)
        return services;
    }

    public static IApplicationBuilder UseDefaultElectraServices(this IApplicationBuilder app)
    {
        app.UseElectraMiddleware();
        return app;
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
}