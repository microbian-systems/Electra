using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Electra.Services.Mail;

public class SendGridMailer : IEmailSender
{
    private readonly IConfiguration config;
    private readonly ILogger<SendGridMailer> log;

    public SendGridMailer(IConfiguration config, ILogger<SendGridMailer> log)
    {
        this.log = log;
        this.config = config;
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        log.LogInformation($"sending contactus email to {email} via send grid");
        var apiKey = config.GetSection("AppSettings:SendGrid:Key").Value;
        var fromAddress = config.GetSection("AppSettings:SendGrid:From").Value;
        var fromName = config.GetSection("AppSettings:SendGrid:FromName").Value;
            
        var opts = new SendGridClientOptions()
        {
            ApiKey = apiKey,
        };
        var client = new SendGridClient(opts);
        var from = new EmailAddress(fromAddress, fromName);
        var tos = new List<EmailAddress>
        {
            new(email, email)
        };
        
        var displayRecipients = false; // set this to true if you want recipients to see each others mail id 
        var msg = MailHelper.CreateSingleEmailToMultipleRecipients(from, tos, subject, "", 
            htmlMessage, displayRecipients);
        var response = await client.SendEmailAsync(msg);
        log.LogInformation($"message sent. Status code - {response.StatusCode}");
    }
}