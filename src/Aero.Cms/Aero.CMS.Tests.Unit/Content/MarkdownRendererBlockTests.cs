using Aero.CMS.Core.Content.Models.Blocks;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class MarkdownRendererBlockTests
{
    [Fact]
    public void MarkdownRendererBlock_Type_Is_markdownRendererBlock()
    {
        var block = new MarkdownRendererBlock();
        block.Type.ShouldBe("markdownRendererBlock");
    }

    [Fact]
    public void MarkdownRendererBlock_MarkdownContent_RoundTrips()
    {
        var block = new MarkdownRendererBlock();
        block.MarkdownContent = "# Hello World";
        block.MarkdownContent.ShouldBe("# Hello World");
        block.Properties["markdownContent"].ShouldBe("# Hello World");
    }

    [Fact]
    public void MarkdownRendererBlock_UseTypographyStyles_Defaults_To_True()
    {
        var block = new MarkdownRendererBlock();
        block.UseTypographyStyles.ShouldBeTrue();
    }

    [Fact]
    public void MarkdownRendererBlock_UseTypographyStyles_RoundTrips()
    {
        var block = new MarkdownRendererBlock();
        block.UseTypographyStyles = false;
        block.UseTypographyStyles.ShouldBeFalse();
        block.Properties["useTypographyStyles"].ShouldBe(false);
    }

    [Fact]
    public void MarkdownRendererBlock_MaxWidth_Defaults_To_prose()
    {
        var block = new MarkdownRendererBlock();
        block.MaxWidth.ShouldBe("prose");
    }

    [Fact]
    public void MarkdownRendererBlock_MaxWidth_RoundTrips()
    {
        var block = new MarkdownRendererBlock();
        block.MaxWidth = "max-w-4xl";
        block.MaxWidth.ShouldBe("max-w-4xl");
        block.Properties["maxWidth"].ShouldBe("max-w-4xl");
    }

    [Fact]
    public void MarkdownRendererBlock_Has_NonEmpty_Guid_Id()
    {
        var block = new MarkdownRendererBlock();
        block.Id.ShouldNotBe(Guid.Empty);
    }
}