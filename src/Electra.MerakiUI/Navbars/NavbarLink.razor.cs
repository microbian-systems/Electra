using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Navbars;

public partial class NavbarLink : MerakiComponentBase
{
    [Parameter]
    public string? Href { get; set; } = "#";
}
