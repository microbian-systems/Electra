using ZauberCMS.Core.Extensions;

namespace ZauberCMS.Core.Languages.Models;

public class LanguageText
{
    /// <summary>
    /// The ID
    /// </summary>
    public string? Id { get; set; } = Guid.NewGuid().NewSequentialGuid().ToString();
    
    /// <summary>
    /// ID of the parent Language Dictionary
    /// </summary>
    public string LanguageDictionaryId { get; set; }
    
    /// <summary>
    /// Parent Language Dictionary
    /// </summary>
    public LanguageDictionary? LanguageDictionary { get; set; }
    
    /// <summary>
    /// Language ID this relates to
    /// </summary>
    public Guid LanguageId { get; set; }
    
    /// <summary>
    /// Language this relates to
    /// </summary>
    public Language? Language { get; set; }
    
    /// <summary>
    /// The language text value
    /// </summary>
    public string Value { get; set; } = string.Empty;
}