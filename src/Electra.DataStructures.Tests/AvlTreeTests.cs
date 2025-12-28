using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using System;

namespace Electra.DataStructures.Tests;

public class AvlTreeTests
{
    [Fact]
    public void Insert_And_Balance_Test()
    {
        // Arrange
        var avl = new AvlTree<int>();
            
        // Act
        avl.Insert(10);
        avl.Insert(20);
        avl.Insert(30); // This should trigger a left rotation

        // Assert
        avl.Root.Value.Should().Be(20);
        ((AvlTreeNode<int>)avl.Root.Left).Value.Should().Be(10);
        ((AvlTreeNode<int>)avl.Root.Right).Value.Should().Be(30);
    }
}