namespace ZauberCMS.Core.Content.Interfaces;

public interface IPropertyValue
{
    public string Id { get; set; }
    public string Alias { get; set; }
    public string Value { get; set; }
    public DateTime? DateUpdated { get; set; }
}