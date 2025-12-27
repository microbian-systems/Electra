using Electra.Core;
using Electra.Web.BlogEngine.Enums;
using Electra.Web.BlogEngine.Models;
using Markdig;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Electra.Web.BlogEngine.Data;

public interface IBlogRepository
{
    Task<IEnumerable<Entities.BlogEntry>> GetLatestBlogsAsync(int count);
    Task<PaginatedResult<Entities.BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true);
    Task<Entities.BlogEntry?> GetBlogByIdAsync(long id);
    Task<Entities.BlogEntry?> GetBlogBySlugAsync(string slug);
    Task<IEnumerable<Entities.BlogEntry>> GetFeaturedBlogsAsync(int count = 5);
    Task<PaginatedResult<Entities.BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize);
    Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize);
    Task<Entities.BlogEntry> AddBlogAsync(Entities.BlogEntry blog);
    Task<Entities.BlogEntry> UpdateBlogAsync(Entities.BlogEntry blog);
    Task<bool> DeleteBlogAsync(long id);
    Task<int> IncrementViewCountAsync(long id);
    Task<IEnumerable<string>> GetAllTagsAsync();
    Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize);
    Task<string> GetContentAsHtmlAsync(Entities.BlogEntry blog);
    Task<string> GetRawMarkdownAsync(Entities.BlogEntry blog);
}

public class BlogRepositoryEfCore(BlogDbContext context, ILogger<BlogRepositoryEfCore> logger) : IBlogRepository
{
    private readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseBootstrap()
        .Build();

    public async Task<IEnumerable<Entities.BlogEntry>> GetLatestBlogsAsync(int count)
    {
        try
        {
            return await context.Blogs
                .Where(b => b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving latest blogs");
            return [];
        }
    }

    public async Task<PaginatedResult<Entities.BlogEntry>> GetPaginatedBlogsAsync(int pageNumber, int pageSize, bool publishedOnly = true)
    {
        try
        {
            var query = context.Blogs.AsQueryable();

            if (publishedOnly)
            {
                query = query.Where(b => b.IsPublished && !b.IsDraft);
            }

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PaginatedResult<Entities.BlogEntry>.Create(items, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving paginated blogs");
            return PaginatedResult<Entities.BlogEntry>.Create([], pageNumber, pageSize, 0);
        }
    }

    public async Task<Entities.BlogEntry?> GetBlogByIdAsync(long id)
    {
        try
        {
            return await context.Blogs
                .FirstOrDefaultAsync(b => b.Id == id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blog by ID: {BlogId}", id);
            return null;
        }
    }

    public async Task<Entities.BlogEntry?> GetBlogBySlugAsync(string slug)
    {
        try
        {
            return await context.Blogs
                .FirstOrDefaultAsync(b => b.Slug == slug && b.IsPublished && !b.IsDraft);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blog by slug: {Slug}", slug);
            return null;
        }
    }

    public async Task<IEnumerable<Entities.BlogEntry>> GetFeaturedBlogsAsync(int count = 5)
    {
        try
        {
            return await context.Blogs
                .Where(b => b.IsFeatured && b.IsPublished && !b.IsDraft)
                .OrderByDescending(b => b.CreatedAt)
                .Take(count)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving featured blogs");
            return [];
        }
    }

    public async Task<PaginatedResult<Entities.BlogEntry>> SearchBlogsAsync(string searchTerm, int pageNumber, int pageSize)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return await GetPaginatedBlogsAsync(pageNumber, pageSize);
            }

            var query = context.Blogs
                .Where(b => b.IsPublished && !b.IsDraft &&
                           (b.Title.Contains(searchTerm) ||
                            b.Description.Contains(searchTerm) ||
                            b.Content.Contains(searchTerm)));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PaginatedResult<Entities.BlogEntry>.Create(items, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error searching blogs with term: {SearchTerm}", searchTerm);
            return PaginatedResult<Entities.BlogEntry>.Create([], pageNumber, pageSize, 0);
        }
    }

    public async Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByTagAsync(string tag, int pageNumber, int pageSize)
    {
        try
        {
            var query = context.Blogs
                .Where(b => b.IsPublished && !b.IsDraft &&
                           b.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PaginatedResult<Entities.BlogEntry>.Create(items, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blogs by tag: {Tag}", tag);
            return PaginatedResult<Entities.BlogEntry>.Create([], pageNumber, pageSize, 0);
        }
    }

    public async Task<Entities.BlogEntry> AddBlogAsync(Entities.BlogEntry blog)
    {
        try
        {
            blog.Id = Snowflake.NewId();
            blog.CreatedAt = DateTime.UtcNow;
            blog.UpdatedAt = DateTime.UtcNow;

            context.Blogs.Add(blog);
            await context.SaveChangesAsync();

            logger.LogInformation("Blog created successfully: {BlogId}", blog.Id);
            return blog;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating blog");
            throw;
        }
    }

    public async Task<Entities.BlogEntry> UpdateBlogAsync(Entities.BlogEntry blog)
    {
        try
        {
            var existingBlog = await context.Blogs.FindAsync(blog.Id);
            if (existingBlog == null)
            {
                throw new InvalidOperationException($"Blog with ID {blog.Id} not found");
            }

            // Update properties
            existingBlog.Title = blog.Title;
            existingBlog.Description = blog.Description;
            existingBlog.Content = blog.Content;
            existingBlog.ContentType = blog.ContentType;
            existingBlog.Tags = blog.Tags;
            existingBlog.Authors = blog.Authors;
            existingBlog.SvgData = blog.SvgData;
            existingBlog.ImageUrl = blog.ImageUrl;
            existingBlog.IsPublished = blog.IsPublished;
            existingBlog.IsDraft = blog.IsDraft;
            existingBlog.IsFeatured = blog.IsFeatured;
            existingBlog.Slug = blog.Slug;
            existingBlog.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync();

            logger.LogInformation("Blog updated successfully: {BlogId}", blog.Id);
            return existingBlog;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating blog: {BlogId}", blog.Id);
            throw;
        }
    }

    public async Task<bool> DeleteBlogAsync(long id)
    {
        try
        {
            var blog = await context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return false;
            }

            context.Blogs.Remove(blog);
            await context.SaveChangesAsync();

            logger.LogInformation("Blog deleted successfully: {BlogId}", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting blog: {BlogId}", id);
            return false;
        }
    }

    public async Task<int> IncrementViewCountAsync(long id)
    {
        try
        {
            var blog = await context.Blogs.FindAsync(id);
            if (blog == null)
            {
                return 0;
            }

            blog.ViewCount++;
            await context.SaveChangesAsync();

            return blog.ViewCount;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing view count for blog: {BlogId}", id);
            return 0;
        }
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        try
        {
            var blogs = await context.Blogs
                .Where(b => b.IsPublished && !b.IsDraft)
                .Select(b => b.Tags)
                .ToListAsync();

            var allTags = blogs
                .SelectMany(tags => tags)
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag)
                .ToList();

            return allTags;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all tags");
            return [];
        }
    }

    public async Task<PaginatedResult<Entities.BlogEntry>> GetBlogsByAuthorAsync(string author, int pageNumber, int pageSize)
    {
        try
        {
            var query = context.Blogs
                .Where(b => b.IsPublished && !b.IsDraft &&
                           b.Authors.Any(a => a.Equals(author, StringComparison.OrdinalIgnoreCase)));

            var totalCount = await query.CountAsync();
            var items = await query
                .OrderByDescending(b => b.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return PaginatedResult<Entities.BlogEntry>.Create(items, pageNumber, pageSize, totalCount);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving blogs by author: {Author}", author);
            return PaginatedResult<Entities.BlogEntry>.Create([], pageNumber, pageSize, 0);
        }
    }

    public async Task<string> GetContentAsHtmlAsync(Entities.BlogEntry blog)
    {
        try
        {
            return await Task.Run(() =>
            {
                return blog.ContentType switch
                {
                    ContentType.Markdown => Markdown.ToHtml(blog.Content, _markdownPipeline),
                    ContentType.HTML => blog.Content,
                    ContentType.JSON => $"<pre><code>{blog.Content}</code></pre>",
                    _ => blog.Content
                };
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error converting content to HTML for blog: {BlogId}", blog.Id);
            return blog.Content;
        }
    }

    public async Task<string> GetRawMarkdownAsync(Entities.BlogEntry blog)
    {
        try
        {
            return await Task.FromResult(
                blog.ContentType == ContentType.Markdown
                    ? blog.Content
                    : string.Empty
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving raw markdown for blog: {BlogId}", blog.Id);
            return string.Empty;
        }
    }
}
