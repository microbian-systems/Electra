using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Navbars;

public partial class NavbarLink : MerakiComponentBase
{
    [Parameter]
    public string? Href { get; set; } = "#";

    [Parameter]
    public bool IsActive { get; set; }
}
