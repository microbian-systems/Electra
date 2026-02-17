using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Tabs;

public partial class SimpleTabs : MerakiComponentBase
{
    [Parameter]
    public string[] Tabs { get; set; } = Array.Empty<string>();
}
