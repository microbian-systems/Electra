using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.ErrorPages;

public partial class Illustration404 : MerakiComponentBase
{
    [Parameter]
    public string ErrorCode { get; set; } = "404 error";

    [Parameter]
    public string Title { get; set; } = "Page not found";

    [Parameter]
    public string Description { get; set; } = "Sorry, the page you are looking for doesn't exist. Here are some helpful links:";

    [Parameter]
    public string IllustrationUrl { get; set; } = "/images/components/illustration.svg";

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
