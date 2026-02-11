using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Modals;

public partial class ModalWithAction : MerakiComponentBase
{
    [Parameter]
    public string TriggerText { get; set; } = "Open Modal";
}
