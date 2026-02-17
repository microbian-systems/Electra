using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Sections;

public partial class CTASection : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Bring your Business to the next level.";

    [Parameter]
    public string? Description { get; set; }

    [Parameter]
    public string ButtonText { get; set; } = "Sign Up";

    [Parameter]
    public string ButtonUrl { get; set; } = "#";
}
