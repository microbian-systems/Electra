using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Avatars;

public partial class AvatarGroup : MerakiComponentBase
{
    [Parameter]
    public AvatarSize GroupSize { get; set; } = AvatarSize.Sm;
}
