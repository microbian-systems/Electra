namespace Aero.CMS.Core.Content.Models.Blocks;

public class LoginBlock : ContentBlock
{
    public static string BlockType => "loginBlock";
    public override string Type => BlockType;

    public string Title
    {
        get => Properties.TryGetValue("title", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["title"] = value;
    }

    public bool ShowForgotPasswordLink
    {
        get => Properties.TryGetValue("showForgotPasswordLink", out var value) && value is bool b && b;
        set => Properties["showForgotPasswordLink"] = value;
    }

    public string RedirectUrl
    {
        get => Properties.TryGetValue("redirectUrl", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["redirectUrl"] = value;
    }

    public string AuthenticationMode
    {
        get => Properties.TryGetValue("authenticationMode", out var value) ? value?.ToString() ?? "Mock" : "Mock";
        set => Properties["authenticationMode"] = value;
    }
}