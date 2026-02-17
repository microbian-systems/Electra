using FluentAssertions;
using Aero.DataStructures.Trees;

namespace Electra.DataStructures.Tests;

public class IntervalTreeTests
{
    [Fact]
    public void SearchOverlapping_Returns_Correct_Intervals()
    {
        // Arrange
        var intervalTree = new IntervalTree();
        intervalTree.Insert(new Interval(15, 20));
        intervalTree.Insert(new Interval(10, 30));
        intervalTree.Insert(new Interval(17, 19));
        intervalTree.Insert(new Interval(5, 20));

        // Act
        var overlapping = intervalTree.SearchOverlapping(new Interval(12, 16)).ToList();

        // Assert
        overlapping.Should().HaveCount(3);
        overlapping.Should().Contain(i => i.Start == 15 && i.End == 20);
        overlapping.Should().Contain(i => i.Start == 10 && i.End == 30);
        overlapping.Should().Contain(i => i.Start == 5 && i.End == 20);
    }
}