using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.ErrorPages;

public partial class Simple404 : MerakiComponentBase
{
    [Parameter]
    public string ErrorCode { get; set; } = "404 error";

    [Parameter]
    public string Title { get; set; } = "We can’t find that page";

    [Parameter]
    public string Description { get; set; } = "Sorry, the page you are looking for doesn't exist or has been moved.";

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
