using Bunit;
using Electra.MerakiUI.Sections;

namespace Electra.MerakiUI.Tests.Sections;

public class SectionTests : BunitContext
{
    [Fact]
    public void PricingSection_ShouldRenderCorrectStructure()
    {
        var cut = Render<PricingSection>(parameters => parameters
            .Add(p => p.Title, "Our Pricing")
        );

        // Verify Title
        Assert.Contains("Our Pricing", cut.Find("h1").TextContent);
        
        // Verify at least one pricing card exists
        var cards = cut.FindAll("div.flex.flex-col");
        Assert.True(cards.Count > 0);
    }
}
