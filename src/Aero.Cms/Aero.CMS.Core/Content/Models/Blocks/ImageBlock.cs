namespace Aero.CMS.Core.Content.Models.Blocks;

public class ImageBlock : ContentBlock
{
    public override string Type => "imageBlock";

    public Guid? MediaId
    {
        get => Properties.TryGetValue("mediaId", out var value) && value != null && Guid.TryParse(value.ToString(), out var guid) ? guid : null;
        set => Properties["mediaId"] = value;
    }

    public string Alt
    {
        get => Properties.TryGetValue("alt", out var value) ? value?.ToString() ?? string.Empty : string.Empty;
        set => Properties["alt"] = value;
    }
}
