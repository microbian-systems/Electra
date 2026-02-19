using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Search.Extractors;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class BlockTextExtractorTests
{
    [Fact]
    public void RichTextBlockExtractor_StripsHtml()
    {
        var extractor = new RichTextBlockExtractor();
        var block = new RichTextBlock { Html = "<h1>Hello</h1><p>World &amp; Space</p>" };
        
        var result = extractor.Extract(block);
        
        result.ShouldBe("Hello World & Space");
    }

    [Fact]
    public void RichTextBlockExtractor_ReturnsNull_ForEmptyHtml()
    {
        var extractor = new RichTextBlockExtractor();
        var block = new RichTextBlock { Html = "" };
        
        extractor.Extract(block).ShouldBeNull();
    }

    [Fact]
    public void MarkdownBlockExtractor_StripsSyntax()
    {
        var extractor = new MarkdownBlockExtractor();
        var block = new MarkdownBlock { Markdown = "# Header\n**Bold** and [Link](url)" };
        
        var result = extractor.Extract(block);
        
        result.ShouldBe("Header\nBold and Link");
    }

    [Fact]
    public void ImageBlockExtractor_ReturnsAltText()
    {
        var extractor = new ImageBlockExtractor();
        var block = new ImageBlock { Alt = "Beautiful Sunset" };
        
        extractor.Extract(block).ShouldBe("Beautiful Sunset");
    }

    [Fact]
    public void HeroBlockExtractor_ReturnsHeadingAndSubtext()
    {
        var extractor = new HeroBlockExtractor();
        var block = new HeroBlock { Heading = "Welcome", Subtext = "To the jungle" };
        
        extractor.Extract(block).ShouldBe("Welcome\nTo the jungle");
    }

    [Fact]
    public void QuoteBlockExtractor_ReturnsQuoteAndAttribution()
    {
        var extractor = new QuoteBlockExtractor();
        var block = new QuoteBlock { Quote = "To be or not to be", Attribution = "Shakespeare" };
        
        extractor.Extract(block).ShouldBe("To be or not to be\nShakespeare");
    }
}
