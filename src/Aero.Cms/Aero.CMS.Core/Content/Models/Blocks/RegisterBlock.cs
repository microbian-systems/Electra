namespace Aero.CMS.Core.Content.Models.Blocks;

public class RegisterBlock : ContentBlock
{
    public static string BlockType => "registerBlock";
    public override string Type => BlockType;

    public string Title
    {
        get => Properties.TryGetValue("title", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["title"] = value;
    }

    public bool RequireEmailConfirmation
    {
        get => Properties.TryGetValue("requireEmailConfirmation", out var value) ? value is bool b && b : true;
        set => Properties["requireEmailConfirmation"] = value;
    }

    public string TermsUrl
    {
        get => Properties.TryGetValue("termsUrl", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["termsUrl"] = value;
    }

    public string PrivacyUrl
    {
        get => Properties.TryGetValue("privacyUrl", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["privacyUrl"] = value;
    }

    public string RedirectUrl
    {
        get => Properties.TryGetValue("redirectUrl", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["redirectUrl"] = value;
    }

    public bool ValidatePasswordStrength
    {
        get => Properties.TryGetValue("validatePasswordStrength", out var value) && value is bool b && b;
        set => Properties["validatePasswordStrength"] = value;
    }

    public int PasswordMinLength
    {
        get => Properties.TryGetValue("passwordMinLength", out var value) && value is int i ? i : 8;
        set => Properties["passwordMinLength"] = value;
    }
}