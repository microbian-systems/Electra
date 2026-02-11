using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Heroes;

public partial class HeroWithForm : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Title";

    [Parameter]
    public string Description { get; set; } = "";

    [Parameter]
    public string InputPlaceholder { get; set; } = "Email Address";

    [Parameter]
    public string ButtonText { get; set; } = "Subscribe";
}
