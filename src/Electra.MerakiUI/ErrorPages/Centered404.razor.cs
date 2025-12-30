using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.ErrorPages;

public partial class Centered404 : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Page not found";

    [Parameter]
    public string Description { get; set; } = "The page you are looking for doesn't exist. Here are some helpful links:";

    [Parameter]
    public RenderFragment? IconContent { get; set; }

    [Parameter]
    public bool ShowBackButton { get; set; } = true;

    [Parameter]
    public string BackButtonText { get; set; } = "Go back";

    [Parameter]
    public bool ShowHomeButton { get; set; } = true;

    [Parameter]
    public string HomeButtonText { get; set; } = "Take me home";

    [Parameter]
    public EventCallback OnBackClick { get; set; }

    [Parameter]
    public EventCallback OnHomeClick { get; set; }
}
