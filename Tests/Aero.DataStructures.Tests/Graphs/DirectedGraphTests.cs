using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;
using NSubstitute;
using FakeItEasy;

namespace Aero.DataStructures.Tests;

public class DirectedGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Vertex Tests

    [Fact]
    public void AddVertex_ShouldIncreaseVertexCount()
    {
        var graph = new DirectedGraph<string>();
        var vertex = _faker.Internet.UserName();

        graph.AddVertex(vertex);

        graph.VertexCount.Should().Be(1);
    }

    [Fact]
    public void AddVertex_ShouldReturnTrue_WhenVertexIsNew()
    {
        var graph = new DirectedGraph<int>();
        var vertex = _fixture.Create<int>();

        var result = graph.AddVertex(vertex);

        result.Should().BeTrue();
    }

    [Fact]
    public void AddVertex_ShouldReturnFalse_WhenAlreadyExists()
    {
        var graph = new DirectedGraph<string>();
        var vertex = _faker.Name.FirstName();
        graph.AddVertex(vertex);

        var result = graph.AddVertex(vertex);

        result.Should().BeFalse();
    }

    [Fact]
    public void AddVertex_ShouldInitializeEmptyEdgeLists()
    {
        var graph = new DirectedGraph<string>();
        var vertex = "test_vertex".Humanize();

        graph.AddVertex(vertex);

        graph.GetOutDegree(vertex).Should().Be(0);
        graph.GetInDegree(vertex).Should().Be(0);
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldIncreaseEdgeCount()
    {
        var graph = new DirectedGraph<string>();
        var source = "follower".Humanize();
        var target = "following".Humanize();

        graph.AddEdge(source, target);

        graph.EdgeCount.Should().Be(1);
    }

    [Fact]
    public void AddEdge_ShouldCreateOneWayConnection()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");

        graph.ContainsEdge("A", "B").Should().BeTrue();
        graph.ContainsEdge("B", "A").Should().BeFalse();
    }

    [Fact]
    public void AddEdge_ShouldAutoAddVertices()
    {
        var graph = new DirectedGraph<int>();
        var v1 = _fixture.Create<int>();
        var v2 = _fixture.Create<int>();

        graph.AddEdge(v1, v2);

        graph.VertexCount.Should().Be(2);
    }

    [Fact]
    public void AddEdge_ShouldAllowReverseEdge()
    {
        var graph = new DirectedGraph<string>();
        
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "A");

        graph.EdgeCount.Should().Be(2);
        graph.ContainsEdge("A", "B").Should().BeTrue();
        graph.ContainsEdge("B", "A").Should().BeTrue();
    }

    [Fact]
    public void AddEdge_ShouldNotDuplicate()
    {
        var graph = new DirectedGraph<string>();
        
        graph.AddEdge("X", "Y");
        graph.AddEdge("X", "Y");

        graph.EdgeCount.Should().Be(1);
    }

    #endregion

    #region Degree Tests

    [Fact]
    public void GetOutDegree_ShouldReturnCorrectValue()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("center", "a");
        graph.AddEdge("center", "b");
        graph.AddEdge("center", "c");

        graph.GetOutDegree("center").Should().Be(3);
    }

    [Fact]
    public void GetInDegree_ShouldReturnCorrectValue()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("a", "center");
        graph.AddEdge("b", "center");
        graph.AddEdge("c", "center");

        graph.GetInDegree("center").Should().Be(3);
    }

    [Fact]
    public void Degree_ShouldHandleBothInAndOut()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("x", "y");
        graph.AddEdge("y", "z");

        graph.GetInDegree("y").Should().Be(1);
        graph.GetOutDegree("y").Should().Be(1);
    }

    #endregion

    #region Neighbor Tests

    [Fact]
    public void GetOutNeighbors_ShouldReturnCorrectVertices()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");

        var neighbors = graph.GetOutNeighbors("A");

        neighbors.Should().Contain(new[] { "B", "C" });
    }

    [Fact]
    public void GetInNeighbors_ShouldReturnCorrectVertices()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("B", "A");
        graph.AddEdge("C", "A");

        var neighbors = graph.GetInNeighbors("A");

        neighbors.Should().Contain(new[] { "B", "C" });
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveVertex_ShouldRemoveOutgoingEdges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("remove", "keep");
        graph.AddEdge("keep", "other");

        graph.RemoveVertex("remove");

        graph.ContainsEdge("remove", "keep").Should().BeFalse();
        graph.ContainsVertex("keep").Should().BeTrue();
    }

    [Fact]
    public void RemoveVertex_ShouldRemoveIncomingEdges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("keep", "remove");

        graph.RemoveVertex("remove");

        graph.ContainsEdge("keep", "remove").Should().BeFalse();
    }

    [Fact]
    public void RemoveEdge_ShouldOnlyRemoveSpecifiedDirection()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "A");

        graph.RemoveEdge("A", "B");

        graph.ContainsEdge("A", "B").Should().BeFalse();
        graph.ContainsEdge("B", "A").Should().BeTrue();
    }

    #endregion

    #region Traversal Tests

    [Fact]
    public void BreadthFirstSearch_ShouldFollowOutgoingEdges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");

        var result = graph.BreadthFirstSearch("A").ToList();

        result.First().Should().Be("A");
        result.Should().Contain(new[] { "B", "C", "D" });
    }

    [Fact]
    public void DepthFirstSearch_ShouldFollowOutgoingEdges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("1", "2");
        graph.AddEdge("1", "3");
        graph.AddEdge("2", "4");

        var result = graph.DepthFirstSearch("1").ToList();

        result.First().Should().Be("1");
        result.Should().HaveCount(4);
    }

    [Fact]
    public void Traversal_ShouldNotFollowIncomingEdges()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("B", "A");
        graph.AddEdge("C", "A");

        var result = graph.BreadthFirstSearch("A").ToList();

        result.Should().ContainSingle("A");
    }

    #endregion

    #region Topological Sort Tests

    [Fact]
    public void TopologicalSort_ShouldReturnValidOrder()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("compile", "test");
        graph.AddEdge("test", "deploy");

        var result = graph.TopologicalSort();

        result.IndexOf("compile").Should().BeLessThan(result.IndexOf("test"));
        result.IndexOf("test").Should().BeLessThan(result.IndexOf("deploy"));
    }

    [Fact]
    public void TopologicalSort_ShouldReturnEmpty_WhenCycleExists()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");

        var result = graph.TopologicalSort();

        result.Should().BeEmpty();
    }

    [Fact]
    public void TopologicalSort_ShouldHandleDiamondDependency()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "D");
        graph.AddEdge("C", "D");

        var result = graph.TopologicalSort();

        result.IndexOf("A").Should().BeLessThan(result.IndexOf("B"));
        result.IndexOf("A").Should().BeLessThan(result.IndexOf("C"));
        result.IndexOf("B").Should().BeLessThan(result.IndexOf("D"));
        result.IndexOf("C").Should().BeLessThan(result.IndexOf("D"));
    }

    #endregion

    #region Cycle Detection Tests

    [Fact]
    public void HasCycle_ShouldReturnTrue_WhenCycleExists()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");

        graph.HasCycle().Should().BeTrue();
    }

    [Fact]
    public void HasCycle_ShouldReturnFalse_WhenNoCycle()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");

        graph.HasCycle().Should().BeFalse();
    }

    [Fact]
    public void HasCycle_ShouldDetectSelfLoop()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "A");

        graph.HasCycle().Should().BeTrue();
    }

    #endregion

    #region Reachability Tests

    [Fact]
    public void GetReachableVertices_ShouldReturnAllReachable()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "D");
        graph.AddVertex("E");

        var reachable = graph.GetReachableVertices("A");

        reachable.Should().Contain(new[] { "A", "B", "C", "D" });
        reachable.Should().NotContain("E");
    }

    #endregion

    #region Strongly Connected Components Tests

    [Fact]
    public void GetStronglyConnectedComponents_ShouldIdentifyScc()
    {
        var graph = new DirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "A");
        graph.AddVertex("D");

        var sccs = graph.GetStronglyConnectedComponents().ToList();

        sccs.Should().HaveCount(2);
        sccs.Should().ContainSingle(c => c.Contains("A") && c.Contains("B") && c.Contains("C"));
        sccs.Should().ContainSingle(c => c.Contains("D"));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldResetGraph()
    {
        var graph = new DirectedGraph<int>();
        var vertices = _fixture.CreateMany<int>(10);
        foreach (var v in vertices)
        {
            graph.AddVertex(v);
        }

        graph.Clear();

        graph.VertexCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion
}
