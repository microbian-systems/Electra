using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Forms;

public partial class ContactForm : MerakiComponentBase
{
    [Parameter]
    public string Title { get; set; } = "Contact Us";

    [Parameter]
    public string Description { get; set; } = "Fill out the form below to get in touch.";
}
