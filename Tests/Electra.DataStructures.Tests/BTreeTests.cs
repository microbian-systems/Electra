using FluentAssertions;
using Aero.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class BTreeTests
{
    [Fact]
    public void Insert_And_Find_Success()
    {
        // Arrange
        var btree = new BTree<int>(2);
        btree.Insert(10);
        btree.Insert(20);
        btree.Insert(5);
        btree.Insert(6);
        btree.Insert(12);

        // Act
        var found = btree.Find(6);

        // Assert
        found.Should().BeTrue();
    }

    [Fact]
    public void Delete_And_Find_Fails()
    {
        // Arrange
        var btree = new BTree<int>(2);
        btree.Insert(10);
        btree.Insert(20);
        btree.Insert(5);
        btree.Insert(6);
        btree.Insert(12);
            
        // Act
        btree.Delete(6);
        var found = btree.Find(6);

        // Assert
        found.Should().BeFalse();
    }
}