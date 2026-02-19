namespace Aero.CMS.Core.Content.Models.Blocks;

public class HeroBlock : ContentBlock
{
    public override string Type => "heroBlock";

    public string Heading
    {
        get => Properties.TryGetValue("heading", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["heading"] = value;
    }

    public string Subtext
    {
        get => Properties.TryGetValue("subtext", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["subtext"] = value;
    }
}
