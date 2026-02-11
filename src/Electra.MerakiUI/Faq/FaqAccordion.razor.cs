using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Faq;

public partial class FaqAccordion : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "FAQ's";
}
