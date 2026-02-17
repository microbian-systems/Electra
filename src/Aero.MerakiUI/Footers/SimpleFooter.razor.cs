using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Footers;

public partial class SimpleFooter : MerakiComponentBase
{
    [Parameter]
    public string BrandName { get; set; } = "Brand";
}
