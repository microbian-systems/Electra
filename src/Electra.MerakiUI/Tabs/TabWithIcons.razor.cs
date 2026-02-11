using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Tabs;

public partial class TabWithIcons : MerakiComponentBase
{
    public class TabItem
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
    }

    [Parameter]
    public List<TabItem> Items { get; set; } = new();
}
