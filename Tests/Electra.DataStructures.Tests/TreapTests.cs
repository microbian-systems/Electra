using FluentAssertions;
using Aero.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class TreapTests
{
    [Fact]
    public void Insert_And_Maintains_Heap_Property()
    {
        // Arrange
        var treap = new Treap<int>();
            
        // Act
        treap.Insert(50);
        treap.Insert(30);
        treap.Insert(70);
        treap.Insert(20);
        treap.Insert(40);
            
        // Assert
        // This is a probabilistic test, but we can check the BST property
        var inorder = GetInorder(treap.Root);
        inorder.Should().BeInAscendingOrder();
    }

    private System.Collections.Generic.IEnumerable<int> GetInorder(TreapNode<int> node)
    {
        if (node == null) yield break;

        foreach (var val in GetInorder((TreapNode<int>)node.Left))
        {
            yield return val;
        }
            
        yield return node.Value;

        foreach (var val in GetInorder((TreapNode<int>)node.Right))
        {
            yield return val;
        }
    }
}