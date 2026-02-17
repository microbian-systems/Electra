using Aero.MerakiUI.Heroes;
using Bunit;
using Aero.MerakiUI.Heroes;

namespace Aero.MerakiUI.Tests.Heroes;

public class HeroTests : BunitContext
{
    [Fact]
    public void HeroWithImage_ShouldRenderCorrectStructure()
    {
        var cut = Render<HeroWithImage>(parameters => parameters
            .Add(p => p.Title, "Build your next project")
            .Add(p => p.Description, "Lorem ipsum dolor sit amet.")
            .Add(p => p.ImageUrl, "https://example.com/hero.jpg")
        );

        // Verify Title
        var title = cut.Find("h1");
        Assert.Contains("Build your next project", title.TextContent);
        
        // Verify Image
        var img = cut.Find("img");
        Assert.Equal("https://example.com/hero.jpg", img.GetAttribute("src"));
    }

    [Fact]
    public void HeroWithForm_ShouldRenderCorrectStructure()
    {
        var cut = Render<HeroWithForm>(parameters => parameters
            .Add(p => p.Title, "Find your next adventure")
            .Add(p => p.Description, "Search for jobs, events and more.")
        );

        // Verify Title
        Assert.Contains("Find your next adventure", cut.Find("h1").TextContent);
        
        // Verify Form input existence
        var input = cut.Find("input");
        Assert.NotNull(input);
    }
}
