using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Buttons;

public partial class IconButton : MerakiComponentBase
{
    [Parameter]
    public RenderFragment? IconContent { get; set; }
}
