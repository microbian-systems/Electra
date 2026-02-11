using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Auth;

public partial class SignInCard : MerakiComponentBase
{
    [Parameter]
    public string LogoSrc { get; set; } = "https://merakiui.com/images/logo.svg";

    [Parameter]
    public string Title { get; set; } = "Welcome Back";

    [Parameter]
    public string Subtitle { get; set; } = "Login or create account";

    [Parameter]
    public string EmailPlaceholder { get; set; } = "Email Address";

    [Parameter]
    public string PasswordPlaceholder { get; set; } = "Password";

    [Parameter]
    public string ButtonText { get; set; } = "Sign In";

    [Parameter]
    public bool ShowForgotPassword { get; set; } = true;

    [Parameter]
    public string ForgotPasswordText { get; set; } = "Forget Password?";

    [Parameter]
    public string ForgotPasswordUrl { get; set; } = "#";

    [Parameter]
    public bool ShowFooter { get; set; } = true;

    [Parameter]
    public string FooterText { get; set; } = "Don't have an account?";

    [Parameter]
    public string FooterLinkText { get; set; } = "Register";

    [Parameter]
    public string FooterLinkUrl { get; set; } = "#";

    [Parameter]
    public string Email { get; set; } = "";

    [Parameter]
    public string Password { get; set; } = "";

    [Parameter]
    public EventCallback OnSubmit { get; set; }
}
