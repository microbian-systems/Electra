namespace Aero.CMS.Core.Content.Models.Blocks;

public class QuoteBlock : ContentBlock
{
    public override string Type => "quoteBlock";

    public string Quote
    {
        get => Properties.TryGetValue("quote", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["quote"] = value;
    }

    public string Attribution
    {
        get => Properties.TryGetValue("attribution", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["attribution"] = value;
    }
}
