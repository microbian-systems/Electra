using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Sections;

public partial class PricingSection : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Pricing";

    [Parameter]
    public string Description { get; set; } = "Choose the plan that's right for you.";
}
