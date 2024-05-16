namespace Electra.Common.Web.Extensions;

// public static class SendGridExtensions
// {
//     public static WebApplicationBuilder AddSengGridService(this WebApplicationBuilder builder)
//     {
//         builder.Services.AddSendGridService(builder.Configuration);
//
//         return builder;
//     }
//
//     public static IServiceCollection AddSendGridService(this IServiceCollection services, IConfiguration configuration)
//     {
//         var option = configuration.GetRequiredSection(nameof(SendGridOptions));
//         services.Configure<SendGridOptions>(option);
//         var config = option.Get<SendGridOptions>();
//
//         services.AddSendGrid(o => o.ApiKey = config.APIKey);
//         services.AddTransient<IEmailSender, EmailSender>();
//
//         return services;
//     }
// }