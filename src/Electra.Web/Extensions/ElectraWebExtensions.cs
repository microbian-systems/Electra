using Electra.Common.Web.Exceptions;
using Electra.Common.Web.Middleware;
using Electra.Services;
using Electra.Common.Web.Performance;
using Electra.Common.Web.Services;
using Electra.Services.Geo;
using Electra.Services.Mail;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity.UI.Services;
using OwaspHeaders.Core.Extensions;
using Serilog;

namespace Electra.Common.Web.Extensions;

public static class ElectraWebExtensions
{
    public static WebApplicationBuilder AddElectraDefaultServices(this WebApplicationBuilder builder)
    {
        builder.Services.AddElectraDefaultServices(builder.Configuration, builder.Environment);
        return builder;
    }

    public static IServiceCollection AddElectraDefaultServices(this IServiceCollection services, IConfiguration config, IWebHostEnvironment host, string connString = "")
    {
        if(string.IsNullOrEmpty(connString))
            connString = config.GetConnectionString("DefaultConnection")
                         ?? throw new ArgumentNullException(nameof(connString), "Connection string is required for Electra services");
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
        services.AddSerilog();
        services.AddSerilogLogging(config);
        services.AddMapster();
        // if (enableAntiForgeryProtection)
        //     services.ConfigureAntiForgeryOptions();
        //services.AddRequestResponseLogging();
        if(!host.IsProduction())
            services.AddMiniProfilerEx();

        // https://learn.microsoft.com/en-us/dotnet/core/compatibility/aspnet-core/7.0/default-authentication-scheme
        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-8.0
        services.AddAuthentication(o =>
        {
            o.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            o.DefaultSignOutScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
        {
            // Configure cookie authentication options
            options.LoginPath = "/login";
            options.LogoutPath = "/logout";
        })
        .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
        {
            // Configure JWT Bearer options
            // todo - pull this from the JwtOptions in appsettings.json
            options.Authority = "https://your-authority.com";
            options.Audience = "your-audience";
        });

        services.AddAuthorization(o =>
        {
            string[] schemes = [

                CookieAuthenticationDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme
            ];
            o.DefaultPolicy = new AuthorizationPolicyBuilder()
                .AddAuthenticationSchemes(schemes)
                .RequireAuthenticatedUser()
                .Build();
        });
        //services.AddAntiforgery();
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenValidationService, ElectraJwtValidationService>();
        services.AddEmailServies(config, host);
        services.AddOpenApi();
        services.AddMiniProfilerEx();
        services.ConfigureAppSettings(config, host);
        services.AddElectraCaching(config);
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

    public static IApplicationBuilder UseDefaultElectraServices(this IApplicationBuilder app)
    {
        app.UseElectraMiddleware();
        return app;
    }
    
    public static IApplicationBuilder UseElectraMiddleware(this IApplicationBuilder app)
    {
        app.ConfigureExceptionMiddleware();
        app.UseDefaultLogging();
        app.UseRequestCultureMiddleware();
        app.UsePerfLogging();
        app.UseSerilogRequestLogging();
        app.UseRequestResponseLogging();
        app.UseMiniProfiler();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();
        app.UseCustom404Handler();
        app.UseCustom401Handler();
        app.UseCustom400Handler();
        //app.UseRequestResponseLogging();
        // todo - fix CORS/OWasp and Xss later
        //app.UseXssMiddleware();
        // https://github.com/GaProgMan/OwaspHeaders.Core
        // app.UseSecureHeadersMiddleware();
        
        
        return app;
    }
}