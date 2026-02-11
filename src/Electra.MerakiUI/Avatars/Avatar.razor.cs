using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Avatars;

public partial class Avatar : MerakiComponentBase
{
    [Parameter]
    public string Src { get; set; } = "https://images.unsplash.com/photo-1531746020798-e6953c6e8e04?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=764&h=764&q=100";

    [Parameter]
    public string Alt { get; set; } = "Avatar";

    [Parameter]
    public AvatarSize Size { get; set; } = AvatarSize.Md;

    [Parameter]
    public AvatarShape Shape { get; set; } = AvatarShape.Circle;

    [Parameter]
    public bool HasBorder { get; set; } = false;

    [Parameter]
    public bool ShowStatus { get; set; } = false;

    [Parameter]
    public string StatusColor { get; set; } = "bg-emerald-500";

    [CascadingParameter(Name = "IsGrouped")]
    public bool IsGrouped { get; set; }

    [CascadingParameter(Name = "GroupSize")]
    public AvatarSize? GroupSizeCascaded { get; set; }

    private string GetSizeClass()
    {
        var size = (IsGrouped && GroupSizeCascaded.HasValue) ? GroupSizeCascaded.Value : Size;
        return size switch
        {
            AvatarSize.Xs => "w-6 h-6",
            AvatarSize.Sm => "w-8 h-8",
            AvatarSize.Md => "w-10 h-10",
            AvatarSize.Lg => "w-12 h-12",
            AvatarSize.Xl => "w-16 h-16",
            AvatarSize.TwoXl => "w-20 h-20",
            _ => "w-10 h-10"
        };
    }

    private string GetShapeClass() => Shape switch
    {
        AvatarShape.Circle => "rounded-full",
        AvatarShape.Square => "rounded-lg",
        _ => "rounded-full"
    };

    private string GetBorderClass() => (HasBorder || IsGrouped) ? "ring ring-white dark:ring-gray-900" : "";

    private string GetGroupClass() => IsGrouped ? "-mx-1" : "";

    private string GetStatusSizeClass()
    {
        var size = (IsGrouped && GroupSizeCascaded.HasValue) ? GroupSizeCascaded.Value : Size;
        return size switch
        {
            AvatarSize.Xs => "w-1.5 h-1.5",
            AvatarSize.Sm => "w-2 h-2",
            AvatarSize.Md => "w-2.5 h-2.5",
            AvatarSize.Lg => "w-2.5 h-2.5",
            AvatarSize.Xl => "w-3 h-3",
            AvatarSize.TwoXl => "w-4 h-4",
            _ => "w-2.5 h-2.5"
        };
    }

    private string GetStatusPositionClass()
    {
        var size = (IsGrouped && GroupSizeCascaded.HasValue) ? GroupSizeCascaded.Value : Size;
        return size switch
        {
            AvatarSize.Xs => "right-0.5 bottom-0",
            AvatarSize.Sm => "right-0.5 bottom-0",
            AvatarSize.Md => "right-0.5 bottom-0",
            AvatarSize.Lg => "right-1 bottom-0",
            AvatarSize.Xl => "right-1 bottom-0",
            AvatarSize.TwoXl => "right-1.5 bottom-0",
            _ => "right-0.5 bottom-0"
        };
    }

    private string GetStatusColorClass() => StatusColor;
}

public enum AvatarSize
{
    Xs,
    Sm,
    Md,
    Lg,
    Xl,
    TwoXl
}

public enum AvatarShape
{
    Circle,
    Square
}
