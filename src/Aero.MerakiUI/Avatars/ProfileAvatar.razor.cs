using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Avatars;

public partial class ProfileAvatar : MerakiComponentBase
{
    [Parameter]
    public string Name { get; set; } = "John Doe";

    [Parameter]
    public string Email { get; set; } = "john@example.com";

    [Parameter]
    public string Src { get; set; } = "https://images.unsplash.com/photo-1544005313-94ddf0286df2?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=faceare&facepad=3&w=688&h=688&q=100";

    [Parameter]
    public string Alt { get; set; } = "Profile Avatar";

    [Parameter]
    public AvatarSize Size { get; set; } = AvatarSize.Md;

    [Parameter]
    public AvatarShape Shape { get; set; } = AvatarShape.Circle;

    private string GetNameSizeClass() => Size switch
    {
        AvatarSize.Xs => "text-xs",
        AvatarSize.Sm => "text-base",
        AvatarSize.Md => "text-lg",
        AvatarSize.Lg => "text-xl",
        AvatarSize.Xl => "text-xl",
        AvatarSize.TwoXl => "text-2xl",
        _ => "text-lg"
    };

    private string GetEmailSizeClass() => Size switch
    {
        AvatarSize.Xs => "text-[10px]",
        AvatarSize.Sm => "text-xs",
        AvatarSize.Md => "text-sm",
        AvatarSize.Lg => "text-sm",
        AvatarSize.Xl => "text-base",
        AvatarSize.TwoXl => "text-lg",
        _ => "text-sm"
    };
}
