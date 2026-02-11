using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Emails;

public partial class EmailVerification : MerakiComponentBase
{
    [Parameter]
    public string LogoSrc { get; set; } = "https://merakiui.com/images/full-logo.svg";

    [Parameter]
    public string LogoUrl { get; set; } = "#";

    [Parameter]
    public string UserName { get; set; } = "Olivia";

    [Parameter]
    public string VerificationMessage { get; set; } = "This is your verification code:";

    [Parameter]
    public string VerificationCode { get; set; } = "6289";

    [Parameter]
    public string ExpiryMessage { get; set; } = "This code will only be valid for the next 5 minutes. If the code does not work, you can use this login verification link:";

    [Parameter]
    public bool ShowVerifyButton { get; set; } = true;

    [Parameter]
    public string VerifyButtonText { get; set; } = "Verify email";

    [Parameter]
    public string TeamName { get; set; } = "Meraki UI team";

    [Parameter]
    public string RecipientEmail { get; set; } = "contact@merakiui.com";

    [Parameter]
    public string UnsubscribeUrl { get; set; } = "#";

    [Parameter]
    public string PreferencesUrl { get; set; } = "#";

    [Parameter]
    public EventCallback OnVerifyClick { get; set; }
}
