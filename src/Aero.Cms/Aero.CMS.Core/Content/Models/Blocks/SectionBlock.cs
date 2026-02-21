namespace Aero.CMS.Core.Content.Models.Blocks;

public class SectionBlock : ContentBlock, ICompositeContentBlock
{
    public static string BlockType => "sectionBlock";
    public override string Type => BlockType;

    public SectionLayout Layout
    {
        get => Enum.TryParse<SectionLayout>(
                   Properties.GetValueOrDefault("layout")?.ToString(),
                   out var l) ? l : SectionLayout.Full;
        set => Properties["layout"] = value.ToString();
    }

    public string? BackgroundColor
    {
        get => Properties.GetValueOrDefault("backgroundColor")?.ToString();
        set => Properties["backgroundColor"] = value ?? string.Empty;
    }

    public string? CssClass
    {
        get => Properties.GetValueOrDefault("cssClass")?.ToString();
        set => Properties["cssClass"] = value ?? string.Empty;
    }

    public string[] AllowedChildTypes => [ColumnBlock.BlockType];
    public bool AllowNestedComposites => false;
    public int? MaxChildren => 3;

    public void InitialiseColumns()
    {
        var count = Layout switch
        {
            SectionLayout.Full        => 1,
            SectionLayout.TwoColumn   => 2,
            SectionLayout.ThreeColumn => 3,
            SectionLayout.Sidebar     => 2,
            _ => 1
        };

        Children.Clear();
        for (var i = 0; i < count; i++)
        {
            Children.Add(new ColumnBlock
            {
                SortOrder = i,
                ColIndex = i
            });
        }
    }
}
