using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Footers;

public partial class SimpleFooter : MerakiComponentBase
{
    [Parameter]
    public string BrandName { get; set; } = "Brand";
}
