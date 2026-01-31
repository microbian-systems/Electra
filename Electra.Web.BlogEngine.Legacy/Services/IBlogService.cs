using Electra.Web.BlogEngine.Models;

namespace Electra.Web.BlogEngine.Services;

/// <summary>
/// Interface for blog service operations
/// </summary>
public interface IBlogService
{
    /// <summary>
    /// Gets the latest published blog posts
    /// </summary>
    /// <param name="count">Number of blogs to retrieve</param>
    /// <returns>Collection of latest blog posts</returns>
    Task<IEnumerable<Entities.BlogEntry>> GetLatestBlogsAsync(int count);

    /// <summary>
    /// Gets paginated blog posts
    /// </summary>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <param name="publishedOnly">Whether to include only published posts</param>
    /// <returns>Paginated result of blog posts</returns>
    Task<PaginatedResult<Entities.BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true);

    /// <summary>
    /// Gets a blog post by its ID
    /// </summary>
    /// <param name="id">Blog post ID</param>
    /// <returns>Blog post if found, null otherwise</returns>
    Task<Entities.BlogEntry?> GetBlogByIdAsync(long id);

    /// <summary>
    /// Gets a blog post by its slug
    /// </summary>
    /// <param name="slug">Blog post slug</param>
    /// <returns>Blog post if found, null otherwise</returns>
    Task<Entities.BlogEntry?> GetBlogBySlugAsync(string slug);

    /// <summary>
    /// Gets featured blog posts
    /// </summary>
    /// <param name="count">Maximum number of featured posts to retrieve</param>
    /// <returns>Collection of featured blog posts</returns>
    Task<IEnumerable<Entities.BlogEntry>> GetFeaturedBlogsAsync(int count = 5);

    /// <summary>
    /// Searches blog posts by title, description, or content
    /// </summary>
    /// <param name="searchTerm">Search term</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated search results</returns>
    Task<PaginatedResult<Entities.BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Gets blog posts by tag
    /// </summary>
    /// <param name="tag">Tag to filter by</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated result of blog posts with the specified tag</returns>
    Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber = 1, int pageSize = 10);

    /// <summary>
    /// Creates a new blog post
    /// </summary>
    /// <param name="blog">Blog post to create</param>
    /// <returns>Created blog post</returns>
    Task<Entities.BlogEntry> AddBlogAsync(Entities.BlogEntry blog);

    /// <summary>
    /// Updates an existing blog post
    /// </summary>
    /// <param name="blog">Blog post to update</param>
    /// <returns>Updated blog post</returns>
    Task<Entities.BlogEntry> UpdateBlogAsync(Entities.BlogEntry blog);

    /// <summary>
    /// Deletes a blog post by ID
    /// </summary>
    /// <param name="id">Blog post ID to delete</param>
    /// <returns>True if deleted successfully, false otherwise</returns>
    Task<bool> DeleteBlogAsync(long id);

    /// <summary>
    /// Converts blog content to HTML format
    /// </summary>
    /// <param name="blog">Blog post</param>
    /// <returns>HTML content</returns>
    Task<string> GetContentAsHtmlAsync(Entities.BlogEntry blog);

    /// <summary>
    /// Gets the raw markdown content
    /// </summary>
    /// <param name="blog">Blog post</param>
    /// <returns>Raw markdown content if available</returns>
    Task<string> GetRawMarkdownAsync(Entities.BlogEntry blog);

    /// <summary>
    /// Increments the view count for a blog post
    /// </summary>
    /// <param name="id">Blog post ID</param>
    /// <returns>Updated view count</returns>
    Task<int> IncrementViewCountAsync(long id);

    /// <summary>
    /// Gets all unique tags used in blog posts
    /// </summary>
    /// <returns>Collection of unique tags</returns>
    Task<IEnumerable<string>> GetAllTagsAsync();

    /// <summary>
    /// Gets blog posts by author
    /// </summary>
    /// <param name="author">Author name</param>
    /// <param name="pageNumber">Page number (1-based)</param>
    /// <param name="pageSize">Number of items per page</param>
    /// <returns>Paginated result of blog posts by the specified author</returns>
    Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber = 1, int pageSize = 10);
    Task<string?> GetLatestEntries(int v);
}