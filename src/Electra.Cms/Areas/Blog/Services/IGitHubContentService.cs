namespace Electra.Web.BlogEngine.Services;

public interface IGitHubContentService
{
    Task<string> GetMarkdownFileAsync(string path);
    Task<IEnumerable<string>> GetDirectoryContentsAsync(string path);
}
