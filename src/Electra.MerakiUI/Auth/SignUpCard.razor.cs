using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Auth;

public partial class SignUpCard : MerakiComponentBase
{
    [Parameter]
    public string LogoSrc { get; set; } = "https://merakiui.com/images/logo.svg";

    [Parameter]
    public string SignInUrl { get; set; } = "#";

    [Parameter]
    public string ButtonText { get; set; } = "Sign Up";

    [Parameter]
    public bool ShowFileUpload { get; set; } = true;

    [Parameter]
    public string FileUploadText { get; set; } = "Profile Photo";

    [Parameter]
    public string AlreadyHaveAccountText { get; set; } = "Already have an account?";

    [Parameter]
    public string Username { get; set; } = "";

    [Parameter]
    public string Email { get; set; } = "";

    [Parameter]
    public string Password { get; set; } = "";

    [Parameter]
    public string ConfirmPassword { get; set; } = "";

    [Parameter]
    public EventCallback OnSubmit { get; set; }
}
