using Bunit;
using Electra.MerakiUI.Pagination;

namespace Electra.MerakiUI.Tests.Pagination;

public class PaginationTests : BunitContext
{
    [Fact]
    public void Pagination_ShouldRenderPages()
    {
        var cut = Render<PaginationControl>(parameters => parameters
            .Add(p => p.TotalPages, 3)
            .Add(p => p.CurrentPage, 1)
        );

        Assert.Contains("1", cut.Markup);
        Assert.Contains("2", cut.Markup);
        Assert.Contains("3", cut.Markup);
    }
}
