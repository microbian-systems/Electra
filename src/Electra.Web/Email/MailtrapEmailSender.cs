using Microsoft.AspNetCore.Identity.UI.Services;

namespace Electra.Common.Web.Email
{
    public class MailtrapEmailSender : IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await Task.Delay(0);
        }
    }
}