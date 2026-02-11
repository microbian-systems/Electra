using Bunit;
using Electra.MerakiUI.Areas.Demo.Pages;

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
        Assert.Contains("Skeletons", cut.Markup);
        Assert.Contains("Tooltips", cut.Markup);
        Assert.Contains("Cards", cut.Markup);
        Assert.Contains("Blog Sections", cut.Markup);
        Assert.Contains("Portfolio", cut.Markup);
        Assert.Contains("Testimonials", cut.Markup);
        Assert.Contains("FAQ", cut.Markup);
        Assert.Contains("Cookie Banners", cut.Markup);
        Assert.Contains("Interactive Elements", cut.Markup);
        Assert.Contains("Marketing Sections", cut.Markup);
        Assert.Contains("Auth & Forms", cut.Markup);
        Assert.Contains("Specialized Layouts", cut.Markup);
        Assert.Contains("Email Templates", cut.Markup);
    }
}
