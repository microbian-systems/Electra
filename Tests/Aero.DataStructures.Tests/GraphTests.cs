using FluentAssertions;
using Aero.DataStructures.Graphs;
using Bogus;

namespace Aero.DataStructures.Tests;

public class GraphTests
{
    private readonly Faker _faker = new();

    [Fact]
    public void AddVertex_ShouldAddVertexToGraph()
    {
        // Arrange
        var graph = new Graph<string>();
        var vertex = _faker.Lorem.Word();

        // Act
        graph.AddVertex(vertex);

        // Assert
        var matrix = graph.GetAdjacencyMatrix();
        matrix.GetLength(0).Should().Be(1);
    }

    [Fact]
    public void AddVertex_ShouldNotDuplicateVertices()
    {
        // Arrange
        var graph = new Graph<string>();
        var vertex = "A";

        // Act
        graph.AddVertex(vertex);
        graph.AddVertex(vertex);

        // Assert
        var matrix = graph.GetAdjacencyMatrix();
        matrix.GetLength(0).Should().Be(1);
    }

    [Fact]
    public void AddEdge_ShouldAddEdgeCorrectly_Directed()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: true);
        graph.AddVertex("A");
        graph.AddVertex("B");

        // Act
        graph.AddEdge("A", "B", 5);

        // Assert
        // Matrix order depends on sorting, likely A then B
        var matrix = graph.GetAdjacencyMatrix();
        // A -> B should be 5
        // B -> A should be 0 (default)
        // Assuming A is index 0, B is index 1
        matrix[0, 1].Should().Be(5);
        matrix[1, 0].Should().Be(0);
    }

    [Fact]
    public void AddEdge_ShouldAddEdgeCorrectly_Undirected()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: false);
        graph.AddVertex("A");
        graph.AddVertex("B");

        // Act
        graph.AddEdge("A", "B", 5);

        // Assert
        var matrix = graph.GetAdjacencyMatrix();
        // A -> B should be 5
        // B -> A should be 5
        matrix[0, 1].Should().Be(5);
        matrix[1, 0].Should().Be(5);
    }
        
    [Fact]
    public void AddEdge_ShouldAutoAddVertices_IfTheyDoNotExist()
    {
        // Arrange
        var graph = new Graph<string>();

        // Act
        graph.AddEdge("A", "B", 1);

        // Assert
        var matrix = graph.GetAdjacencyMatrix();
        matrix.GetLength(0).Should().Be(2);
    }

    [Fact]
    public void Bfs_ShouldTraverseGraphCorrectly()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: true);
        // A -> B -> C
        // |
        // v
        // D
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 1);
        graph.AddEdge("A", "D", 1);

        // Act
        var result = graph.Bfs("A");

        // Assert
        // Order should be A, then neighbors of A (B, D), then neighbors of neighbors (C).
        // Since B and D are neighbors of A, the order between them depends on internal storage (Dictionary).
        // However, A must be first. C must be last (distance 2).
        result.First().Should().Be("A");
        result.Last().Should().Be("C");
        result.Should().Contain(new[] { "A", "B", "C", "D" });
        result.Count.Should().Be(4);
    }

    [Fact]
    public void Dfs_ShouldTraverseGraphCorrectly()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: true);
        // A -> B -> C
        // |
        // v
        // D
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 1);
        graph.AddEdge("A", "D", 1);

        // Act
        var result = graph.Dfs("A");

        // Assert
        // DFS uses a stack. 
        // Neighbors of A are B, D. 
        // If it pushes B then D, it pops D first -> A, D...
        // If it pushes D then B, it pops B first -> A, B, C...
        result.First().Should().Be("A");
        result.Should().Contain(new[] { "A", "B", "C", "D" });
        result.Count.Should().Be(4);
    }

    [Fact]
    public void Dijkstra_ShouldFindShortestPaths()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: true);
        // A -> B (1)
        // B -> C (2)
        // A -> C (10)
        // Shortest A -> C is A->B->C = 3
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 2);
        graph.AddEdge("A", "C", 10);

        // Act
        var distances = graph.Dijkstra("A");

        // Assert
        distances["A"].Should().Be(0);
        distances["B"].Should().Be(1);
        distances["C"].Should().Be(3);
    }

    [Fact]
    public void Dijkstra_ShouldHandleUnreachableNodes()
    {
        // Arrange
        var graph = new Graph<string>(isDirected: true);
        graph.AddVertex("A");
        graph.AddVertex("B"); 
        // No edge

        // Act
        var distances = graph.Dijkstra("A");

        // Assert
        distances["A"].Should().Be(0);
        distances["B"].Should().Be(int.MaxValue);
    }
}