using Bunit;
using Electra.MerakiUI.Sidebars;

namespace Electra.MerakiUI.Tests.Sidebars;

public class SidebarTests : BunitContext
{
    [Fact]
    public void SimpleSidebar_ShouldRenderCorrectStructure()
    {
        var cut = Render<SimpleSidebar>(parameters => parameters
            .Add(p => p.BrandName, "Electra")
            .AddChildContent("<nav>Links</nav>")
        );

        // Verify Brand Name
        Assert.Contains("Electra", cut.Find("h2").TextContent);
        
        // Verify Child Content
        Assert.Contains("Links", cut.Markup);
    }
}
