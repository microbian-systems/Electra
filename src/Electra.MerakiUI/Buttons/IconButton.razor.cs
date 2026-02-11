using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Buttons;

public partial class IconButton : MerakiComponentBase
{
    [Parameter]
    public RenderFragment? IconContent { get; set; }
}
