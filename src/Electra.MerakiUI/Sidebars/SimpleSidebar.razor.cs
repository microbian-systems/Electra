using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Sidebars;

public partial class SimpleSidebar : MerakiComponentBase
{
    [Parameter]
    public string BrandName { get; set; } = "Brand";
}
