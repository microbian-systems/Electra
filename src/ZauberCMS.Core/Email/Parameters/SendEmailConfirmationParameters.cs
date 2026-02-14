using Electra.Models.Entities;
using ZauberCMS.Core.Membership.Models;

namespace ZauberCMS.Core.Email.Parameters;

public class SendEmailConfirmationParameters
{
    public ElectraUser? User { get; set; }
    public string? NewEmailAddress { get; set; }
    public string? ReturnUrl { get; set; } = "/";
}