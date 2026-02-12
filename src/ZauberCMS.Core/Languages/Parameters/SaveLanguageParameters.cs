using System.Globalization;

namespace ZauberCMS.Core.Languages.Parameters;

public class SaveLanguageParameters
{
    public CultureInfo? CultureInfo { get; set; }
    public string? Id {get; set;}
}