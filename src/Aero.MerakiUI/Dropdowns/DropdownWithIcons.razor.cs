using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Dropdowns;

public partial class DropdownWithIcons : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Dropdown";
}
