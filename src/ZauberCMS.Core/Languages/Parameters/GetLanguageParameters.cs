namespace ZauberCMS.Core.Languages.Parameters;

public class GetLanguageParameters
{
    public string? Id { get; set; }
    public bool AsNoTracking { get; set; }
    public string? LanguageIsoCode { get; set; }
}