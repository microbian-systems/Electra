using Aero.MerakiUI.Portfolio;
using Bunit;
using Aero.MerakiUI.Portfolio;

namespace Aero.MerakiUI.Tests.Portfolio;

public class PortfolioTests : BunitContext
{
    [Fact]
    public void PortfolioCard_ShouldRenderTitle()
    {
        var cut = Render<PortfolioCard>(parameters => parameters
            .Add(p => p.Title, "Project X")
        );

        Assert.Contains("Project X", cut.Markup);
    }
}
