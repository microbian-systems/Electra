using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;
using ZiggyCreatures.Caching.Fusion;

namespace Aero.Cms.Areas.Blog.Services;

public class GitHubBlogService
{
    private readonly IFusionCache _cache;
    private readonly GitHubClient _github;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<GitHubBlogService> _logger;

    public GitHubBlogService(
        IFusionCache cache,
        IConfiguration config,
        IWebHostEnvironment env,
        ILogger<GitHubBlogService> logger)
    {
        _cache = cache;
        _config = config;
        _env = env;
        _logger = logger;
        _github = new GitHubClient(new Octokit.ProductHeaderValue("MicrobiansBlogEngine"));
        
        var token = _config["GitHub:Token"];
        if (!string.IsNullOrEmpty(token))
        {
            _github.Credentials = new Octokit.Credentials(token);
        }
    }

    public async Task<string?> GetRawMarkdownAsync(string slug)
    {
        var cacheKey = $"blog:{slug}:markdown";

        // Try to get from Cache (Redis/Memory via FusionCache)
        // FusionCache handles the "Get from Cache, if missing, calling factory" logic.
        // The factory here implements the Disk -> GitHub fallback.
        var markdown = await _cache.GetOrSetAsync<string?>(
            cacheKey,
            async (ctx, token) => 
            {
                // Fallback to Disk
                var diskContent = await GetFromDiskAsync(slug);
                if (diskContent != null) 
                {
                    _logger.LogInformation("Cache Hit (Disk): {Slug}", slug);
                    return diskContent;
                }

                // Fallback to GitHub
                return await GetFromGitHubAsync(slug);
            },
            options => options.SetDuration(TimeSpan.FromDays(30)) // Long cache, invalidated by webhook
        );

        return markdown;
    }

    public async Task ForceUpdateFromGitHubAsync(string slug)
    {
        var content = await GetFromGitHubAsync(slug);
        if (content != null)
        {
            await UpdateCacheAsync(slug, content);
        }
    }

    private async Task<string?> GetFromDiskAsync(string slug)
    {
        var path = GetDiskPath(slug);
        if (File.Exists(path))
        {
            return await File.ReadAllTextAsync(path);
        }
        return null;
    }

    private async Task<string?> GetFromGitHubAsync(string slug)
    {
        try 
        {
            var owner = _config["GitHub:Owner"];
            var repo = _config["GitHub:Repository"];
            // Assuming the structure is /posts/{slug}.md as per prompt
            var path = $"posts/{slug}.md"; 

            _logger.LogInformation("Fetching from GitHub: {Path}", path);
            
            // GetAllContents returns a list (file or directory contents). 
            // For a single file, it returns a list with one item.
            var file = await _github.Repository.Content.GetAllContents(owner, repo, path);
            var content = file[0].Content;

            // Save to Disk for future "Disk Cache" hits
            await SaveToDiskAsync(slug, content);

            return content;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch from GitHub for slug: {Slug}", slug);
            return null;
        }
    }

    public async Task SaveToDiskAsync(string slug, string content)
    {
        var path = GetDiskPath(slug);
        var dir = Path.GetDirectoryName(path);
        if (dir != null && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        await File.WriteAllTextAsync(path, content);
    }
    
    public async Task InvalidateCacheAsync(string slug)
    {
         var cacheKey = $"blog:{slug}:markdown";
         await _cache.RemoveAsync(cacheKey);
         
         // Ideally we also update Disk cache here if we have the new content, 
         // but Invalidate usually just clears Redis so next fetch updates everything.
         // However, the Webhook flow says: 
         // 4. Local Disk Cache: Save raw Markdown
         // 5. Redis Cache: Cache raw Markdown
         // So the WebhookController should call SaveToDiskAsync and SetCache.
    }

    public async Task UpdateCacheAsync(string slug, string content)
    {
        // Update Disk
        await SaveToDiskAsync(slug, content);

        // Update Redis
        var cacheKey = $"blog:{slug}:markdown";
        await _cache.SetAsync(cacheKey, content, options => options.SetDuration(TimeSpan.FromDays(30)));
    }

    private string GetDiskPath(string slug)
    {
        return Path.Combine(_env.ContentRootPath, "BlogCache", "Markdown", $"{slug}.md");
    }
}
