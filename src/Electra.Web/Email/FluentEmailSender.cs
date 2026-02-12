using System.Threading.Tasks;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Logging;

namespace Electra.Common.Web.Email;

public class FluentEmailSender : IEmailSender
{
    private readonly IFluentEmail client;
    private readonly ILogger<FluentEmailSender> log;

    public FluentEmailSender(IFluentEmail client, ILogger<FluentEmailSender> log)
    {
        this.log = log;
        this.client = client;
    }
        
    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        log.LogInformation($"sending email to {email}");
        await client.To(email)
            .Subject(subject)
            .Body(htmlMessage, true)
            .SendAsync();
        log.LogInformation($"email successfully sent");
    }
}