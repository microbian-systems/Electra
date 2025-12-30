using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Sections;

public partial class FeatureItem : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Feature Title";

    [Parameter]
    public string Description { get; set; } = "Feature description goes here.";

    [Parameter]
    public RenderFragment? IconContent { get; set; }
}
