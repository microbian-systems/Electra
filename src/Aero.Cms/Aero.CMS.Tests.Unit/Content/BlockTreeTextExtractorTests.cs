using Aero.CMS.Core.Content.Interfaces;
using Aero.CMS.Core.Content.Models;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Content.Search;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Content;

public class BlockTreeTextExtractorTests
{
    private readonly BlockTreeTextExtractor _sut;
    private readonly IBlockTextExtractor _richTextExtractor;
    private readonly IBlockTextExtractor _imageExtractor;

    public BlockTreeTextExtractorTests()
    {
        _richTextExtractor = Substitute.For<IBlockTextExtractor>();
        _richTextExtractor.BlockType.Returns("richTextBlock");
        
        _imageExtractor = Substitute.For<IBlockTextExtractor>();
        _imageExtractor.BlockType.Returns("imageBlock");

        _sut = new BlockTreeTextExtractor(new[] { _richTextExtractor, _imageExtractor });
    }

    [Fact]
    public void Extract_ShouldExtractFromFlatList()
    {
        // Arrange
        var b1 = new RichTextBlock { SortOrder = 1 };
        var b2 = new RichTextBlock { SortOrder = 2 };
        _richTextExtractor.Extract(b1).Returns("First");
        _richTextExtractor.Extract(b2).Returns("Second");

        // Act
        var result = _sut.Extract(new[] { b1, b2 });

        // Assert
        result.ShouldBe("First\n\nSecond");
    }

    public class UnknownBlock : ContentBlock { public override string Type => "unknown"; }

    [Fact]
    public void Extract_ShouldExtractRecursively_InDfsOrder()
    {
        // Arrange
        var root = new DivBlock { SortOrder = 1 }; // No extractor for divBlock
        var child1 = new RichTextBlock { SortOrder = 1 };
        var child2 = new DivBlock { SortOrder = 2 };
        var grandchild = new RichTextBlock { SortOrder = 1 };

        root.Children.Add(child1);
        root.Children.Add(child2);
        child2.Children.Add(grandchild);

        _richTextExtractor.Extract(child1).Returns("Child 1");
        _richTextExtractor.Extract(grandchild).Returns("Grandchild");

        // Act
        var result = _sut.Extract(new[] { root });

        // Assert
        result.ShouldBe("Child 1\n\nGrandchild");
    }

    [Fact]
    public void Extract_ShouldSkipUnregisteredTypes()
    {
        // Arrange
        var block = new UnknownBlock { SortOrder = 1 };

        // Act
        var result = _sut.Extract(new[] { block });

        // Assert
        result.ShouldBe("");
    }

    [Fact]
    public void Extract_ShouldHandleEmptyList()
    {
        _sut.Extract(Array.Empty<ContentBlock>()).ShouldBe("");
    }
}
