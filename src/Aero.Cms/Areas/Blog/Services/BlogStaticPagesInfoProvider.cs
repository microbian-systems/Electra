using AspNetStatic;
using Microsoft.Extensions.Logging;

namespace Aero.Cms.Areas.Blog.Services;

public class BlogStaticPagesInfoProvider : StaticResourcesInfoProviderBase, IStaticResourcesInfoProvider
{
    private readonly IGitHubContentService _contentService;
    private readonly ILogger<BlogStaticPagesInfoProvider> _logger;

    public BlogStaticPagesInfoProvider(IGitHubContentService contentService, ILogger<BlogStaticPagesInfoProvider> logger)
    {
        _contentService = contentService;
        _logger = logger;
    }


    private IEnumerable<ResourceInfoBase> GetResourcesSync()
    {
        // Since the interface properties are synchronous, we have to run this synchronously.
        // This is not ideal for IO-bound operations but AspNetStatic might require it this way
        // if we implement the interface directly.
        // Better approach might be to pre-fetch or use a different mechanism if possible.
        // For now, blocking.
        return GetResourcesAsync().GetAwaiter().GetResult();
    }

    private async Task<IEnumerable<ResourceInfoBase>> GetResourcesAsync()
    {
        var resources = new List<ResourceInfoBase>();

        try
        {
            // Fetch all markdown files from the "posts" directory
            var postPaths = await _contentService.GetDirectoryContentsAsync("posts");

            foreach (var path in postPaths)
            {
                // Assuming path is like "posts/my-slug.md"
                // We want the route to be "/blog/my-slug"
                var fileName = Path.GetFileNameWithoutExtension(path);
                var route = $"/blog/{fileName}";

                resources.Add(new PageResource(route)
                {
                    OutFile = Path.Combine("blog", $"{fileName}.html")
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to retrieve blog posts for static generation.");
        }

        return resources;
    }
}
