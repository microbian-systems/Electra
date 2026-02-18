using Aero.MerakiUI.Inputs;
using Bunit;
using Aero.MerakiUI.Inputs;

namespace Aero.MerakiUI.Tests.Inputs;

public class InputTests : BunitContext
{
    [Fact]
    public void TextInput_ShouldRenderCorrectClasses()
    {
        var cut = Render<TextInput>(parameters => parameters
            .Add(p => p.Label, "Full Name")
            .Add(p => p.Placeholder, "John Doe")
        );

        cut.Find("input");
        Assert.Contains("Full Name", cut.Markup);
        Assert.Contains("John Doe", cut.Markup);
    }
}
