namespace Aero.Common.Web.Extensions;

public static class EmailServiceExtensions
{
    public static IServiceCollection ConfigureEmailServices(this IServiceCollection services, IConfiguration config)
    {
        var apiKey = config["AppSettings:SendGrid:Key"];
        var replyEmail = config["AppSettings:SendGrid:From"];

        ArgumentException.ThrowIfNullOrEmpty(apiKey);
        ArgumentException.ThrowIfNullOrEmpty(replyEmail);

        services
            .AddFluentEmail(replyEmail)
            .AddRazorRenderer()
            .AddSendGridSender(apiKey);

        return services;
    }
}