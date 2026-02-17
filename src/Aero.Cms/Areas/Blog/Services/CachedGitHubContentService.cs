using Microsoft.Extensions.Logging;
using ZiggyCreatures.Caching.Fusion;

namespace Aero.Cms.Areas.Blog.Services;

public class CachedGitHubContentService : IGitHubContentService
{
    private readonly IGitHubContentService _innerService;
    private readonly IFusionCache _cache;
    private readonly ILogger<CachedGitHubContentService> _logger;
    private const string CachePrefix = "blog:content:";
    private static readonly TimeSpan DefaultCacheDuration = TimeSpan.FromMinutes(30);

    public CachedGitHubContentService(
        IGitHubContentService innerService,
        IFusionCache cache,
        ILogger<CachedGitHubContentService> logger)
    {
        _innerService = innerService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<string> GetMarkdownFileAsync(string path)
    {
        var cacheKey = $"{CachePrefix}file:{path}";

        return await _cache.GetOrSetAsync<string>(
            cacheKey,
            async (ctx, ct) =>
            {
                _logger.LogInformation("Cache miss for {Path}. Fetching from source.", path);
                return await _innerService.GetMarkdownFileAsync(path);
            },
            options => options.SetDuration(DefaultCacheDuration)
        );
    }

    public async Task<IEnumerable<string>> GetDirectoryContentsAsync(string path)
    {
        var cacheKey = $"{CachePrefix}dir:{path}";

        return await _cache.GetOrSetAsync<IEnumerable<string>>(
            cacheKey,
            async (ctx, ct) =>
            {
                _logger.LogInformation("Cache miss for directory {Path}. Fetching from source.", path);
                return await _innerService.GetDirectoryContentsAsync(path);
            },
            options => options.SetDuration(DefaultCacheDuration)
        );
    }
}
