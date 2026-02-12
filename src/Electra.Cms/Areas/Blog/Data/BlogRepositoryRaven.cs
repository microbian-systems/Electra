using System.Collections.Generic;
using Electra.Cms.Areas.Blog.Entities;
using Electra.Models;
using Electra.Persistence.RavenDB;
using LanguageExt;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System.Linq;

namespace Electra.Cms.Areas.Blog.Data;

public class BlogRepositoryRaven : RavenDbRepositoryBase<BlogPost>, IBlogRepository
{
    public BlogRepositoryRaven(IAsyncDocumentSession session, ILogger<BlogRepositoryRaven> log) 
        : base(session, log)
    {
    }
    
    public async Task<IEnumerable<BlogPost>> GetLatestBlogsAsync(int count)
    {
        log.LogInformation("Getting latest {Count} blogs", count);
        var posts = await session.Query<BlogPost>()
            .OrderByDescending(b => b.CreatedOn)
            .Take(count)
            .ToListAsync();
        return posts;
    }

    public async Task<PagedResult<BlogPost>> GetPaginatedBlogsAsync(int pageNumber = 1, int pageSize = 10, bool publishedOnly = true)
    {
        if(pageNumber <= 0) pageNumber = 1;
        log.LogInformation("Getting paginated blogs (pageNumber: {PageNumber}, pageSize: {PageSize})", pageNumber, pageSize);
        var posts = await session.Query<BlogPost>()
            .OrderByDescending(b => b.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return PagedResult<BlogPost>.Create(posts ?? [], pageNumber, pageSize, posts?.Count ?? 0);
    }

    public async Task<Option<BlogPost>> GetBlogByIdAsync(string id)
        => await FindByIdAsync(id);

    public async Task<Option<BlogPost>> GetBlogBySlugAsync(string slug)
    {
        var post = await session.Query<BlogPost>()
            .Where(b => b.Slug == slug)
            .SingleOrDefaultAsync();
        return post;
    }

    public async Task<IEnumerable<BlogPost>> GetFeaturedBlogsAsync(int count = 5)
    {
        var featuredBlogs = await session.Query<BlogPost>()
            .Where(b => b.IsFeatured)
            .OrderByDescending(b => b.CreatedOn)
            .Take(count)
            .ToListAsync();
        return featuredBlogs ?? [];
    }

    public async Task<PagedResult<BlogPost>> SearchBlogsAsync(string searchTerm, int pageNumber=1, int pageSize=10)
    {
        if(pageNumber <= 0) pageNumber = 1;
        if(pageSize <= 0) pageSize = 10;
        var posts = await session.Query<BlogPost>()
            .Where(b => b.Title.Contains(searchTerm) || b.Content.Contains(searchTerm))
            .OrderByDescending(b => b.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return PagedResult<BlogPost>.Create(posts ?? [], pageNumber, pageSize, posts?.Count ?? 0);
    }

    public async Task<PagedResult<BlogPost>> GetBlogsByTagAsync(string tag, int pageNumber=1, int pageSize=10)
    {
        if(pageNumber <= 0) pageNumber = 1;
        if(pageSize <= 0) pageSize = 10;
        var posts = await session.Query<BlogPost>()
            .Where(b => b.Tags.AsEnumerable().Contains(tag))
            .OrderByDescending(b => b.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return PagedResult<BlogPost>.Create(posts ?? [], pageNumber, pageSize, posts?.Count ?? 0);
    }

    public async Task<Option<BlogPost>> AddBlogAsync(BlogPost? blog)
    {
        if (blog is not null) 
            return await InsertAsync(blog);
        
        log.LogWarning("attempted to add a null blog entry. returning w/ noop");
        return Option<BlogPost>.None;

    }

    public async Task<Option<BlogPost>> UpdateBlogAsync(BlogPost? blog)
    {
        if (blog is not null) 
            return await UpsertAsync(blog);
        log.LogWarning("attempted to update a null blog entry. returning w/ noop");
        return Option<BlogPost>.None;
    }

    public async Task<bool> DeleteBlogAsync(string id)
    {
        if(string.IsNullOrEmpty(id))
        {
            log.LogWarning("attempted to delete a null blog entry. returning w/ noop");
            return false;
        }
        
        session.Delete(id);
        return await Task.FromResult(true);
    }

    public async Task<int> IncrementViewCountAsync(string id)
    {
        var post = await FindByIdAsync(id);
        if(post.IsNone) return 0;
        post.IfSome(p => p.ViewCount++);
        return post.Match(p => p.ViewCount, 0);
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        var tags = await session.Query<BlogPost>()
            .SelectMany(b => b.Tags)
            .Distinct()
            .ToListAsync();
        return tags ?? [];
    }

    public async Task<IEnumerable<BlogPost>> GetRelatedBlogsAsync(string blogId, int count = 3)
    {
        var blogOption = await GetBlogByIdAsync(blogId);
        if (blogOption.IsNone) return [];
        
        var blog = blogOption.IfNone(new BlogPost());

        // Priority 1: Vector similarity (if vector search is configured and available)
        // Note: This requires a specialized index in RavenDB for high performance.
        // For now, we combine tags and series matching as a robust default.
        
        var query = session.Query<BlogPost>()
            .Where(b => b.BlogId != blogId && b.IsPublished);

        // If part of a series, prioritize series
        if (!string.IsNullOrEmpty(blog.Series))
        {
            var seriesPosts = await query
                .Where(b => b.Series == blog.Series)
                .Take(count)
                .ToListAsync();
            
            if (seriesPosts.Count >= count) return seriesPosts;
            
            // If not enough series posts, we'll continue with tags
        }

        // Tag matching
        var related = await session.Query<BlogPost>()
            .Where(b => b.BlogId != blogId && b.IsPublished)
            .Where(b => b.Tags.ContainsAny(blog.Tags))
            .OrderByDescending(b => b.PublishDate)
            .Take(count)
            .ToListAsync();

        return related ?? [];
    }

    public async Task<PagedResult<BlogPost>> GetBlogsByAuthorAsync(string author, int pageNumber=1, int pageSize=10)
    {
        if(pageNumber <= 0) pageNumber = 1;
        if(pageSize <= 0) pageSize = 10;
        var posts = await session.Query<BlogPost>()
            .Where(b => b.Authors.AsEnumerable().Contains(author))
            .OrderByDescending(b => b.CreatedOn)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        return PagedResult<BlogPost>.Create(posts ?? [], pageNumber, pageSize, posts?.Count ?? 0);
    }

    public async Task<bool> BulkImportMarkdown(IReadOnlyList<MarkDownContentModel>? entries)
    {
        if (entries is null || !entries.Any())
        {
            log.LogWarning("No blogs provided for the call to bulk import.");
            return false;
        }

        foreach (var entry in entries)
        {
            var post = new BlogPost
            {
                ImageUrl = entry.imageUrl,
                Tags = entry.tags,
                PublishDate = entry.publishedAt,
                Content = entry.content,
                Title = entry.title,
                Slug = entry.slug,
                ApprovalRequired = false,
                IsPublished = false,
                Series = entry.series,
                Authors = ["microbians"],
            };
            await InsertAsync(post);
        }

        log.LogInformation("Successfully imported {count} blogs.", entries.Count);
        return true;
    }
}