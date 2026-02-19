namespace Aero.CMS.Core.Content.Models;

public class SearchMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> ImageAlts { get; set; } = new();
    public DateTime? LastIndexed { get; set; }
}
