using Microsoft.AspNetCore.Identity;

namespace ZauberCMS.Core.Membership.Parameters;

public class ExternalLoginParameters
{
    public string? ProviderDisplayName { get; set; }
    public ExternalLoginInfo? ExternalLoginInfo { get; set; }
    public string? ReturnUrl { get; set; }
}