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
}
