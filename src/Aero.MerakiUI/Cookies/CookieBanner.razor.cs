using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Cookies;

public partial class CookieBanner : MerakiComponentBase
{
    [Parameter]
    public string Message { get; set; } = "We use cookies to ensure that we give you the best experience on our website.";

    [Parameter]
    public string PolicyUrl { get; set; } = "#";

    [Parameter]
    public string PolicyLinkText { get; set; } = "Read cookies policies";

    [Parameter]
    public string SettingsButtonText { get; set; } = "Cookie Setting";

    [Parameter]
    public string AcceptButtonText { get; set; } = "Accept All Cookies";

    [Parameter]
    public RenderFragment? IconContent { get; set; }

    [Parameter]
    public EventCallback OnSettingsClick { get; set; }

    [Parameter]
    public EventCallback OnAcceptClick { get; set; }
}
