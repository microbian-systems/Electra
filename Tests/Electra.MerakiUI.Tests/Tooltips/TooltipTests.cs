using Bunit;
using Electra.MerakiUI.Tooltips;
using Xunit;

namespace Electra.MerakiUI.Tests.Tooltips;

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
