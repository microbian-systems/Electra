using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class SectionBlockTests
{
    [Fact]
    public void SectionBlock_BlockType_Is_sectionBlock()
    {
        SectionBlock.BlockType.ShouldBe("sectionBlock");
    }

    [Fact]
    public void SectionBlock_AllowedChildTypes_ContainsOnly_columnBlock()
    {
        var section = new SectionBlock();
        section.AllowedChildTypes.ShouldNotBeNull();
        section.AllowedChildTypes.Length.ShouldBe(1);
        section.AllowedChildTypes[0].ShouldBe("columnBlock");
    }

    [Fact]
    public void SectionBlock_AllowNestedComposites_Is_False()
    {
        var section = new SectionBlock();
        section.AllowNestedComposites.ShouldBeFalse();
    }

    [Fact]
    public void InitialiseColumns_WithFull_Creates_1_ColumnBlock()
    {
        var section = new SectionBlock { Layout = SectionLayout.Full };
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(1);
        section.Children[0].ShouldBeOfType<ColumnBlock>();
    }

    [Fact]
    public void InitialiseColumns_WithTwoColumn_Creates_2_ColumnBlocks()
    {
        var section = new SectionBlock { Layout = SectionLayout.TwoColumn };
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(2);
        section.Children.All(c => c is ColumnBlock).ShouldBeTrue();
    }

    [Fact]
    public void InitialiseColumns_WithThreeColumn_Creates_3_ColumnBlocks()
    {
        var section = new SectionBlock { Layout = SectionLayout.ThreeColumn };
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(3);
        section.Children.All(c => c is ColumnBlock).ShouldBeTrue();
    }

    [Fact]
    public void InitialiseColumns_WithSidebar_Creates_2_ColumnBlocks()
    {
        var section = new SectionBlock { Layout = SectionLayout.Sidebar };
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(2);
        section.Children.All(c => c is ColumnBlock).ShouldBeTrue();
    }

    [Fact]
    public void InitialiseColumns_ClearsExistingChildren_BeforeCreating()
    {
        var section = new SectionBlock { Layout = SectionLayout.Full };
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(1);
        
        section.Layout = SectionLayout.TwoColumn;
        section.InitialiseColumns();
        section.Children.Count.ShouldBe(2);
    }

    [Fact]
    public void InitialiseColumns_SetsColIndex_0_1_2_InOrder()
    {
        var section = new SectionBlock { Layout = SectionLayout.ThreeColumn };
        section.InitialiseColumns();
        
        var columns = section.Children.Cast<ColumnBlock>().ToList();
        columns[0].ColIndex.ShouldBe(0);
        columns[1].ColIndex.ShouldBe(1);
        columns[2].ColIndex.ShouldBe(2);
    }

    [Fact]
    public void ColumnBlock_BlockType_Is_columnBlock()
    {
        ColumnBlock.BlockType.ShouldBe("columnBlock");
    }

    [Fact]
    public void ColumnBlock_AllowNestedComposites_Is_False()
    {
        var column = new ColumnBlock();
        column.AllowNestedComposites.ShouldBeFalse();
    }

    [Fact]
    public void ColumnBlock_AllowedChildTypes_Is_Null()
    {
        var column = new ColumnBlock();
        column.AllowedChildTypes.ShouldBeNull();
    }

    [Fact]
    public void HtmlBlock_BlockType_Is_htmlBlock()
    {
        HtmlBlock.BlockType.ShouldBe("htmlBlock");
    }

    [Fact]
    public void HtmlBlock_Html_GetterSetter_RoundTrips()
    {
        var block = new HtmlBlock();
        block.Html = "<div>Hello</div>";
        block.Html.ShouldBe("<div>Hello</div>");
    }

    [Fact]
    public void SectionBlock_NewInstance_HasNonEmptyGuid()
    {
        var section = new SectionBlock();
        section.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void ColumnBlock_NewInstance_HasNonEmptyGuid()
    {
        var column = new ColumnBlock();
        column.Id.ShouldNotBe(Guid.Empty);
    }
}
