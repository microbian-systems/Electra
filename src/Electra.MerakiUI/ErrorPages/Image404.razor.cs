using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.ErrorPages;

public partial class Image404 : MerakiComponentBase
{
    [Parameter]
    public string ErrorCode { get; set; } = "404 error";

    [Parameter]
    public string Title { get; set; } = "Page not found";

    [Parameter]
    public string Description { get; set; } = "Sorry, the page you are looking for doesn't exist. Here are some helpful links:";

    [Parameter]
    public string ImageUrl { get; set; } = "https://images.unsplash.com/photo-1613310023042-ad79320c00ff?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxwaG90by1wYWdlfHx8fGVufDB8fHx8&auto=format&fit=crop&w=2070&q=80";

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
