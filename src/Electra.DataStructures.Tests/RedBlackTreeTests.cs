using FluentAssertions;
using Electra.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class RedBlackTreeTests
{
    [Fact]
    public void Insert_And_Balance_Test()
    {
        // Arrange
        var rbt = new RedBlackTree<int>();
            
        // Act
        rbt.Insert(10);
        rbt.Insert(20);
        rbt.Insert(30);

        // Assert
        rbt.Root.Value.Should().Be(20);
        rbt.Root.Color.Should().Be(NodeColor.Black);
        ((RedBlackTreeNode<int>)rbt.Root.Left).Color.Should().Be(NodeColor.Black);
        ((RedBlackTreeNode<int>)rbt.Root.Right).Color.Should().Be(NodeColor.Black);
    }

    [Fact]
    public void Delete_And_Balance_Test()
    {
        // Arrange
        var rbt = new RedBlackTree<int>();
        rbt.Insert(10);
        rbt.Insert(5);
        rbt.Insert(15);
        rbt.Insert(3);
        rbt.Insert(7);
        rbt.Insert(12);
        rbt.Insert(18);

        // Act
        rbt.Delete(12);
        rbt.Delete(18);
        rbt.Delete(3);
            
        // Assert
        var foundNode = rbt.Find(12);
        foundNode.Should().BeNull();
    }
}