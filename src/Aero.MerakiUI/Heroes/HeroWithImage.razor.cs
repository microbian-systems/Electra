using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Heroes;

public partial class HeroWithImage : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Build your next project";

    [Parameter]
    public string Description { get; set; } = "Lorem ipsum dolor sit amet.";

    [Parameter]
    public string ImageUrl { get; set; } = "";
}
