using Bunit;
using Electra.MerakiUI.ErrorPages;
using Xunit;

namespace Electra.MerakiUI.Tests.ErrorPages;

public class ErrorPagesTests : BunitContext
{
    [Fact]
    public void Simple404_ShouldRender()
    {
        var cut = Render<Simple404>(parameters => parameters
            .Add(p => p.Title, "Page Not Found")
        );

        Assert.Contains("Page Not Found", cut.Markup);
    }

    [Fact]
    public void Centered404_ShouldRender()
    {
        var cut = Render<Centered404>(parameters => parameters
            .Add(p => p.Title, "Centered Not Found")
        );

        Assert.Contains("Centered Not Found", cut.Markup);
    }

    [Fact]
    public void Illustration404_ShouldRender()
    {
        var cut = Render<Illustration404>(parameters => parameters
            .Add(p => p.IllustrationUrl, "test.svg")
        );

        Assert.Contains("src=\"test.svg\"", cut.Markup);
    }

    [Fact]
    public void Image404_ShouldRender()
    {
        var cut = Render<Image404>(parameters => parameters
            .Add(p => p.ImageUrl, "test.jpg")
        );

        Assert.Contains("src=\"test.jpg\"", cut.Markup);
    }
}
