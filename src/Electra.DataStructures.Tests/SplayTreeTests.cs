using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using System;

namespace Electra.DataStructures.Tests;

public class SplayTreeTests
{
    [Fact]
    public void Insert_And_Splay_Test()
    {
        // Arrange
        var splay = new SplayTree<int>();
        splay.Insert(100);
        splay.Insert(50);
        splay.Insert(200);
        splay.Insert(40);
        splay.Insert(30);
        splay.Insert(20);
            
        // Act
        splay.Find(20); // Splay 20 to the root

        // Assert
        splay.Root.Value.Should().Be(20);
    }
}