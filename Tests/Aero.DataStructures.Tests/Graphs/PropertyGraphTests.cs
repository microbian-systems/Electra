using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;
using NSubstitute;

namespace Aero.DataStructures.Tests;

public class PropertyGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Vertex Tests

    [Fact]
    public void AddVertex_ShouldCreateVertexWithLabel()
    {
        var graph = new PropertyGraph<string, long>();
        var id = _fixture.Create<string>();
        var label = "Person".Humanize();

        var vertex = graph.AddVertex(id, label);

        vertex.Should().NotBeNull();
        vertex!.Id.Should().Be(id);
        vertex.Label.Should().Be(label);
    }

    [Fact]
    public void AddVertex_ShouldStoreProperties()
    {
        var graph = new PropertyGraph<string, long>();
        var props = new Dictionary<string, object>
        {
            ["name"] = "Alice",
            ["age"] = 30
        };

        var vertex = graph.AddVertex("v1", "Person", props);

        vertex!.Properties["name"].Should().Be("Alice");
        vertex!.Properties["age"].Should().Be(30);
    }

    [Fact]
    public void AddVertex_ShouldReturnNull_WhenDuplicate()
    {
        var graph = new PropertyGraph<string, long>();
        graph.AddVertex("duplicate", "Type");

        var result = graph.AddVertex("duplicate", "OtherType");

        result.Should().BeNull();
    }

    [Fact]
    public void GetVertex_ShouldReturnVertex()
    {
        var graph = new PropertyGraph<int, long>();
        var id = _fixture.Create<int>();
        graph.AddVertex(id, "Node");

        var vertex = graph.GetVertex(id);

        vertex.Should().NotBeNull();
        vertex!.Id.Should().Be(id);
    }

    [Fact]
    public void GetVerticesByLabel_ShouldFilterCorrectly()
    {
        var graph = new PropertyGraph<string, long>();
        graph.AddVertex("p1", "Person");
        graph.AddVertex("p2", "Person");
        graph.AddVertex("c1", "Company");

        var persons = graph.GetVerticesByLabel("Person").ToList();

        persons.Should().HaveCount(2);
        persons.All(p => p.Label == "Person").Should().BeTrue();
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldCreateEdgeWithLabel()
    {
        var graph = new PropertyGraph<string, long>();
        graph.AddVertex("a", "Person");
        graph.AddVertex("b", "Person");

        var edge = graph.AddEdge("a", "b", 1, "KNOWS");

        edge.Should().NotBeNull();
        edge!.SourceId.Should().Be("a");
        edge.TargetId.Should().Be("b");
        edge.Label.Should().Be("KNOWS");
    }

    [Fact]
    public void AddEdge_ShouldStoreProperties()
    {
        var graph = new PropertyGraph<string, long>();
        graph.AddVertex("a", "Person");
        graph.AddVertex("b", "Person");
        var props = new Dictionary<string, object> { ["since"] = 2020 };

        var edge = graph.AddEdge("a", "b", 1, "FRIEND", props);

        edge!.Properties["since"].Should().Be(2020);
    }

    [Fact]
    public void AddEdge_ShouldReturnNull_WhenVerticesNotExist()
    {
        var graph = new PropertyGraph<string, long>();

        var edge = graph.AddEdge("nonexistent1", "nonexistent2", 1, "EDGE");

        edge.Should().BeNull();
    }

    [Fact]
    public void GetEdge_ShouldReturnEdge()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddEdge("a", "b", 42, "LINKS");

        var edge = graph.GetEdge(42);

        edge.Should().NotBeNull();
        edge!.Id.Should().Be(42);
    }

    #endregion

    #region Traversal Tests

    [Fact]
    public void GetOutEdges_ShouldReturnOutgoingEdges()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("center", "Node");
        graph.AddVertex("neighbor1", "Node");
        graph.AddVertex("neighbor2", "Node");
        graph.AddEdge("center", "neighbor1", 1, "CONNECTS");
        graph.AddEdge("center", "neighbor2", 2, "CONNECTS");

        var outEdges = graph.GetOutEdges("center").ToList();

        outEdges.Should().HaveCount(2);
    }

    [Fact]
    public void GetOutEdges_WithLabel_ShouldFilter()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddVertex("c", "Node");
        graph.AddEdge("a", "b", 1, "FRIEND");
        graph.AddEdge("a", "c", 2, "ENEMY");

        var friendEdges = graph.GetOutEdges("a", "FRIEND").ToList();

        friendEdges.Should().ContainSingle();
        friendEdges[0].TargetId.Should().Be("b");
    }

    [Fact]
    public void GetInEdges_ShouldReturnIncomingEdges()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddVertex("target", "Node");
        graph.AddEdge("a", "target", 1, "CONNECTS");
        graph.AddEdge("b", "target", 2, "CONNECTS");

        var inEdges = graph.GetInEdges("target").ToList();

        inEdges.Should().HaveCount(2);
    }

    [Fact]
    public void GetOutNeighbors_ShouldReturnTargetVertices()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("start", "Node");
        graph.AddVertex("end1", "Node");
        graph.AddVertex("end2", "Node");
        graph.AddEdge("start", "end1", 1, "LINKS");
        graph.AddEdge("start", "end2", 2, "LINKS");

        var neighbors = graph.GetOutNeighbors("start").ToList();

        neighbors.Select(n => n.Id).Should().Contain(new[] { "end1", "end2" });
    }

    #endregion

    #region Query Tests

    [Fact]
    public void FindVertices_ShouldFindByProperty()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("alice", "Person", new Dictionary<string, object> { ["city"] = "NYC" });
        graph.AddVertex("bob", "Person", new Dictionary<string, object> { ["city"] = "NYC" });
        graph.AddVertex("charlie", "Person", new Dictionary<string, object> { ["city"] = "LA" });

        var nycResidents = graph.FindVertices("city", "NYC").ToList();

        nycResidents.Should().HaveCount(2);
    }

    [Fact]
    public void FindEdges_ShouldFindByProperty()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddVertex("c", "Node");
        graph.AddEdge("a", "b", 1, "LINKS", new Dictionary<string, object> { ["weight"] = 5 });
        graph.AddEdge("b", "c", 2, "LINKS", new Dictionary<string, object> { ["weight"] = 5 });
        graph.AddEdge("a", "c", 3, "LINKS", new Dictionary<string, object> { ["weight"] = 10 });

        var weight5Edges = graph.FindEdges("weight", 5).ToList();

        weight5Edges.Should().HaveCount(2);
    }

    #endregion

    #region Pattern Matching Tests

    [Fact]
    public void MatchPattern_ShouldReturnMatchingTriples()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("alice", "Person");
        graph.AddVertex("bob", "Person");
        graph.AddVertex("charlie", "Person");
        graph.AddEdge("alice", "bob", 1, "KNOWS");
        graph.AddEdge("alice", "charlie", 2, "KNOWS");

        var patterns = graph.MatchPattern("alice", "KNOWS").ToList();

        patterns.Should().HaveCount(2);
        patterns.All(p => p.Source.Id == "alice").Should().BeTrue();
    }

    [Fact]
    public void MatchTwoHop_ShouldFindPaths()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddVertex("c", "Node");
        graph.AddVertex("d", "Node");
        graph.AddEdge("a", "b", 1, "FIRST");
        graph.AddEdge("b", "c", 2, "SECOND");
        graph.AddEdge("b", "d", 3, "SECOND");

        var results = graph.MatchTwoHop("a", "FIRST", "SECOND").ToList();

        results.Should().HaveCount(2);
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveVertex_ShouldRemoveIncidentEdges()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("remove", "Node");
        graph.AddVertex("keep", "Node");
        graph.AddEdge("remove", "keep", 1, "LINKS");
        graph.AddEdge("keep", "remove", 2, "LINKS");

        graph.RemoveVertex("remove");

        graph.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void RemoveEdge_ShouldNotRemoveVertices()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("a", "Node");
        graph.AddVertex("b", "Node");
        graph.AddEdge("a", "b", 1, "LINKS");

        graph.RemoveEdge(1);

        graph.VertexCount.Should().Be(2);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion

    #region Count Tests

    [Fact]
    public void Counts_ShouldBeAccurate()
    {
        var graph = new PropertyGraph<int, int>();
        var numVertices = 10;
        var numEdges = 5;

        for (int i = 0; i < numVertices; i++)
            graph.AddVertex(i, "Node");

        for (int i = 0; i < numEdges; i++)
            graph.AddEdge(i, i + 1, i, "LINKS");

        graph.VertexCount.Should().Be(numVertices);
        graph.EdgeCount.Should().Be(numEdges);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldRemoveAllData()
    {
        var graph = new PropertyGraph<string, int>();
        graph.AddVertex("v1", "Type", new Dictionary<string, object> { ["x"] = 1 });
        graph.AddVertex("v2", "Type");
        graph.AddEdge("v1", "v2", 1, "EDGE");

        graph.Clear();

        graph.VertexCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion
}
