using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Octokit;

namespace Aero.Cms.Areas.Blog.Services;

public class GitHubContentService : IGitHubContentService
{
    private readonly IGitHubClient _gitHubClient;
    private readonly ILogger<GitHubContentService> _logger;
    private readonly IConfiguration _config;
    private readonly string _owner;
    private readonly string _repo;

    public GitHubContentService(
        IGitHubClient gitHubClient,
        ILogger<GitHubContentService> logger,
        IConfiguration config)
    {
        _gitHubClient = gitHubClient;
        _logger = logger;
        _config = config;
        _owner = _config["GitHub:Owner"] ?? throw new ArgumentNullException("GitHub:Owner configuration is missing");
        _repo = _config["GitHub:Repository"] ?? throw new ArgumentNullException("GitHub:Repository configuration is missing");
    }

    public async Task<string> GetMarkdownFileAsync(string path)
    {
        try
        {
            _logger.LogInformation("Fetching file from GitHub: {Owner}/{Repo}/{Path}", _owner, _repo, path);
            
            // Get repository content
            // Note: This fetches the file content. For larger files, we might want to use the raw content URL.
            var fileContent = await _gitHubClient.Repository.Content.GetAllContents(_owner, _repo, path);
            
            if (fileContent == null || fileContent.Count == 0)
            {
                _logger.LogWarning("File not found: {Path}", path);
                return null;
            }

            return fileContent[0].Content;
        }
        catch (NotFoundException)
        {
            _logger.LogWarning("File not found (404): {Path}", path);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching file from GitHub: {Path}", path);
            throw;
        }
    }

    public async Task<IEnumerable<string>> GetDirectoryContentsAsync(string path)
    {
        try
        {
             _logger.LogInformation("Fetching directory contents from GitHub: {Owner}/{Repo}/{Path}", _owner, _repo, path);

            var contents = await _gitHubClient.Repository.Content.GetAllContents(_owner, _repo, path);
            
            return contents
                .Where(c => c.Type == ContentType.File && c.Name.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                .Select(c => c.Path)
                .ToList();
        }
        catch (NotFoundException)
        {
             _logger.LogWarning("Directory not found (404): {Path}", path);
            return Enumerable.Empty<string>();
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error fetching directory contents from GitHub: {Path}", path);
            throw;
        }
    }
}
