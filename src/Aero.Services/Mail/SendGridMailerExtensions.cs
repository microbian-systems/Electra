namespace Aero.Services.Mail;

public static class SendGridMailerExtensions
{
    /// <summary>
    /// Sends an email via sendgrid synchronously
    /// </summary>
    /// <param name="mailer"></param>
    /// <param name="email"></param>
    /// <param name="subject"></param>
    /// <param name="htmlMessage"></param>
    public static void SendEmail(this SendGridMailer mailer, string email, string subject, string htmlMessage)
    {
        mailer.SendEmailAsync(email, subject, htmlMessage).GetAwaiter().GetResult();
    }
}