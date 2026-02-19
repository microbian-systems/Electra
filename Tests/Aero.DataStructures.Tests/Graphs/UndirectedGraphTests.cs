using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;

namespace Aero.DataStructures.Tests;

public class UndirectedGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Vertex Tests

    [Fact]
    public void AddVertex_ShouldIncreaseVertexCount()
    {
        var graph = new UndirectedGraph<string>();
        var vertex = _faker.Lorem.Word();

        graph.AddVertex(vertex);

        graph.VertexCount.Should().Be(1);
    }

    [Fact]
    public void AddVertex_ShouldReturnTrue_WhenVertexIsNew()
    {
        var graph = new UndirectedGraph<int>();
        var vertex = _fixture.Create<int>();

        var result = graph.AddVertex(vertex);

        result.Should().BeTrue();
    }

    [Fact]
    public void AddVertex_ShouldReturnFalse_WhenVertexAlreadyExists()
    {
        var graph = new UndirectedGraph<string>();
        var vertex = _faker.Name.FirstName();

        graph.AddVertex(vertex);
        var result = graph.AddVertex(vertex);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    [InlineData(100)]
    public void AddVertex_ShouldHandleMultipleVertices(int count)
    {
        var graph = new UndirectedGraph<int>();
        var vertices = _fixture.CreateMany<int>(count);

        foreach (var v in vertices)
            graph.AddVertex(v);

        graph.VertexCount.Should().Be(count);
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldIncreaseEdgeCount()
    {
        var graph = new UndirectedGraph<string>();
        var v1 = "Alice".Humanize(LetterCasing.Title);
        var v2 = "Bob".Humanize(LetterCasing.Title);

        graph.AddEdge(v1, v2);

        graph.EdgeCount.Should().Be(1);
    }

    [Fact]
    public void AddEdge_ShouldAutoAddVertices()
    {
        var graph = new UndirectedGraph<string>();
        var v1 = _faker.Name.FirstName();
        var v2 = _faker.Name.FirstName();

        graph.AddEdge(v1, v2);

        graph.VertexCount.Should().Be(2);
        graph.ContainsVertex(v1).Should().BeTrue();
        graph.ContainsVertex(v2).Should().BeTrue();
    }

    [Fact]
    public void AddEdge_ShouldCreateBidirectionalConnection()
    {
        var graph = new UndirectedGraph<string>();
        var v1 = "node_1".Humanize();
        var v2 = "node_2".Humanize();

        graph.AddEdge(v1, v2);

        graph.ContainsEdge(v1, v2).Should().BeTrue();
        graph.ContainsEdge(v2, v1).Should().BeTrue();
    }

    [Fact]
    public void AddEdge_ShouldThrow_WhenSelfLoopAttempted()
    {
        var graph = new UndirectedGraph<string>();
        var vertex = _faker.Name.FirstName();

        var act = () => graph.AddEdge(vertex, vertex);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Self-loops*");
    }

    [Fact]
    public void AddEdge_ShouldNotDuplicateEdge()
    {
        var graph = new UndirectedGraph<int>();
        var v1 = _fixture.Create<int>();
        var v2 = _fixture.Create<int>();

        graph.AddEdge(v1, v2);
        graph.AddEdge(v1, v2);

        graph.EdgeCount.Should().Be(1);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveVertex_ShouldReturnTrue_WhenVertexExists()
    {
        var graph = new UndirectedGraph<string>();
        var vertex = _faker.Lorem.Word();
        graph.AddVertex(vertex);

        var result = graph.RemoveVertex(vertex);

        result.Should().BeTrue();
    }

    [Fact]
    public void RemoveVertex_ShouldReturnFalse_WhenVertexDoesNotExist()
    {
        var graph = new UndirectedGraph<string>();

        var result = graph.RemoveVertex("nonexistent");

        result.Should().BeFalse();
    }

    [Fact]
    public void RemoveVertex_ShouldRemoveAllIncidentEdges()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("A", "C");
        graph.AddEdge("B", "C");

        graph.RemoveVertex("A");

        graph.EdgeCount.Should().Be(1);
        graph.ContainsEdge("B", "C").Should().BeTrue();
    }

    [Fact]
    public void RemoveEdge_ShouldDecreaseEdgeCount()
    {
        var graph = new UndirectedGraph<int>();
        var v1 = _fixture.Create<int>();
        var v2 = _fixture.Create<int>();
        graph.AddEdge(v1, v2);

        graph.RemoveEdge(v1, v2);

        graph.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void RemoveEdge_ShouldRemoveBothDirections()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("X", "Y");

        graph.RemoveEdge("X", "Y");

        graph.ContainsEdge("X", "Y").Should().BeFalse();
        graph.ContainsEdge("Y", "X").Should().BeFalse();
    }

    #endregion

    #region Degree Tests

    [Fact]
    public void GetDegree_ShouldReturnCorrectDegree()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("center", "node1");
        graph.AddEdge("center", "node2");
        graph.AddEdge("center", "node3");

        graph.GetDegree("center").Should().Be(3);
    }

    [Fact]
    public void GetDegree_ShouldReturnZero_ForNonExistentVertex()
    {
        var graph = new UndirectedGraph<int>();

        graph.GetDegree(_fixture.Create<int>()).Should().Be(0);
    }

    #endregion

    #region Traversal Tests

    [Fact]
    public void BreadthFirstSearch_ShouldVisitAllConnectedVertices()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "D");

        var result = graph.BreadthFirstSearch("A").ToList();

        result.Should().HaveCount(4);
        result.Should().Contain(new[] { "A", "B", "C", "D" });
    }

    [Fact]
    public void BreadthFirstSearch_ShouldStartWithGivenVertex()
    {
        var graph = new UndirectedGraph<int>();
        var start = _fixture.Create<int>();
        graph.AddVertex(start);

        var result = graph.BreadthFirstSearch(start).First();

        result.Should().Be(start);
    }

    [Fact]
    public void DepthFirstSearch_ShouldVisitAllConnectedVertices()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("1", "2");
        graph.AddEdge("2", "3");
        graph.AddEdge("3", "4");

        var result = graph.DepthFirstSearch("1").ToList();

        result.Should().HaveCount(4);
    }

    [Fact]
    public void Traversal_ShouldReturnEmpty_WhenVertexNotFound()
    {
        var graph = new UndirectedGraph<string>();

        graph.BreadthFirstSearch("nonexistent").Should().BeEmpty();
        graph.DepthFirstSearch("nonexistent").Should().BeEmpty();
    }

    #endregion

    #region Shortest Path Tests

    [Fact]
    public void GetShortestPath_ShouldFindDirectPath()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");

        var path = graph.GetShortestPath("A", "B");

        path.Should().ContainInOrder("A", "B");
    }

    [Fact]
    public void GetShortestPath_ShouldFindMultiHopPath()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "D");

        var path = graph.GetShortestPath("A", "D");

        path.Should().ContainInOrder("A", "B", "C", "D");
        path.Count.Should().Be(4);
    }

    [Fact]
    public void GetShortestPath_ShouldReturnEmpty_WhenNoPathExists()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddVertex("isolated");
        graph.AddVertex("other");

        var path = graph.GetShortestPath("isolated", "other");

        path.Should().BeEmpty();
    }

    [Fact]
    public void GetShortestPath_ShouldReturnSameVertex_WhenSourceEqualsDestination()
    {
        var graph = new UndirectedGraph<int>();
        var vertex = _fixture.Create<int>();
        graph.AddVertex(vertex);

        var path = graph.GetShortestPath(vertex, vertex);

        path.Should().ContainSingle().Which.Should().Be(vertex);
    }

    #endregion

    #region Connectivity Tests

    [Fact]
    public void IsConnected_ShouldReturnTrue_ForConnectedGraph()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");
        graph.AddEdge("C", "D");

        graph.IsConnected().Should().BeTrue();
    }

    [Fact]
    public void IsConnected_ShouldReturnFalse_ForDisconnectedGraph()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddVertex("C");

        graph.IsConnected().Should().BeFalse();
    }

    [Fact]
    public void IsConnected_ShouldReturnTrue_ForEmptyGraph()
    {
        var graph = new UndirectedGraph<int>();

        graph.IsConnected().Should().BeTrue();
    }

    [Fact]
    public void IsConnected_ShouldReturnTrue_ForSingleVertex()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddVertex("alone");

        graph.IsConnected().Should().BeTrue();
    }

    #endregion

    #region Connected Components Tests

    [Fact]
    public void GetConnectedComponents_ShouldReturnSingleComponent_ForConnectedGraph()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");

        var components = graph.GetConnectedComponents().ToList();

        components.Should().HaveCount(1);
        components[0].Should().Contain(new[] { "A", "B", "C" });
    }

    [Fact]
    public void GetConnectedComponents_ShouldReturnMultipleComponents_ForDisconnectedGraph()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("C", "D");

        var components = graph.GetConnectedComponents().ToList();

        components.Should().HaveCount(2);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllVerticesAndEdges()
    {
        var graph = new UndirectedGraph<string>();
        graph.AddEdge("A", "B");
        graph.AddEdge("B", "C");

        graph.Clear();

        graph.VertexCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion
}
