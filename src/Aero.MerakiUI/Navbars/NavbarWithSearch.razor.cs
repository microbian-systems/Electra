using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Navbars;

public partial class NavbarWithSearch : MerakiComponentBase
{
    [Parameter]
    public string? BrandName { get; set; }

    [Parameter]
    public string? BrandHref { get; set; } = "/";

    [Parameter]
    public RenderFragment? Links { get; set; }

    [Parameter]
    public string? SearchPlaceholder { get; set; } = "Search";

    protected bool IsMobileMenuOpen { get; set; }

    protected void ToggleMobileMenu()
    {
        IsMobileMenuOpen = !IsMobileMenuOpen;
    }
}
