using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using System.Linq;

namespace Electra.DataStructures.Tests;

public class KdTreeTests
{
    [Fact]
    public void RangeSearch_Returns_Points_In_Range()
    {
        // Arrange
        var kdTree = new KdTree();
        kdTree.Insert(new Point(3, 6));
        kdTree.Insert(new Point(17, 15));
        kdTree.Insert(new Point(13, 15));
        kdTree.Insert(new Point(6, 12));

        var range = new Rect(5, 10, 15, 16);
            
        // Act
        var inRange = kdTree.RangeSearch(range).ToList();

        // Assert
        inRange.Should().HaveCount(2);
        inRange.Should().Contain(p => p.X == 13 && p.Y == 15);
        inRange.Should().Contain(p => p.X == 6 && p.Y == 12);
    }
        
    [Fact]
    public void NearestNeighbor_Returns_Closest_Point()
    {
        // Arrange
        var kdTree = new KdTree();
        kdTree.Insert(new Point(3, 6));
        kdTree.Insert(new Point(17, 15));
        kdTree.Insert(new Point(13, 15));
        kdTree.Insert(new Point(6, 12));
            
        // Act
        var nearest = kdTree.NearestNeighbor(new Point(5, 11));

        // Assert
        nearest.X.Should().Be(6);
        nearest.Y.Should().Be(12);
    }
}