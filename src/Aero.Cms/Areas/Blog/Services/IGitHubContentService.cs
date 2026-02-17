namespace Aero.Cms.Areas.Blog.Services;

public interface IGitHubContentService
{
    Task<string> GetMarkdownFileAsync(string path);
    Task<IEnumerable<string>> GetDirectoryContentsAsync(string path);
}
