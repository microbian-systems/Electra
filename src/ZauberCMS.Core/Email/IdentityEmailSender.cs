using Electra.Models.Entities;
using Microsoft.AspNetCore.Identity;
using ZauberCMS.Core.Membership.Models;
using ZauberCMS.Core.Providers;

namespace ZauberCMS.Core.Email;

public class IdentityEmailSender(ProviderService providerService) : IEmailSender<ElectraUser>
{
    public Task SendConfirmationLinkAsync(ElectraUser user, string email, string confirmationLink)=>  
        providerService.EmailProvider!.SendEmailAsync(email, "Confirm your email",
            $"Please confirm your account by <a href='{confirmationLink}'>clicking here</a>.");

    public Task SendPasswordResetLinkAsync(ElectraUser user, string email, string resetLink) =>
        providerService.EmailProvider!.SendEmailAsync(email, "Reset your password",
            $"Please reset your password by <a href='{resetLink}'>clicking here</a>.");

    public Task SendPasswordResetCodeAsync(ElectraUser user, string email, string resetCode) =>
        providerService.EmailProvider!.SendEmailAsync(email, "Reset your password",
            $"Please reset your password using the following code: {resetCode}");
}