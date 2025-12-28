using Electra.Web.BlogEngine.Entities;
using Electra.Web.BlogEngine.Services;
using Electra.Web.Core.Controllers;
using LanguageExt;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Electra.Web.BlogEngine.Controllers;

/// <summary>
/// Controller for blog operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class BlogAdminController(IBlogService blogService, ILogger<BlogAdminController> log) : ElectraApiBaseController(log)
{
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
    [HttpGet("{id:string}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlog(string id)
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
            
            var result = blog.Match<IActionResult>(
                Some:  b =>
                {
                    blogService.IncrementViewCountAsync(b.Id);
                    return Ok(b);
                },
                None: NotFound);

            return result;
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
    [HttpGet("{id:string}/html")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBlogHtml(string id)
    {
        try
        {
            var blog = await blogService.GetBlogByIdAsync(id);
            if(blog.IsNone)
                return NotFound();

            var b = (BlogEntry)blog;
            
            var html = await blogService.GetContentAsHtmlAsync(b);
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
    public async Task<IActionResult> CreateBlog([FromBody] BlogEntry blog)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var createdBlog = await blogService.AddBlogAsync(blog);
            var res =  createdBlog.Match<IActionResult>(
                Some: b => CreatedAtAction(nameof(CreateBlog), new { id = b.Id }, b),
                None: BadRequest(blog));

            return res;
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
    [HttpPut("{id:tring}")]
    public async Task<IActionResult> UpdateBlog(string id, [FromBody] BlogEntry blog)
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
    [HttpDelete("{id:string}")]
    public async Task<IActionResult> DeleteBlog(string id)
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