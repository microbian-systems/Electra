using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;
using AutoFixture;
using Humanizer;

namespace Aero.DataStructures.Tests;

public class HeterogeneousGraphTests
{
    private readonly Faker _faker = new();
    private readonly Fixture _fixture = new();

    #region Node Tests

    [Fact]
    public void AddNode_ShouldCreateTypedNode()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        var nodeId = _fixture.Create<string>();
        var nodeType = "User".Humanize();

        var node = graph.AddNode(nodeId, nodeType);

        node.Should().NotBeNull();
        node!.Id.Should().Be(nodeId);
        node.Type.Should().Be(nodeType);
    }

    [Fact]
    public void AddNode_ShouldStoreAttributes()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        
        var node = graph.AddNode("user1", "User", new { name = "Alice", age = 30 });

        node!.GetAttribute<string>("name").Should().Be("Alice");
        node.GetAttribute<int>("age").Should().Be(30);
    }

    [Fact]
    public void AddNode_ShouldReturnNull_WhenDuplicate()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("duplicate", "Type");

        var result = graph.AddNode("duplicate", "OtherType");

        result.Should().BeNull();
    }

    [Fact]
    public void GetNodes_ShouldFilterByType()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("u1", "User");
        graph.AddNode("u2", "User");
        graph.AddNode("p1", "Product");

        var users = graph.GetNodes("User").ToList();

        users.Should().HaveCount(2);
        users.All(n => n.Type == "User").Should().BeTrue();
    }

    [Fact]
    public void NodeTypeCounts_ShouldBeAccurate()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("u1", "User");
        graph.AddNode("u2", "User");
        graph.AddNode("p1", "Product");

        graph.NodeTypeCounts["User"].Should().Be(2);
        graph.NodeTypeCounts["Product"].Should().Be(1);
    }

    #endregion

    #region Edge Tests

    [Fact]
    public void AddEdge_ShouldCreateTypedEdge()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("user1", "User");
        graph.AddNode("prod1", "Product");

        var edge = graph.AddEdge("user1", "prod1", "PURCHASED");

        edge.Should().NotBeNull();
        edge!.Type.Should().Be("PURCHASED");
        edge.SourceId.Should().Be("user1");
        edge.TargetId.Should().Be("prod1");
    }

    [Fact]
    public void AddEdge_ShouldStoreWeight()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("a", "Node");
        graph.AddNode("b", "Node");

        var edge = graph.AddEdge("a", "b", "LINKS", weight: 0.95);

        edge!.Weight.Should().Be(0.95);
    }

    [Fact]
    public void AddEdge_ShouldReturnNull_WhenNodesNotExist()
    {
        var graph = new HeterogeneousGraph<string, string, string>();

        var edge = graph.AddEdge("nonexistent1", "nonexistent2", "EDGE");

        edge.Should().BeNull();
    }

    [Fact]
    public void GetEdges_ShouldFilterByType()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("a", "Node");
        graph.AddNode("b", "Node");
        graph.AddNode("c", "Node");
        graph.AddEdge("a", "b", "FRIEND");
        graph.AddEdge("a", "c", "FRIEND");
        graph.AddEdge("b", "c", "ENEMY");

        var friendEdges = graph.GetEdges("FRIEND").ToList();

        friendEdges.Should().HaveCount(2);
    }

    [Fact]
    public void EdgeTypeCounts_ShouldBeAccurate()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("a", "Node");
        graph.AddNode("b", "Node");
        graph.AddNode("c", "Node");
        graph.AddEdge("a", "b", "KNOWS");
        graph.AddEdge("a", "c", "KNOWS");

        graph.EdgeTypeCounts["KNOWS"].Should().Be(2);
    }

    [Fact]
    public void AddEdge_ShouldAutoGenerateEdgeId()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("a", "Node");
        graph.AddNode("b", "Node");
        graph.AddNode("c", "Node");

        var edge1 = graph.AddEdge("a", "b", "LINKS");
        var edge2 = graph.AddEdge("b", "c", "LINKS");

        edge1!.Id.Should().Be(0);
        edge2!.Id.Should().Be(1);
    }

    #endregion

    #region Neighbor Tests

    [Fact]
    public void GetNeighbors_ShouldReturnConnectedNodes()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("center", "Node");
        graph.AddNode("n1", "Node");
        graph.AddNode("n2", "Node");
        graph.AddEdge("center", "n1", "LINKS");
        graph.AddEdge("n2", "center", "LINKS");

        var neighbors = graph.GetNeighbors("center").ToList();

        neighbors.Select(n => n.Id).Should().Contain(new[] { "n1", "n2" });
    }

    [Fact]
    public void GetNeighbors_WithType_ShouldFilter()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("center", "Node");
        graph.AddNode("friend1", "Node");
        graph.AddNode("enemy1", "Node");
        graph.AddEdge("center", "friend1", "FRIEND");
        graph.AddEdge("center", "enemy1", "ENEMY");

        var friends = graph.GetNeighbors("center", "FRIEND").ToList();

        friends.Should().ContainSingle();
        friends[0].Id.Should().Be("friend1");
    }

    #endregion

    #region Meta-Path Tests

    [Fact]
    public void FindMetaPaths_ShouldFindMatchingPaths()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("user1", "User");
        graph.AddNode("prod1", "Product");
        graph.AddNode("cat1", "Category");
        graph.AddEdge("user1", "prod1", "PURCHASED");
        graph.AddEdge("prod1", "cat1", "BELONGS_TO");

        var metaPath = new HeterogeneousGraph<string, string, string>.MetaPath
        {
            NodeTypes = new List<string> { "User", "Product", "Category" },
            EdgeTypes = new List<string> { "PURCHASED", "BELONGS_TO" }
        };

        var paths = graph.FindMetaPaths("user1", metaPath).ToList();

        paths.Should().ContainSingle();
        paths[0].Select(n => n.Id).Should().ContainInOrder("user1", "prod1", "cat1");
    }

    [Fact]
    public void ComputePathSimilarity_ShouldCalculateCorrectly()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("u1", "User");
        graph.AddNode("u2", "User");
        graph.AddNode("i1", "Item");
        graph.AddNode("i2", "Item");
        graph.AddEdge("u1", "i1", "LIKED");
        graph.AddEdge("u1", "i2", "LIKED");
        graph.AddEdge("u2", "i1", "LIKED");
        graph.AddEdge("u2", "i2", "LIKED");

        var metaPath = new HeterogeneousGraph<string, string, string>.MetaPath
        {
            NodeTypes = new List<string> { "User", "Item" },
            EdgeTypes = new List<string> { "LIKED" }
        };

        var similarity = graph.ComputePathSimilarity("u1", "u2", metaPath);

        similarity.Should().Be(1.0);
    }

    #endregion

    #region Schema Tests

    [Fact]
    public void GetSchema_ShouldReturnEdgeTypes()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("user", "User");
        graph.AddNode("product", "Product");
        graph.AddEdge("user", "product", "PURCHASED");

        var schema = graph.GetSchema();

        schema.Should().ContainKey(("User", "PURCHASED"));
        schema[("User", "PURCHASED")].Should().Be("Product");
    }

    #endregion

    #region Remove Tests

    [Fact]
    public void RemoveNode_ShouldRemoveIncidentEdges()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("remove", "Node");
        graph.AddNode("keep", "Node");
        graph.AddEdge("remove", "keep", "LINKS");

        graph.RemoveNode("remove");

        graph.EdgeCount.Should().Be(0);
    }

    [Fact]
    public void RemoveEdge_ShouldKeepNodes()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        graph.AddNode("a", "Node");
        graph.AddNode("b", "Node");
        var edge = graph.AddEdge("a", "b", "LINKS");

        graph.RemoveEdge(edge!.Id);

        graph.NodeCount.Should().Be(2);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_ShouldResetGraph()
    {
        var graph = new HeterogeneousGraph<int, string, string>();
        graph.AddNode(1, "Type1");
        graph.AddNode(2, "Type2");
        graph.AddEdge(1, 2, "CONNECTS");

        graph.Clear();

        graph.NodeCount.Should().Be(0);
        graph.EdgeCount.Should().Be(0);
    }

    #endregion

    #region Real-World Scenario Tests

    [Fact]
    public void ECommerceScenario_ShouldModelCorrectly()
    {
        var graph = new HeterogeneousGraph<string, string, string>();
        
        graph.AddNode("alice", "User", new { name = "Alice" });
        graph.AddNode("laptop", "Product", new { price = 999.99 });
        graph.AddNode("electronics", "Category", new { name = "Electronics" });
        graph.AddNode("techco", "Brand", new { name = "TechCo" });
        
        graph.AddEdge("alice", "laptop", "PURCHASED", new { date = DateTime.Now });
        graph.AddEdge("laptop", "electronics", "BELONGS_TO");
        graph.AddEdge("laptop", "techco", "MANUFACTURED_BY");

        graph.NodeCount.Should().Be(4);
        graph.EdgeCount.Should().Be(3);
        
        var aliceNeighbors = graph.GetNeighbors("alice", "PURCHASED").ToList();
        aliceNeighbors.Should().ContainSingle();
        aliceNeighbors[0].Id.Should().Be("laptop");
    }

    #endregion
}
