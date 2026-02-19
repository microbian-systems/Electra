using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;
using Moq;

namespace Aero.DataStructures.Tests;

public class WeightedGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Constructor Tests

    [Fact]
    public void Constructor_ShouldCreateDirectedGraph_WhenSpecified()
    {
        var graph = new WeightedGraph<string, int>(directed: true);

        graph.IsDirected.Should().BeTrue();
    }

    [Fact]
    public void Constructor_ShouldCreateUndirectedGraph_ByDefault()
    {
        var graph = new WeightedGraph<string, int>(directed: false);

        graph.IsDirected.Should().BeFalse();
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldStoreWeight()
    {
        var graph = new WeightedGraph<string, int>();
        var weight = _faker.Random.Int(1, 100);

        graph.AddEdge("A", "B", weight);

        graph.TryGetWeight("A", "B", out var storedWeight).Should().BeTrue();
        storedWeight.Should().Be(weight);
    }

    [Fact]
    public void AddEdge_ShouldAutoAddVertices()
    {
        var graph = new WeightedGraph<int, double>();
        var v1 = _fixture.Create<int>();
        var v2 = _fixture.Create<int>();

        graph.AddEdge(v1, v2, 1.5);

        graph.VertexCount.Should().Be(2);
    }

    [Fact]
    public void AddEdge_Undirected_ShouldStoreBidirectional()
    {
        var graph = new WeightedGraph<string, int>(directed: false);
        graph.AddEdge("X", "Y", 10);

        graph.ContainsEdge("X", "Y").Should().BeTrue();
        graph.ContainsEdge("Y", "X").Should().BeTrue();
    }

    [Fact]
    public void AddEdge_Directed_ShouldStoreOneDirection()
    {
        var graph = new WeightedGraph<string, int>(directed: true);
        graph.AddEdge("X", "Y", 10);

        graph.ContainsEdge("X", "Y").Should().BeTrue();
        graph.ContainsEdge("Y", "X").Should().BeFalse();
    }

    [Fact]
    public void AddEdge_ShouldIncreaseEdgeCount()
    {
        var graph = new WeightedGraph<string, int>();
        
        graph.AddEdge("A", "B", 5);

        graph.EdgeCount.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100)]
    public void AddEdge_ShouldAcceptVariousWeights(int weight)
    {
        var graph = new WeightedGraph<string, int>();

        graph.AddEdge("A", "B", weight);

        graph.TryGetWeight("A", "B", out var w).Should().BeTrue();
        w.Should().Be(weight);
    }

    #endregion

    #region Weight Retrieval Tests

    [Fact]
    public void TryGetWeight_ShouldReturnFalse_WhenEdgeDoesNotExist()
    {
        var graph = new WeightedGraph<string, int>();

        graph.TryGetWeight("nonexistent", "edge", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGetWeight_ShouldReturnCorrectWeight()
    {
        var graph = new WeightedGraph<string, double>();
        var expectedWeight = _faker.Random.Double(0.1, 100.0);
        graph.AddEdge("from", "to", expectedWeight);

        graph.TryGetWeight("from", "to", out var weight).Should().BeTrue();
        weight.Should().BeApproximately(expectedWeight, 0.0001);
    }

    #endregion

    #region GetEdges Tests

    [Fact]
    public void GetEdges_ShouldReturnAllEdgesWithWeights()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 2);

        var edges = graph.GetEdges().ToList();

        edges.Should().HaveCount(2);
        edges.Should().Contain(e => e.Source == "A" && e.Destination == "B" && e.Weight == 1);
        edges.Should().Contain(e => e.Source == "B" && e.Destination == "C" && e.Weight == 2);
    }

    #endregion

    #region Neighbors Tests

    [Fact]
    public void GetNeighborsWithWeights_ShouldReturnCorrectData()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("center", "n1", 10);
        graph.AddEdge("center", "n2", 20);

        var neighbors = graph.GetNeighborsWithWeights("center");

        neighbors.Should().ContainKey("n1").WhoseValue.Should().Be(10);
        neighbors.Should().ContainKey("n2").WhoseValue.Should().Be(20);
    }

    #endregion

    #region Dijkstra Tests

    [Fact]
    public void Dijkstra_ShouldFindShortestPaths()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 2);
        graph.AddEdge("A", "C", 10);

        var distances = graph.Dijkstra("A");

        distances["A"].Should().Be(0);
        distances["B"].Should().Be(1);
        distances["C"].Should().Be(3);
    }

    [Fact]
    public void Dijkstra_ShouldThrow_WhenStartVertexNotFound()
    {
        var graph = new WeightedGraph<string, int>();

        var act = () => graph.Dijkstra("nonexistent");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Dijkstra_ShouldHandleDisconnectedVertices()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddVertex("A");
        graph.AddVertex("B");

        var distances = graph.Dijkstra("A");

        distances["B"].Should().Be(int.MaxValue);
    }

    #endregion

    #region Shortest Path Tests

    [Fact]
    public void GetShortestPath_ShouldReturnCorrectPath()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 1);
        graph.AddEdge("A", "C", 10);

        var (path, weight) = graph.GetShortestPath("A", "C");

        path.Should().ContainInOrder("A", "B", "C");
        weight.Should().Be(2);
    }

    [Fact]
    public void GetShortestPath_ShouldReturnEmpty_WhenNoPath()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddVertex("A");
        graph.AddVertex("B");

        var (path, _) = graph.GetShortestPath("A", "B");

        path.Should().BeEmpty();
    }

    #endregion

    #region MST Tests

    [Fact]
    public void GetMinimumSpanningTree_ShouldReturnCorrectEdges()
    {
        var graph = new WeightedGraph<string, int>(directed: false);
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 2);
        graph.AddEdge("A", "C", 5);

        var mst = graph.GetMinimumSpanningTree();

        mst.EdgeCount.Should().Be(2);
        mst.ContainsEdge("A", "B").Should().BeTrue();
        mst.ContainsEdge("B", "C").Should().BeTrue();
    }

    [Fact]
    public void GetMinimumSpanningTree_ShouldThrow_ForDirectedGraph()
    {
        var graph = new WeightedGraph<string, int>(directed: true);

        var act = () => graph.GetMinimumSpanningTree();

        act.Should().Throw<InvalidOperationException>();
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveEdge_ShouldRemoveWeight()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("A", "B", 5);

        graph.RemoveEdge("A", "B");

        graph.TryGetWeight("A", "B", out _).Should().BeFalse();
    }

    [Fact]
    public void RemoveVertex_ShouldRemoveAllIncidentEdges()
    {
        var graph = new WeightedGraph<string, int>();
        graph.AddEdge("X", "A", 1);
        graph.AddEdge("A", "Y", 2);

        graph.RemoveVertex("A");

        graph.ContainsEdge("X", "A").Should().BeFalse();
        graph.ContainsEdge("A", "Y").Should().BeFalse();
    }

    #endregion

    #region Generic Weight Type Tests

    [Fact]
    public void WeightedGraph_ShouldWorkWithDoubleWeights()
    {
        var graph = new WeightedGraph<string, double>();
        
        graph.AddEdge("A", "B", 1.5);
        graph.AddEdge("B", "C", 2.7);

        var distances = graph.Dijkstra("A");
        distances["C"].Should().BeApproximately(4.2, 0.0001);
    }

    [Fact]
    public void WeightedGraph_ShouldWorkWithLongWeights()
    {
        var graph = new WeightedGraph<int, long>();
        var w1 = _fixture.Create<long>();
        var w2 = _fixture.Create<long>();

        graph.AddEdge(1, 2, w1);
        graph.AddEdge(2, 3, w2);

        graph.TryGetWeight(1, 2, out var stored).Should().BeTrue();
        stored.Should().Be(w1);
    }

    #endregion
}
