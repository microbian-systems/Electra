using Aero.MerakiUI.Navbars;
using Bunit;
using Aero.MerakiUI.Navbars;

namespace Aero.MerakiUI.Tests.Navbars;

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
