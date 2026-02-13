using ZauberCMS.Core.Media.Models;

namespace ZauberCMS.Core.Media.Parameters;

public class GetMediaParameters
{
    public string? Id { get; set; }
    public bool IncludeChildren { get; set; }
    public bool IncludeParent { get; set; }
    public bool Cached { get; set; }
    
    public MediaType? MediaType { get; set; }
}