using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using System.Linq;

namespace Electra.DataStructures.Tests;

public class RTreeTests
{
    [Fact(Skip = "R-Tree insert is not fully implemented")]
    public void Insert_And_Search_Success()
    {
        // Arrange
        var rtree = new RTree();
        rtree.Insert(new Point(1, 1));
        rtree.Insert(new Point(5, 5));
        rtree.Insert(new Point(10, 10));

        var searchArea = new Mbr(new Point(0, 0), new Point(6, 6));
            
        // Act
        var found = rtree.Search(searchArea).ToList();

        // Assert
        found.Should().HaveCount(2);
        found.Should().Contain(p => p.X == 1 && p.Y == 1);
        found.Should().Contain(p => p.X == 5 && p.Y == 5);
    }
}