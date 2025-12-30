using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Navbars;

public partial class SimpleNavbar : MerakiComponentBase
{
    [Parameter]
    public string? BrandName { get; set; }

    [Parameter]
    public string? BrandHref { get; set; } = "/";

    [Parameter]
    public RenderFragment? Links { get; set; }

    protected bool IsMobileMenuOpen { get; set; }

    protected void ToggleMobileMenu()
    {
        IsMobileMenuOpen = !IsMobileMenuOpen;
    }
}
