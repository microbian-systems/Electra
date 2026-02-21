using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Services;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class SectionServiceTests
{
    private readonly SectionService _service = new();

    [Fact]
    public void AddSection_AddsSectionBlock_ToPageBlocks()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        
        page.Blocks.Count.ShouldBe(1);
        page.Blocks[0].ShouldBe(section);
    }

    [Fact]
    public void AddSection_ReturnsCreatedSectionBlock()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.TwoColumn);
        
        section.ShouldNotBeNull();
        section.Layout.ShouldBe(SectionLayout.TwoColumn);
    }

    [Fact]
    public void AddSection_AssignsSortOrder_AsCurrentBlocksCount()
    {
        var page = new ContentDocument();
        var section1 = _service.AddSection(page, SectionLayout.Full);
        var section2 = _service.AddSection(page, SectionLayout.Full);
        
        section1.SortOrder.ShouldBe(0);
        section2.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void AddSection_WithTwoColumn_CreatesSectionWith2ColumnBlockChildren()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.TwoColumn);
        
        section.Children.Count.ShouldBe(2);
        section.Children[0].ShouldBeOfType<ColumnBlock>();
        section.Children[1].ShouldBeOfType<ColumnBlock>();
    }

    [Fact]
    public void RemoveSection_RemovesCorrectSection_ById()
    {
        var page = new ContentDocument();
        var section1 = _service.AddSection(page, SectionLayout.Full);
        var section2 = _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.RemoveSection(page, section1.Id);
        
        result.ShouldBeTrue();
        page.Blocks.Count.ShouldBe(1);
        page.Blocks[0].Id.ShouldBe(section2.Id);
    }

    [Fact]
    public void RemoveSection_ReturnsFalse_ForUnknownId()
    {
        var page = new ContentDocument();
        _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.RemoveSection(page, Guid.NewGuid());
        
        result.ShouldBeFalse();
        page.Blocks.Count.ShouldBe(1);
    }

    [Fact]
    public void RemoveSection_RenumbersRemainingSections_SortOrderStartingFrom0()
    {
        var page = new ContentDocument();
        _service.AddSection(page, SectionLayout.Full);
        var section2 = _service.AddSection(page, SectionLayout.Full);
        var section3 = _service.AddSection(page, SectionLayout.Full);
        
        _service.RemoveSection(page, section2.Id);
        
        page.Blocks[0].SortOrder.ShouldBe(0);
        page.Blocks[1].SortOrder.ShouldBe(1);
    }

    [Fact]
    public void MoveSection_SwapsSortOrder_OfAdjacentSections()
    {
        var page = new ContentDocument();
        var section1 = _service.AddSection(page, SectionLayout.Full);
        var section2 = _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.MoveSection(page, section1.Id, 1);
        
        result.ShouldBeTrue();
        section1.SortOrder.ShouldBe(1);
        section2.SortOrder.ShouldBe(0);
    }

    [Fact]
    public void MoveSection_ReturnsFalse_WhenMovingFirstSectionUp()
    {
        var page = new ContentDocument();
        var section1 = _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.MoveSection(page, section1.Id, -1);
        
        result.ShouldBeFalse();
    }

    [Fact]
    public void MoveSection_ReturnsFalse_WhenMovingLastSectionDown()
    {
        var page = new ContentDocument();
        _service.AddSection(page, SectionLayout.Full);
        var section2 = _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.MoveSection(page, section2.Id, 1);
        
        result.ShouldBeFalse();
    }

    [Fact]
    public void MoveSection_ReturnsFalse_ForUnknownSectionId()
    {
        var page = new ContentDocument();
        _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.MoveSection(page, Guid.NewGuid(), 1);
        
        result.ShouldBeFalse();
    }

    [Fact]
    public void AddBlock_AddsLeafBlock_ToCorrectColumn()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.TwoColumn);
        var block = new RichTextBlock { Html = "<p>Test</p>" };
        
        var result = _service.AddBlock(page, section.Id, 0, block);
        
        result.ShouldBe(block);
        var column = section.Children.OfType<ColumnBlock>().First(c => c.ColIndex == 0);
        column.Children.Count.ShouldBe(1);
        column.Children[0].ShouldBe(block);
    }

    [Fact]
    public void AddBlock_Throws_WhenSectionIdNotFound()
    {
        var page = new ContentDocument();
        var block = new RichTextBlock();
        
        Should.Throw<InvalidOperationException>(() => 
            _service.AddBlock(page, Guid.NewGuid(), 0, block));
    }

    [Fact]
    public void AddBlock_Throws_WhenColIndexNotFound()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        var block = new RichTextBlock();
        
        Should.Throw<InvalidOperationException>(() => 
            _service.AddBlock(page, section.Id, 5, block));
    }

    [Fact]
    public void AddBlock_AssignsSortOrder_AsColumnChildrenCount()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        var block1 = new RichTextBlock();
        var block2 = new RichTextBlock();
        
        _service.AddBlock(page, section.Id, 0, block1);
        _service.AddBlock(page, section.Id, 0, block2);
        
        block1.SortOrder.ShouldBe(0);
        block2.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void RemoveBlock_RemovesBlock_FromCorrectColumn()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        var block = new RichTextBlock();
        _service.AddBlock(page, section.Id, 0, block);
        
        var result = _service.RemoveBlock(page, section.Id, block.Id);
        
        result.ShouldBeTrue();
        var column = section.Children.OfType<ColumnBlock>().First();
        column.Children.ShouldBeEmpty();
    }

    [Fact]
    public void RemoveBlock_RenumbersRemainingBlocks_InColumn()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        var block1 = new RichTextBlock();
        var block2 = new RichTextBlock();
        var block3 = new RichTextBlock();
        _service.AddBlock(page, section.Id, 0, block1);
        _service.AddBlock(page, section.Id, 0, block2);
        _service.AddBlock(page, section.Id, 0, block3);
        
        _service.RemoveBlock(page, section.Id, block2.Id);
        
        var column = section.Children.OfType<ColumnBlock>().First();
        column.Children.Count.ShouldBe(2);
        block1.SortOrder.ShouldBe(0);
        block3.SortOrder.ShouldBe(1);
    }

    [Fact]
    public void RemoveBlock_ReturnsFalse_WhenBlockNotFound()
    {
        var page = new ContentDocument();
        var section = _service.AddSection(page, SectionLayout.Full);
        
        var result = _service.RemoveBlock(page, section.Id, Guid.NewGuid());
        
        result.ShouldBeFalse();
    }

    [Fact]
    public void RemoveBlock_ReturnsFalse_WhenSectionNotFound()
    {
        var page = new ContentDocument();
        
        var result = _service.RemoveBlock(page, Guid.NewGuid(), Guid.NewGuid());
        
        result.ShouldBeFalse();
    }
}
