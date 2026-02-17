using Aero.Cms.Areas.Blog.Entities;

namespace Aero.Cms.Areas.Blog.Models;

public class BlogArticleViewModel : BlogPost
{
    public string Subtitle { get; set; } = string.Empty;
    public string Brief { get; set; } = string.Empty;
    public string CoverImageUrl { get; set; } = string.Empty;
    public string AuthorName { get; set; } = string.Empty;
    public string AuthorProfilePicture { get; set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; set; }
    public string ContentHtml { get; set; } = string.Empty;
}

public class BlogListViewModel
{
    public string PublicationTitle { get; set; } = string.Empty;
    public List<BlogArticleViewModel> Posts { get; set; } = new();
}
