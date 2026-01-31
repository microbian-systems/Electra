using System.ComponentModel.DataAnnotations;
using Electra.Core;
using Electra.Core.Entities;
using Electra.Web.BlogEngine.Enums;

namespace Electra.Web.BlogEngine.Entities;

/// <summary>
/// Blog entity representing a blog post
/// </summary>
public class BlogEntry : Entity
{
    [MaxLength(5)]
    public string Culture { get; set; } = "en-US";
    /// <summary>
    /// Title of the blog post
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Short description/summary of the blog post
    /// </summary>
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Raw content of the blog post
    /// </summary>
    [Required]
    [MaxLength(2048)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Type of content format (Markdown, HTML, JSON)
    /// </summary>
    public ContentType ContentType { get; set; } = ContentType.Markdown;

    /// <summary>
    /// Tags associated with the blog post
    /// </summary>
    public string[] Tags { get; set; } = [];

    /// <summary>
    /// Authors of the blog post
    /// </summary>
    public string[] Authors { get; set; } = [];

    /// <summary>
    /// Inline SVG data for the blog post image
    /// </summary>
    public string? SvgData { get; set; }

    /// <summary>
    /// URL to an image for the blog post (used when not using SVG)
    /// </summary>
    [MaxLength(1024)]
    public string? ImageUrl { get; set; }

    /// <summary>
    /// Indicates whether the blog post is published
    /// </summary>
    public bool IsPublished { get; set; } = false;

    /// <summary>
    /// Indicates whether the blog post is a draft
    /// </summary>
    public bool IsDraft { get; set; } = true;

    /// <summary>
    /// Date and time when the blog post was created
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTimeOffset PublishDate { get; set; } = DateTimeOffset.MaxValue; 

    /// <summary>
    /// Date and time when the blog post was last updated
    /// </summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public bool ApprovalRequired { get; set; }
    public long ApprovedBy { get; set; } = 0;
    public DateTimeOffset ApprovedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Slug for SEO-friendly URLs (generated from title)
    /// </summary>
    [MaxLength(250)]
    public string Slug { get; set; } = string.Empty;

    /// <summary>
    /// View count for the blog post
    /// </summary>
    public int ViewCount { get; set; } = 0;

    /// <summary>
    /// Featured flag for highlighting important posts
    /// </summary>
    public bool IsFeatured { get; set; } = false;
    public int Likes { get; set; } = 0;
    public int Dislikes { get; set; } = 0;
    public int Comments { get; set; } = 0;
    public int Shares { get; set; } = 0;
    public int Reads { get; set; } = 0;
    public int Saves { get; set; } = 0;
    public int Prints { get; set; } = 0;
    public int Copies { get; set; } = 0; 
}