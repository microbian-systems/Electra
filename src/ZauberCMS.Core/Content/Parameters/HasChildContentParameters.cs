namespace ZauberCMS.Core.Content.Parameters;

public class HasChildContentParameters
{
    public bool Cached { get; set; } = true;
    public string ParentId { get; set; }
}