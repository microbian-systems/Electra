using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Dropdowns;

public partial class SimpleDropdown : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Dropdown";
}
