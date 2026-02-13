namespace ZauberCMS.Core.Content.Parameters;

public class CopyContentParameters
{
    public string ContentToCopy { get; set; }
    public bool IncludeDescendants { get; set; }
    public string? CopyTo { get; set; }
}