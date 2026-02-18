using Aero.MerakiUI.Buttons;
using Bunit;
using Aero.MerakiUI.Buttons;

namespace Aero.MerakiUI.Tests.Buttons;

public class ButtonTests : BunitContext
{
    [Fact]
    public void PrimaryButton_ShouldRenderCorrectClasses()
    {
        var cut = Render<PrimaryButton>(parameters => parameters
            .AddChildContent("Click Me")
        );

        cut.Find("button.bg-blue-600");
        Assert.Contains("Click Me", cut.Markup);
    }
}
