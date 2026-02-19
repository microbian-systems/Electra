using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;

namespace Aero.DataStructures.Tests;

public class TemporalGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Vertex Tests

    [Fact]
    public void AddVertex_ShouldCreateTemporalVertex()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = DateTime.Now;
        var end = start.AddDays(30);

        var vertex = graph.AddVertex("user1", start, end);

        vertex.Id.Should().Be("user1");
        vertex.Lifetime.Start.Should().Be(start);
        vertex.Lifetime.End.Should().Be(end);
    }

    [Fact]
    public void AddVertex_ShouldSupportOpenEndedLifetime()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = DateTime.Now;
        var farFuture = DateTime.MaxValue;

        var vertex = graph.AddVertex("active", start, farFuture);

        vertex.Lifetime.Contains(DateTime.Now.AddYears(10)).Should().BeTrue();
    }

    [Fact]
    public void VertexExistsAt_ShouldReturnCorrectResult()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = new DateTime(2020, 1, 1);
        var end = new DateTime(2023, 1, 1);
        graph.AddVertex("temporal", start, end);

        var vertex = graph.GetVertex("temporal");

        vertex!.ExistsAt(new DateTime(2021, 6, 1)).Should().BeTrue();
        vertex.ExistsAt(new DateTime(2024, 1, 1)).Should().BeFalse();
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldCreateTemporalEdge()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = DateTime.Now;
        graph.AddVertex("a", start);
        graph.AddVertex("b", start);

        var edge = graph.AddEdge("a", "b", 1, start, start.AddDays(7));

        edge.Should().NotBeNull();
        edge!.Source.Should().Be("a");
        edge.Target.Should().Be("b");
        edge.Lifetime.Start.Should().Be(start);
    }

    [Fact]
    public void AddEdge_ShouldThrow_WhenVerticesNotExist()
    {
        var graph = new TemporalGraph<string, int, DateTime>();

        var act = () => graph.AddEdge("nonexistent1", "nonexistent2", 1, DateTime.Now);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void EdgeExistsAt_ShouldReturnCorrectResult()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = new DateTime(2020, 1, 1);
        var end = new DateTime(2021, 1, 1);
        graph.AddVertex("a", start);
        graph.AddVertex("b", start);
        graph.AddEdge("a", "b", 1, start, end);

        var edge = graph.GetEdge(1);

        edge!.ExistsAt(new DateTime(2020, 6, 1)).Should().BeTrue();
        edge.ExistsAt(new DateTime(2022, 1, 1)).Should().BeFalse();
    }

    #endregion

    #region TimeInterval Tests

    [Fact]
    public void TimeInterval_Contains_ShouldWorkCorrectly()
    {
        var start = new DateTime(2020, 1, 1);
        var end = new DateTime(2021, 1, 1);
        var interval = new TemporalGraph<string, int, DateTime>.TimeInterval(start, end);

        interval.Contains(new DateTime(2020, 6, 1)).Should().BeTrue();
        interval.Contains(new DateTime(2019, 12, 31)).Should().BeFalse();
        interval.Contains(new DateTime(2021, 1, 1)).Should().BeFalse();
    }

    [Fact]
    public void TimeInterval_Overlaps_ShouldWorkCorrectly()
    {
        var interval1 = new TemporalGraph<string, int, DateTime>.TimeInterval(
            new DateTime(2020, 1, 1), new DateTime(2020, 6, 1));
        var interval2 = new TemporalGraph<string, int, DateTime>.TimeInterval(
            new DateTime(2020, 3, 1), new DateTime(2020, 9, 1));

        interval1.Overlaps(interval2).Should().BeTrue();
    }

    #endregion

    #region Snapshot Tests

    [Fact]
    public void GetSnapshot_ShouldReturnGraphAtTime()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var t1 = new DateTime(2020, 1, 1);
        var t2 = new DateTime(2021, 1, 1);
        var tEnd = new DateTime(2025, 1, 1);
        
        graph.AddVertex("a", t1, tEnd);
        graph.AddVertex("b", t1, tEnd);
        graph.AddVertex("c", t2, tEnd);
        graph.AddEdge("a", "b", 1, t1, tEnd);

        var snapshot = graph.GetSnapshot(new DateTime(2020, 6, 1));

        snapshot.VertexCount.Should().Be(2);
        snapshot.ContainsVertex("a").Should().BeTrue();
        snapshot.ContainsVertex("c").Should().BeFalse();
    }

    [Fact]
    public void GetSnapshot_ShouldIncludeOnlyActiveEdges()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = new DateTime(2020, 1, 1);
        var farFuture = new DateTime(2030, 1, 1);
        graph.AddVertex("a", start, farFuture);
        graph.AddVertex("b", start, farFuture);
        graph.AddVertex("c", start, farFuture);
        
        graph.AddEdge("a", "b", 1, start, new DateTime(2020, 6, 1));
        graph.AddEdge("a", "c", 2, new DateTime(2021, 1, 1), farFuture);

        var snapshot = graph.GetSnapshot(new DateTime(2020, 3, 1));

        snapshot.EdgeCount.Should().Be(1);
        snapshot.ContainsEdge("a", "b").Should().BeTrue();
        snapshot.ContainsEdge("a", "c").Should().BeFalse();
    }

    #endregion

    #region Temporal Path Tests

    [Fact]
    public void GetTemporalPaths_ShouldRespectTimeOrder()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var t1 = new DateTime(2020, 1, 1);
        var t2 = new DateTime(2020, 2, 1);
        var t3 = new DateTime(2020, 3, 1);
        
        graph.AddVertex("a", t1);
        graph.AddVertex("b", t1);
        graph.AddVertex("c", t1);
        
        graph.AddEdge("a", "b", 1, t2);
        graph.AddEdge("b", "c", 2, t3);

        var paths = graph.GetTemporalPaths("a", "c", t1).ToList();

        paths.Should().NotBeEmpty();
        paths[0].Last().Vertex.Should().Be("c");
    }

    [Fact]
    public void GetEarliestArrival_ShouldComputeCorrectly()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var t1 = new DateTime(2020, 1, 1);
        var t2 = new DateTime(2020, 2, 1);
        
        graph.AddVertex("start", t1);
        graph.AddVertex("end", t1);
        graph.AddEdge("start", "end", 1, t2);

        var arrival = graph.GetEarliestArrival("start", "end", t1);

        arrival.Should().Be(t2);
    }

    [Fact]
    public void GetEarliestArrival_ShouldReturnNull_WhenUnreachable()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        graph.AddVertex("isolated", DateTime.Now, DateTime.MaxValue);
        graph.AddVertex("target", DateTime.Now, DateTime.MaxValue);

        var arrival = graph.GetEarliestArrival("isolated", "target", DateTime.Now);

        arrival.Equals(default(DateTime)).Should().BeTrue();
    }

    #endregion

    #region Edge Queries Tests

    [Fact]
    public void GetEdgesInInterval_ShouldFilterCorrectly()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var baseTime = DateTime.Now;
        graph.AddVertex("a", baseTime);
        graph.AddVertex("b", baseTime);
        graph.AddVertex("c", baseTime);
        
        graph.AddEdge("a", "b", 1, baseTime, baseTime.AddDays(10));
        graph.AddEdge("b", "c", 2, baseTime.AddDays(5), baseTime.AddDays(15));
        graph.AddEdge("a", "c", 3, baseTime.AddDays(20), baseTime.AddDays(30));

        var edges = graph.GetEdgesInInterval(baseTime, baseTime.AddDays(7)).ToList();

        edges.Select(e => e.Id).Should().Contain(new[] { 1, 2 });
        edges.Select(e => e.Id).Should().NotContain(3);
    }

    [Fact]
    public void GetChangePoints_ShouldReturnAllChanges()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var t1 = new DateTime(2020, 1, 1);
        var t2 = new DateTime(2020, 2, 1);
        var t3 = new DateTime(2020, 3, 1);
        
        graph.AddVertex("a", t1, t3);
        graph.AddVertex("b", t2);

        var changePoints = graph.GetChangePoints();

        changePoints.Should().Contain(new[] { t1, t2, t3 });
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveVertex_ShouldRemoveIncidentEdges()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = DateTime.Now;
        graph.AddVertex("remove", start);
        graph.AddVertex("keep", start);
        graph.AddEdge("remove", "keep", 1, start);

        graph.RemoveVertex("remove");

        graph.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void RemoveEdge_ShouldKeepVertices()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        var start = DateTime.Now;
        graph.AddVertex("a", start);
        graph.AddVertex("b", start);
        graph.AddEdge("a", "b", 1, start);

        graph.RemoveEdge(1);

        graph.VertexCount.Should().Be(2);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldResetGraph()
    {
        var graph = new TemporalGraph<string, int, DateTime>();
        graph.AddVertex("v1", DateTime.Now);
        graph.AddVertex("v2", DateTime.Now);
        graph.AddEdge("v1", "v2", 1, DateTime.Now);

        graph.Clear();

        graph.VertexCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void SocialNetworkEvolutionScenario_ShouldWorkCorrectly()
    {
        var graph = new TemporalGraph<string, long, DateTime>();
        var registration = new DateTime(2020, 1, 1);
        var farFuture = new DateTime(2030, 1, 1);
        
        graph.AddVertex("alice", registration, farFuture);
        graph.AddVertex("bob", registration.AddDays(30), farFuture);
        graph.AddVertex("charlie", registration.AddDays(60), farFuture);
        
        var friendshipStart = new DateTime(2020, 6, 1);
        var friendshipEnd = new DateTime(2022, 6, 1);
        graph.AddEdge("alice", "bob", 1, friendshipStart, friendshipEnd);
        graph.AddEdge("bob", "charlie", 2, new DateTime(2020, 7, 1), farFuture);
        
        var snapshot2021 = graph.GetSnapshot(new DateTime(2021, 1, 1));
        var snapshot2023 = graph.GetSnapshot(new DateTime(2023, 1, 1));

        snapshot2021.EdgeCount.Should().Be(2);
        snapshot2023.EdgeCount.Should().Be(1);
        snapshot2023.ContainsEdge("bob", "charlie").Should().BeTrue();
    }

    #endregion
}
