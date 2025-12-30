using Bunit;
using Electra.MerakiUI.Areas.Demo.Pages;
using Xunit;

namespace Electra.MerakiUI.Tests.Areas.Demo.Pages;

public class MerakiShowcaseTests : BunitContext
{
    [Fact]
    public void MerakiShowcase_ShouldRenderMainSections()
    {
        var cut = Render<MerakiShowcase>();

        Assert.Contains("Navbars", cut.Markup);
        Assert.Contains("Buttons", cut.Markup);
        Assert.Contains("Alerts", cut.Markup);
        Assert.Contains("Avatars", cut.Markup);
        Assert.Contains("Sidebars", cut.Markup);
        Assert.Contains("Footers", cut.Markup);
        Assert.Contains("Breadcrumbs", cut.Markup);
        Assert.Contains("Pagination", cut.Markup);
    }
}
