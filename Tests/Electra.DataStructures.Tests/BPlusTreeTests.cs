using FluentAssertions;
using Aero.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class BPlusTreeTests
{
    [Fact]
    public void Insert_And_Find_Success()
    {
        // Arrange
        var bptree = new BPlusTree<int>(3);
        bptree.Insert(10);
        bptree.Insert(20);
        bptree.Insert(30);
        bptree.Insert(40);
        bptree.Insert(50);

        // Act
        var found = bptree.Find(30);

        // Assert
        found.Should().Be(30);
    }

    [Fact]
    public void FindRange_Returns_Correct_Range()
    {
        // Arrange
        var bptree = new BPlusTree<int>(3);
        bptree.Insert(10);
        bptree.Insert(20);
        bptree.Insert(30);
        bptree.Insert(40);
        bptree.Insert(50);

        // Act
        var range = bptree.FindRange(20, 40).ToList();

        // Assert
        range.Should().HaveCount(3);
        range.Should().ContainInOrder(20, 30, 40);
    }
}