using System;
using System.Collections.Generic;

namespace Electra.Web.BlogEngine.Models;

public class BlogIndexViewModel
{
    public IEnumerable<ArticleViewModel> Articles { get; set; } = new List<ArticleViewModel>();
    public ArticleViewModel? FeaturedArticle { get; set; }
    
    // Pagination
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    
    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}

public class ArticleViewModel
{
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Slug { get; set; }
    public string? Content { get; set; }
    public string? Excerpt { get; set; }
    public string? Author { get; set; }
    public DateTimeOffset PublishedDate { get; set; }
    public string? ThumbnailUrl { get; set; }
    
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string[] Categories { get; set; } = Array.Empty<string>();
    
    public string? PreviousArticleSlug { get; set; }
    public string? NextArticleSlug { get; set; }
    
    public IEnumerable<ArticleViewModel> RelatedArticles { get; set; } = new List<ArticleViewModel>();
    
    // SEO
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
}
