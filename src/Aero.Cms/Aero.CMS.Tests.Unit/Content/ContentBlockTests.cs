using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class ContentBlockTests
{
    [Fact]
    public void RichTextBlock_Type_Is_richTextBlock()
    {
        var block = new RichTextBlock();
        block.Type.ShouldBe("richTextBlock");
    }

    [Fact]
    public void RichTextBlock_Html_RoundTrips()
    {
        var block = new RichTextBlock();
        block.Html = "<p>Hello</p>";
        block.Html.ShouldBe("<p>Hello</p>");
        block.Properties["html"].ShouldBe("<p>Hello</p>");
    }

    [Fact]
    public void ImageBlock_MediaId_Returns_Null_When_Absent()
    {
        var block = new ImageBlock();
        block.MediaId.ShouldBeNull();
    }

    [Fact]
    public void ImageBlock_MediaId_RoundTrips()
    {
        var block = new ImageBlock();
        var id = Guid.NewGuid();
        block.MediaId = id;
        block.MediaId.ShouldBe(id);
        block.Properties["mediaId"].ShouldBe(id);
    }

    [Fact]
    public void DivBlock_Implements_ICompositeContentBlock()
    {
        var block = new DivBlock();
        block.ShouldBeAssignableTo<ICompositeContentBlock>();
    }

    [Fact]
    public void GridBlock_MaxChildren_Is_12()
    {
        var block = new GridBlock();
        block.MaxChildren.ShouldBe(12);
    }

    [Fact]
    public void GridBlock_AllowNestedComposites_Is_False()
    {
        var block = new GridBlock();
        block.AllowNestedComposites.ShouldBeFalse();
    }

    [Fact]
    public void GridBlock_Columns_Default_Is_1()
    {
        var block = new GridBlock();
        block.Columns.ShouldBe(1);
    }

    [Fact]
    public void GridBlock_Columns_RoundTrips()
    {
        var block = new GridBlock();
        block.Columns = 3;
        block.Columns.ShouldBe(3);
        block.Properties["columns"].ShouldBe(3);
    }

    [Fact]
    public void GridBlock_Columns_InvalidValue_Defaults_To_1()
    {
        var block = new GridBlock();
        block.Properties["columns"] = "invalid";
        block.Columns.ShouldBe(1);
    }

    [Fact]
    public void GridBlock_BlockType_Is_gridBlock()
    {
        GridBlock.BlockType.ShouldBe("gridBlock");
    }

    [Fact]
    public void GridBlock_Type_Returns_BlockType()
    {
        var block = new GridBlock();
        block.Type.ShouldBe(GridBlock.BlockType);
    }

    [Fact]
    public void GridBlock_AllowedChildTypes_Is_Empty()
    {
        var block = new GridBlock();
        block.AllowedChildTypes.ShouldBeEmpty();
    }

    [Fact]
    public void NewBlock_Has_NonEmpty_Guid_Id()
    {
        var block = new RichTextBlock();
        block.Id.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void Blocks_Can_Have_Children()
    {
        var parent = new DivBlock();
        var child = new RichTextBlock();
        parent.Children.Add(child);
        
        parent.Children.Count.ShouldBe(1);
        parent.Children[0].ShouldBe(child);
    }

    [Fact]
    public void HeroBlock_Heading_RoundTrips()
    {
        var block = new HeroBlock();
        block.Heading = "Test Heading";
        block.Heading.ShouldBe("Test Heading");
        block.Properties["heading"].ShouldBe("Test Heading");
    }

    [Fact]
    public void HeroBlock_Subtext_RoundTrips()
    {
        var block = new HeroBlock();
        block.Subtext = "Test Subtext";
        block.Subtext.ShouldBe("Test Subtext");
        block.Properties["subtext"].ShouldBe("Test Subtext");
    }

    [Fact]
    public void ImageBlock_Alt_RoundTrips()
    {
        var block = new ImageBlock();
        block.Alt = "Test Alt";
        block.Alt.ShouldBe("Test Alt");
        block.Properties["alt"].ShouldBe("Test Alt");
    }

    [Fact]
    public void QuoteBlock_Quote_RoundTrips()
    {
        var block = new QuoteBlock();
        block.Quote = "Test Quote";
        block.Quote.ShouldBe("Test Quote");
        block.Properties["quote"].ShouldBe("Test Quote");
    }

    [Fact]
    public void QuoteBlock_Attribution_RoundTrips()
    {
        var block = new QuoteBlock();
        block.Attribution = "Test Attribution";
        block.Attribution.ShouldBe("Test Attribution");
        block.Properties["attribution"].ShouldBe("Test Attribution");
    }
}
