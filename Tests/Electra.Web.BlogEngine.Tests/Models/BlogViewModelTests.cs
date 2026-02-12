using Electra.Cms.Areas.Blog.Models;

namespace Electra.Web.BlogEngine.Tests.Models;

public class BlogViewModelTests
{
    [Fact]
    public void BlogIndexViewModel_ShouldHaveRequiredProperties()
    {
        // This test will fail compilation initially because the properties don't exist yet
        var model = new BlogIndexViewModel();
        
        Assert.NotNull(model.Articles);
        Assert.Null(model.FeaturedArticle);
        
        // Pagination
        Assert.Equal(0, model.CurrentPage);
        Assert.Equal(0, model.TotalPages);
        Assert.Equal(0, model.PageSize);
        
        // SEO
        Assert.Null(model.MetaTitle);
        Assert.Null(model.MetaDescription);
    }

    [Fact]
    public void ArticleViewModel_ShouldHaveRequiredProperties()
    {
        // This test will fail compilation initially
        var model = new ArticleViewModel();
        
        Assert.Null(model.Title);
        Assert.Null(model.Content);
        Assert.Null(model.Author);
        Assert.Equal(default(DateTimeOffset), model.PublishedDate);
        
        Assert.NotNull(model.Tags);
        Assert.NotNull(model.Categories);
        
        Assert.Null(model.PreviousArticleSlug);
        Assert.Null(model.NextArticleSlug);
        
        Assert.NotNull(model.RelatedArticles);
        
        // SEO
        Assert.Null(model.MetaTitle);
        Assert.Null(model.MetaDescription);
    }
}
