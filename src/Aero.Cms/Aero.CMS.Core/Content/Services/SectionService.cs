using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;

namespace Aero.CMS.Core.Content.Services;

public class SectionService
{
    public SectionBlock AddSection(ContentDocument page, SectionLayout layout)
    {
        var section = new SectionBlock
        {
            Layout = layout,
            SortOrder = page.Blocks.Count
        };
        section.InitialiseColumns();
        page.Blocks.Add(section);
        return section;
    }

    public bool RemoveSection(ContentDocument page, Guid sectionId)
    {
        var section = page.Blocks.FirstOrDefault(b => b.Id == sectionId);
        if (section is null) return false;
        page.Blocks.Remove(section);
        RenumberSortOrder(page.Blocks);
        return true;
    }

    public bool MoveSection(ContentDocument page, Guid sectionId, int direction)
    {
        var ordered = page.Blocks.OrderBy(b => b.SortOrder).ToList();
        var idx = ordered.FindIndex(b => b.Id == sectionId);
        if (idx < 0) return false;

        var targetIdx = idx + direction;
        if (targetIdx < 0 || targetIdx >= ordered.Count) return false;

        (ordered[idx].SortOrder, ordered[targetIdx].SortOrder) =
            (ordered[targetIdx].SortOrder, ordered[idx].SortOrder);
        return true;
    }

    public ContentBlock AddBlock(
        ContentDocument page,
        Guid sectionId,
        int colIndex,
        ContentBlock block)
    {
        var section = page.Blocks
            .OfType<SectionBlock>()
            .FirstOrDefault(s => s.Id == sectionId)
            ?? throw new InvalidOperationException($"Section {sectionId} not found.");

        var column = section.Children
            .OfType<ColumnBlock>()
            .FirstOrDefault(c => c.ColIndex == colIndex)
            ?? throw new InvalidOperationException($"Column {colIndex} not found.");

        block.SortOrder = column.Children.Count;
        column.Children.Add(block);
        return block;
    }

    public bool RemoveBlock(ContentDocument page, Guid sectionId, Guid blockId)
    {
        var section = page.Blocks
            .OfType<SectionBlock>()
            .FirstOrDefault(s => s.Id == sectionId);
        if (section is null) return false;

        foreach (var column in section.Children.OfType<ColumnBlock>())
        {
            var block = column.Children.FirstOrDefault(b => b.Id == blockId);
            if (block is null) continue;
            column.Children.Remove(block);
            RenumberSortOrder(column.Children);
            return true;
        }
        return false;
    }

    private static void RenumberSortOrder(List<ContentBlock> blocks)
    {
        for (var i = 0; i < blocks.Count; i++)
            blocks[i].SortOrder = i;
    }
}
