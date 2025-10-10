using Electra.Common.Web;
using Electra.Web.BlogEngine.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Controllers;

/// <summary>
/// Controller for blog operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BlogController : ApiControllerBase
{
    private readonly IBlogService blogService;
    private readonly ILogger<BlogController> log;

    public BlogController(IBlogService blogService, ILogger<BlogController> log) : base(log)
    {
        this.log = log;
        this.blogService = blogService;
    }

    /// <summary>
    /// Gets paginated blog posts
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlogs(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] bool publishedOnly = true)
    {
        try
        {
            var result = await blogService.GetPaginatedBlogsAsync(pageNumber, pageSize, publishedOnly);
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving paginated blogs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets latest blog posts
    /// </summary>
    [HttpGet("latest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestBlogs([FromQuery] int count = 5)
    {
        try
        {
            var blogs = await blogService.GetLatestBlogsAsync(count);
            return Ok(blogs);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving latest blogs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets featured blog posts
    /// </summary>
    [HttpGet("featured")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeaturedBlogs([FromQuery] int count = 5)
    {
        try
        {
            var blogs = await blogService.GetFeaturedBlogsAsync(count);
            return Ok(blogs);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving featured blogs");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a blog post by ID
    /// </summary>
    [HttpGet("{id:long}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlog(long id)
    {
        try
        {
            var blog = await blogService.GetBlogByIdAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            // Increment view count
            await blogService.IncrementViewCountAsync(id);

            return Ok(blog);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving blog: {BlogId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets a blog post by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlogBySlug(string slug)
    {
        try
        {
            var blog = await blogService.GetBlogBySlugAsync(slug);
            if (blog == null)
            {
                return NotFound();
            }

            // Increment view count
            await blogService.IncrementViewCountAsync(blog.Id);

            return Ok(blog);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving blog by slug: {Slug}", slug);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets blog content as HTML
    /// </summary>
    [HttpGet("{id:long}/html")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlogHtml(long id)
    {
        try
        {
            var blog = await blogService.GetBlogByIdAsync(id);
            if (blog == null)
            {
                return NotFound();
            }

            var html = await blogService.GetContentAsHtmlAsync(blog);
            return Ok(new { html });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving blog HTML: {BlogId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Searches blog posts
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> SearchBlogs(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await blogService.SearchBlogsAsync(searchTerm, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error searching blogs with term: {SearchTerm}", searchTerm);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets blog posts by tag
    /// </summary>
    [HttpGet("tag/{tag}")]
    [AllowAnonymous]
    
    public async Task<IActionResult> GetBlogsByTag(
        string tag,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await blogService.GetBlogsByTagAsync(tag, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving blogs by tag: {Tag}", tag);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets blog posts by author
    /// </summary>
    [AllowAnonymous]
    [HttpGet("author/{author}")]
    public async Task<IActionResult> GetBlogsByAuthor(
        string author,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var result = await blogService.GetBlogsByAuthorAsync(author, pageNumber, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving blogs by author: {Author}", author);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Gets all tags
    /// </summary>
    [AllowAnonymous]
    [HttpGet("tags")]
    public async Task<IActionResult> GetAllTags()
    {
        try
        {
            var tags = await blogService.GetAllTagsAsync();
            return Ok(tags);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error retrieving all tags");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Creates a new blog post
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateBlog([FromBody] Entities.BlogEntry blog)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdBlog = await blogService.AddBlogAsync(blog);
            return CreatedAtAction(nameof(GetBlog), new { id = createdBlog.Id }, createdBlog);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error creating blog");
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Updates an existing blog post
    /// </summary>
    [HttpPut("{id:long}")]
    public async Task<IActionResult> UpdateBlog(long id, [FromBody] Entities.BlogEntry blog)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (id != blog.Id)
            {
                return BadRequest("ID mismatch");
            }

            var updatedBlog = await blogService.UpdateBlogAsync(blog);
            return Ok(updatedBlog);
        }
        catch (InvalidOperationException)
        {
            return NotFound();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error updating blog: {BlogId}", id);
            return StatusCode(500, "Internal server error");
        }
    }

    /// <summary>
    /// Deletes a blog post
    /// </summary>
    [HttpDelete("{id:long}")]
    public async Task<IActionResult> DeleteBlog(long id)
    {
        try
        {
            var result = await blogService.DeleteBlogAsync(id);
            if (!result)
            {
                return NotFound();
            }

            return NoContent();
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error deleting blog: {BlogId}", id);
            return StatusCode(500, "Internal server error");
        }
    }
}