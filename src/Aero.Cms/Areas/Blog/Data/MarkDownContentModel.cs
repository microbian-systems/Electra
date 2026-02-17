namespace Aero.Cms.Areas.Blog.Data;

public record MarkDownContentModel(string title, DateTimeOffset publishedAt, string slug, string[] tags, string imageUrl, string content)
{
    /// <summary>
    /// The series (name) this post belongs to
    /// </summary>
    public string? series { get; set; }
}

