namespace Aero.CMS.Core.Content.Models.Blocks;

public class ForgotPasswordBlock : ContentBlock
{
    public static string BlockType => "forgotPasswordBlock";
    public override string Type => BlockType;

    public string Title
    {
        get => Properties.TryGetValue("title", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["title"] = value;
    }

    public string SuccessMessage
    {
        get => Properties.TryGetValue("successMessage", out var value) ? value?.ToString() ?? "Check your email for password reset instructions." : "Check your email for password reset instructions.";
        set => Properties["successMessage"] = value;
    }

    public string ErrorMessage
    {
        get => Properties.TryGetValue("errorMessage", out var value) ? value?.ToString() ?? "An error occurred. Please try again." : "An error occurred. Please try again.";
        set => Properties["errorMessage"] = value;
    }

    public string EmailFieldLabel
    {
        get => Properties.TryGetValue("emailFieldLabel", out var value) ? value?.ToString() ?? "Email Address" : "Email Address";
        set => Properties["emailFieldLabel"] = value;
    }
}