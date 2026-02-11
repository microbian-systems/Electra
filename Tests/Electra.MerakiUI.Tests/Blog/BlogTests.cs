using Bunit;
using Electra.MerakiUI.Blog;

namespace Electra.MerakiUI.Tests.Blog;

public class BlogTests : BunitContext
{
    [Fact]
    public void BlogCard_ShouldRender()
    {
        var cut = Render<BlogCard>(parameters => parameters
            .Add(p => p.Title, "Awesome Post")
        );

        Assert.Contains("Awesome Post", cut.Markup);
    }

    [Fact]
    public void BlogSection_ShouldRenderTitle()
    {
        var cut = Render<BlogSection>(parameters => parameters
            .Add(p => p.Title, "Latest News")
        );

        Assert.Contains("Latest News", cut.Markup);
    }
}
