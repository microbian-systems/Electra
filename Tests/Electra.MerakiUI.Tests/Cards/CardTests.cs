using Bunit;
using Electra.MerakiUI.Cards;
using Xunit;

namespace Electra.MerakiUI.Tests.Cards;

public class CardTests : BunitContext
{
    [Fact]
    public void ProductCard_ShouldRenderCorrectStructure()
    {
        var cut = Render<ProductCard>(parameters => parameters
            .Add(p => p.Title, "NIKE Air")
            .Add(p => p.Price, "$129")
            .Add(p => p.ImageUrl, "https://example.com/image.jpg")
        );

        // Verify Title
        var title = cut.Find("h3");
        Assert.Contains("NIKE Air", title.TextContent);
        
        // Verify Price
        var price = cut.Find("span");
        Assert.Contains("$129", price.TextContent);
        
        // Verify Image div style
        var imageDiv = cut.Find("div.bg-cover");
        Assert.Contains("background-image: url(https://example.com/image.jpg)", imageDiv.GetAttribute("style"));
    }

    [Fact]
    public void ArticleCard_ShouldRenderCorrectStructure()
    {
        var cut = Render<ArticleCard>(parameters => parameters
            .Add(p => p.Category, "Design")
            .Add(p => p.Title, "Accessibility in UI")
            .Add(p => p.Description, "Learn how to build accessible components.")
            .Add(p => p.Date, "21 Oct 2023")
        );

        // Verify Category
        Assert.Contains("Design", cut.Find("span.text-blue-600").TextContent);
        
        // Verify Title
        Assert.Contains("Accessibility in UI", cut.Find("a.text-gray-800").TextContent);
        
        // Verify Date
        Assert.Contains("21 Oct 2023", cut.Find("span.text-gray-600").TextContent);
    }
}
