using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class SegmentTreeTests
{
    [Fact]
    public void Query_Returns_Correct_Sum()
    {
        // Arrange
        var data = new[] { 1, 3, 5, 7, 9, 11 };
        var segTree = new SegmentTree(data);

        // Act
        var sum = segTree.Query(1, 3);

        // Assert
        sum.Should().Be(15); // 3 + 5 + 7
    }

    [Fact]
    public void Update_And_Query_Returns_Correct_Sum()
    {
        // Arrange
        var data = new[] { 1, 3, 5, 7, 9, 11 };
        var segTree = new SegmentTree(data);
            
        // Act
        segTree.Update(2, 6);
        var sum = segTree.Query(1, 3);

        // Assert
        sum.Should().Be(16); // 3 + 6 + 7
    }
}