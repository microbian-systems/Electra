using Bunit;
using Electra.MerakiUI.Footers;

namespace Electra.MerakiUI.Tests.Footers;

public class FooterTests : BunitContext
{
    [Fact]
    public void SimpleFooter_ShouldRenderCorrectStructure()
    {
        var cut = Render<SimpleFooter>(parameters => parameters
            .Add(p => p.BrandName, "Electra")
            .AddChildContent("<nav>Links</nav>")
        );

        // Verify Brand Name
        Assert.Contains("Electra", cut.Find("a").TextContent);
        
        // Verify Child Content
        Assert.Contains("Links", cut.Markup);
    }
}
