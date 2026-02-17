using Aero.MerakiUI.Tooltips;
using Bunit;
using Aero.MerakiUI.Tooltips;

namespace Aero.MerakiUI.Tests.Tooltips;

public class TooltipTests : BunitContext
{
    [Fact]
    public void Tooltip_ShouldRenderText()
    {
        var cut = Render<Tooltip>(parameters => parameters
            .Add(p => p.Text, "Hint info")
        );

        Assert.Contains("Hint info", cut.Markup);
    }
}
