using Bunit;
using Electra.MerakiUI.Navbars;

namespace Electra.MerakiUI.Tests.Navbars;

public class NavbarTests : BunitContext
{
    [Fact]
    public void SimpleNavbar_ShouldRenderCorrectClasses()
    {
        var cut = Render<SimpleNavbar>(parameters => parameters
            .Add(p => p.BrandName, "MyBrand")
        );

        Assert.Contains("MyBrand", cut.Markup);
        cut.Find("nav");
    }
}
