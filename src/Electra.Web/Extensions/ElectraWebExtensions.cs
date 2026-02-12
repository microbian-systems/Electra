using System;
using Electra.Common.Web.Exceptions;
using Electra.Common.Web.Middleware;
using Electra.Services;
using Electra.Common.Web.Services;
using Electra.Core.Extensions;
using Electra.Persistence;
using Electra.Services.Geo;
using Electra.Services.Mail;
using Electra.Web.Extensions;
using Mapster;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
        services.AddEncryptionServices();
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
            var jwtOptions = config.GetSection("Jwt");
            if (jwtOptions.Exists())
            {
                options.Authority = jwtOptions["Authority"];
                options.Audience = jwtOptions["Audience"];
            }
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
        services.ConfigureAppSettings(config, host);
        services.AddElectraCaching(config);
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
        //app.UsePerfLogging();
        // app.UseSerilogRequestLogging();
        // app.UseRequestResponseLogging();
        app.UseMiniProfiler();
        // app.UseCustom404Handler();
        // app.UseCustom401Handler();
        // app.UseCustom400Handler();
        //app.UseRequestResponseLogging();
        // todo - fix CORS/OWasp and Xss later
        //app.UseXssMiddleware();
        // https://github.com/GaProgMan/OwaspHeaders.Core
        // app.UseSecureHeadersMiddleware();
        
        
        return app;
    }
}