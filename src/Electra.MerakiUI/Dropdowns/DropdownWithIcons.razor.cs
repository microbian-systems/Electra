using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Dropdowns;

public partial class DropdownWithIcons : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Dropdown";
}
