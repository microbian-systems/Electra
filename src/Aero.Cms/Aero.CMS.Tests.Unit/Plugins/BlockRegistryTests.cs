using System;
using Aero.CMS.Core.Content.Models.Blocks;
using Aero.CMS.Core.Plugins;
using Xunit;

namespace Aero.CMS.Tests.Unit.Plugins;

public class BlockRegistryTests
{
    private readonly BlockRegistry _sut;

    public BlockRegistryTests()
    {
        _sut = new BlockRegistry();
    }

    private class TestView { }

    [Fact]
    public void Register_Generic_UsesBlockTypeAsAlias()
    {
        // Act
        _sut.Register<RichTextBlock, TestView>();

        // Assert
        var resolved = _sut.Resolve("richTextBlock");
        Assert.Equal(typeof(TestView), resolved);
    }

    [Fact]
    public void Resolve_ReturnsNullForUnregisteredAlias()
    {
        // Act
        var resolved = _sut.Resolve("unknown");

        // Assert
        Assert.Null(resolved);
    }

    [Fact]
    public void Register_ExplicitAlias_Works()
    {
        // Act
        _sut.Register("custom", typeof(TestView));

        // Assert
        var resolved = _sut.Resolve("custom");
        Assert.Equal(typeof(TestView), resolved);
    }

    [Fact]
    public void GetAll_ReturnsAllEntries()
    {
        // Arrange
        _sut.Register<RichTextBlock, TestView>();
        _sut.Register("other", typeof(string));

        // Act
        var all = _sut.GetAll();

        // Assert
        Assert.Equal(2, all.Count);
        Assert.Contains("richTextBlock", all.Keys);
        Assert.Contains("other", all.Keys);
    }

    [Fact]
    public void ReRegistering_OverwritesPrevious()
    {
        // Arrange
        _sut.Register("same", typeof(int));

        // Act
        _sut.Register("same", typeof(string));

        // Assert
        var resolved = _sut.Resolve("same");
        Assert.Equal(typeof(string), resolved);
    }
}
