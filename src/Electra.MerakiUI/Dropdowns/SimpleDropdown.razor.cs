using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Dropdowns;

public partial class SimpleDropdown : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Dropdown";
}
