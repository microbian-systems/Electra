using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Modals;

public partial class ModalWithAction : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Open Modal";
}
