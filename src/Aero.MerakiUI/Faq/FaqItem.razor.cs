using Microsoft.AspNetCore.Components;

namespace Aero.MerakiUI.Faq;

public partial class FaqItem : MerakiComponentBase
{
    [Parameter]
    public string Question { get; set; } = "Question";

    [Parameter]
    public string Answer { get; set; } = "Answer";

    [Parameter]
    public bool IsInitiallyOpen { get; set; } = false;
}
