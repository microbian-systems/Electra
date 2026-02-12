using Boxed.AspNetCore;
using Electra.Common.Web.Email;
using Electra.Services.Mail;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Electra.Common.Web.Extensions;

// FluentEmail - https://github.com/lukencode/FluentEmail
public static class FluentEmailExtensions
{
    public static IServiceCollection AddEmailServies(this IServiceCollection services, IConfiguration config,
        IWebHostEnvironment env)
    {
        var smtpOpts = new SmtpEmailOptions();
        services.Configure<SmtpEmailOptions>(config.GetSection(smtpOpts.SectionName));

        services.AddIfElse(env.IsProduction(), sc =>
        {
            sc.AddMailtrapMailer(config);
            return sc;
        }, sc =>
        {
            sc.AddFluentSmtpEmailSender(config); // swap this out to whatever FluentEmail client you have
            return sc;
        });

        services.AddScoped<IEmailSender, FluentEmailSender>();

        return services;
    }

    public static IServiceCollection AddAspNetEmailSender(this IServiceCollection services)
    {
        var sp = services.BuildServiceProvider();
        var opts = sp.GetRequiredService<IOptionsMonitor<SmtpEmailOptions>>();
        var client = new SmtpEmailSender(opts);
        services.AddScoped<IEmailSender>(provider => client);

        return services;
    }

    public static IServiceCollection AddMailtrapMailer(this IServiceCollection services, IConfiguration config)
    {
        var opts = GetEmailOptions(services, config);
        services.AddFluentEmail(opts.SenderEmail) // todo - get value from appsettings
            .AddRazorRenderer()
            .AddMailtrapSender();

        return services;
    }

    public static IServiceCollection AddFluentSmtpEmailSender(this IServiceCollection services, IConfiguration config)
    {
        var opts = GetEmailOptions(services, config);
        services.AddFluentEmail(opts.SenderEmail)
            .AddRazorRenderer()
            .AddSmtpSender(opts.Host, opts.Port, opts.Username, opts.Password);

        return services;
    }

    private static string GetFromEmailAddress(IServiceCollection services, IConfiguration config)
    {
        var opts = GetEmailOptions(services, config);
        return opts.SenderEmail;
    }

    private static SmtpEmailOptions GetEmailOptions(IServiceCollection services, IConfiguration config)
    {
        var opts = new SmtpEmailOptions();
        config.GetSection("AppSettings:SmtpEmailOptions").Bind(opts);
        //services.Configure<SmtpEmailOptions>();

        return opts;
    }
}