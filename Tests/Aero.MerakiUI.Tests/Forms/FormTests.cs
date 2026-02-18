using Aero.MerakiUI.Forms;
using Bunit;
using Aero.MerakiUI.Forms;

namespace Aero.MerakiUI.Tests.Forms;

public class FormTests : BunitContext
{
    [Fact]
    public void ContactForm_ShouldRenderCorrectStructure()
    {
        var cut = Render<ContactForm>(parameters => parameters
            .Add(p => p.Title, "Get in touch")
        );

        // Verify Title
        Assert.Contains("Get in touch", cut.Find("h1").TextContent);
        
        // Verify Inputs (Name, Email, Message)
        Assert.NotNull(cut.Find("input[type='text']"));
        Assert.NotNull(cut.Find("input[type='email']"));
        Assert.NotNull(cut.Find("textarea"));
        
        // Verify Submit Button
        var button = cut.Find("button");
        Assert.NotNull(button);
    }
}
