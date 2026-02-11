using Electra.Cms.Areas.Blog.Entities;
using Electra.Cms.Areas.Blog.Enums;
using Electra.Models;
using LanguageExt;
using Markdig;

using Microsoft.Extensions.Logging;

namespace Electra.Cms.Areas.Blog.Data;

public class BlogRepositoryEfCore : IBlogRepository
{
    private readonly ILogger<BlogRepositoryEfCore> log;
    private readonly MarkdownPipeline _markdownPipeline;

    public BlogRepositoryEfCore(BlogRepositoryEfCore db, ILogger<BlogRepositoryEfCore> log)
    {
        
        this.log = log;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
    }

    public async Task<IEnumerable<BlogPost>> GetLatestBlogsAsync(int count) 
    {
        var result = await context.Blogs
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishDate)
            .Take(count)
            //.ToListAsync()
            ;
    }

    public async Task<PagedResult<BlogPost>> GetPaginatedBlogsAsync(int pageNumber = 1, int pageSize = 10, bool publishedOnly = true)
    {
        var query = context.Blogs.AsQueryable();
        if (publishedOnly)
        {
            query = query.Where(b => b.IsPublished);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.PublishDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<BlogPost>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<Option<BlogPost>> GetBlogByIdAsync(string id)
    {
        var blog = await context.Blogs.FindAsync(id);
        return blog != null ? LanguageExt.Option<BlogPost>.Some(blog) : LanguageExt.Option<BlogPost>.None;
    }

    public async Task<Option<BlogPost>> GetBlogBySlugAsync(string slug)
    {
        var blog = await context.Blogs.FirstOrDefaultAsync(b => b.Slug == slug);
        return blog != null ? LanguageExt.Option<BlogPost>.Some(blog) : LanguageExt.Option<BlogPost>.None;
    }

    public async Task<IEnumerable<BlogPost>> GetFeaturedBlogsAsync(int count = 5)
    {
        return await context.Blogs
            .Where(b => b.IsPublished && b.IsFeatured)
            .OrderByDescending(b => b.PublishDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PagedResult<BlogPost>> SearchBlogsAsync(string searchTerm, int pageNumber = 1, int pageSize = 10)
    {
        var query = context.Blogs
            .Where(b => b.IsPublished && (b.Title.Contains(searchTerm) || b.Description.Contains(searchTerm) || b.Content.Contains(searchTerm)));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.PublishDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<BlogPost>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<PagedResult<BlogPost>> GetBlogsByTagAsync(string tag, int pageNumber = 1, int pageSize = 10)
    {
        var query = context.Blogs
            .Where(b => b.IsPublished && b.Tags.Any(t => t == tag));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.PublishDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<BlogPost>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<Option<BlogPost>> AddBlogAsync(BlogPost blog)
    {
        context.Blogs.Add(blog);
        await context.SaveChangesAsync();
        return LanguageExt.Option<BlogPost>.Some(blog);
    }

    public async Task<Option<BlogPost>> UpdateBlogAsync(BlogPost? blog)
    {
        if (blog == null) return LanguageExt.Option<BlogPost>.None;
        context.Entry(blog).State = EntityState.Modified;
        await context.SaveChangesAsync();
        return LanguageExt.Option<BlogPost>.Some(blog);
    }

    public async Task<bool> DeleteBlogAsync(string id)
    {
        var blog = await context.Blogs.FindAsync(id);
        if (blog == null) return false;
        context.Blogs.Remove(blog);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<int> IncrementViewCountAsync(string id)
    {
        var blog = await context.Blogs.FindAsync(id);
        if (blog == null) return 0;
        blog.ViewCount++;
        await context.SaveChangesAsync();
        return blog.ViewCount;
    }

    public async Task<IEnumerable<string>> GetAllTagsAsync()
    {
        return await context.Blogs
            .SelectMany(b => b.Tags)
            .Distinct()
            .ToListAsync();
    }

    public async Task<IEnumerable<BlogPost>> GetRelatedBlogsAsync(string blogId, int count = 3)
    {
        var blog = await context.Blogs.FindAsync(blogId);
        if (blog == null) return [];

        var tags = blog.Tags;
        
        return await context.Blogs
            .Where(b => b.BlogId != blogId && b.IsPublished)
            .Where(b => b.Tags.Any(t => tags.Contains(t)))
            .OrderByDescending(b => b.PublishDate)
            .Take(count)
            .ToListAsync();
    }

    public async Task<PagedResult<BlogPost>> GetBlogsByAuthorAsync(string author, int pageNumber = 1, int pageSize = 10)
    {
        var query = context.Blogs
            .Where(b => b.IsPublished && b.Authors.Any(a => a == author));

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(b => b.PublishDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return PagedResult<BlogPost>.Create(items, pageNumber, pageSize, totalCount);
    }

    public async Task<string> GetContentAsHtmlAsync(BlogPost blog)
    {
        try
        {
            return await Task.Run(() =>
            {
                return blog.ContentType switch
                {
                    ContentType.Markdown => Markdown.ToHtml(blog.Content, _markdownPipeline),
                    ContentType.HTML => blog.Content,
                    _ => blog.Content
                };
            });
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error converting content to HTML for blog {BlogId}", blog.BlogId);
            return "Error converting content.";
        }
    }

    public async Task<string> GetRawMarkdownAsync(BlogPost blog)
    {
        return await Task.FromResult(blog.Content);
    }

    public async Task<bool> BulkImportMarkdown(IReadOnlyList<MarkDownContentModel> blogs)
    {
        foreach (var entry in blogs)
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
            context.Blogs.Add(post);
        }
        await context.SaveChangesAsync();
        return true;
    }
}
