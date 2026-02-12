using ZauberCMS.Core.Email.Parameters;

namespace ZauberCMS.Core.Email.Interfaces;

public interface IEmailService
{
    Task SendEmailConfirmationAsync(SendEmailConfirmationParameters parameters, CancellationToken cancellationToken = default);
}