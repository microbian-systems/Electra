using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Electra.Core.Entities;

namespace Electra.Cms.Areas.Blog.Entities;

/// <summary>
/// Entity representing a comment on a blog post
/// </summary>
public class BlogComment : Entity
{
    /// <summary>
    /// The ID of the blog post this comment belongs to
    /// </summary>
    [Required]
    public string BlogPostId { get; set; } = string.Empty;

    /// <summary>
    /// The blog post this comment belongs to
    /// </summary>
    [ForeignKey(nameof(BlogPostId))]
    public BlogPost? BlogPost { get; set; }

    /// <summary>
    /// The content of the comment
    /// </summary>
    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// The name of the author of the comment
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string AuthorName { get; set; } = string.Empty;

    /// <summary>
    /// The ID of the author if they are a registered user
    /// </summary>
    public string? AuthorId { get; set; }

    /// <summary>
    /// The email of the author
    /// </summary>
    [MaxLength(200)]
    [EmailAddress]
    public string? Email { get; set; }

    /// <summary>
    /// Indicates whether the comment is approved
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// The ID of the parent comment if this is a reply
    /// </summary>
    public string? ParentId { get; set; }

    /// <summary>
    /// The parent comment if this is a reply
    /// </summary>
    [ForeignKey(nameof(ParentId))]
    public BlogComment? Parent { get; set; }

    /// <summary>
    /// Number of likes for this comment
    /// </summary>
    public int Likes { get; set; } = 0;
    
    /// <summary>
    /// Total number of reactions on the comment.
    /// </summary>
    public int TotalReactions { get; set; } = 0;
    
    /// <summary>
    /// A unique string identifying the comment. Used as element id in the DOM.
    /// </summary>
    public string? Stamp { get; set; }
    
    /// <summary>
    /// The date the comment was created.
    /// </summary>
    public DateTimeOffset DateAdded { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Replies to this comment
    /// </summary>
    public List<BlogComment> Replies { get; set; } = [];
}
