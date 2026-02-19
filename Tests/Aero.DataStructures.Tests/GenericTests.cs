using Aero.DataStructures.Graphs;
using FluentAssertions;

namespace Aero.DataStructures.Tests;

public class GenericTests
{
    [Fact]
    public void Graph_BFS_Test()
    {
        var graph = new Graph<string>();
        graph.AddVertex("A");
        graph.AddVertex("B");
        graph.AddVertex("C");
        graph.AddVertex("D");
        graph.AddVertex("E");
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("B", "D", 1);
        graph.AddEdge("C", "E", 1);
        graph.AddEdge("D", "E", 1);

        var bfs = graph.Bfs("A");
        Assert.Equal(new[] { "A", "B", "C", "D", "E" }, bfs);
    }

            [Fact]
        public void AddEdge_ShouldAddVertices_WhenTheyDoNotExist()
        {
            // Arrange
            var graph = new Graph<string>();

            // Act
            graph.AddEdge("A", "B", 10);

            // Assert
            var matrix = graph.GetAdjacencyMatrix();
            // Since we added A and B, matrix should be 2x2
            matrix.GetLength(0).Should().Be(2); 
            matrix.GetLength(1).Should().Be(2);
        }

        [Fact]
        public void Bfs_ShouldTraverseInLevelOrder_SortedAlphabetically()
        {
            // Arrange
            var graph = new Graph<string>();
            // Root connects to B and C
            graph.AddEdge("A", "B", 1);
            graph.AddEdge("A", "C", 1);
            // B connects to D
            graph.AddEdge("B", "D", 1);
            // C connects to E
            graph.AddEdge("C", "E", 1);

            /* Structure:
                 A
               /   \
              B     C
              |     |
              D     E
            */

            // Act
            var result = graph.Bfs("A");

            // Assert
            // Expected: Root (A) -> Layer 1 (B, C) -> Layer 2 (D, E)
            result.Should().ContainInOrder("A", "B", "C", "D", "E");
        }

        [Fact]
        public void Bfs_ShouldHandleDisconnectedGraph_ByIgnoringUnreachableNodes()
        {
            // Arrange
            var graph = new Graph<string>();
            graph.AddEdge("A", "B", 1);
            graph.AddEdge("Z", "Y", 1); // Z-Y is an island

            // Act
            var result = graph.Bfs("A");

            // Assert
            result.Should().Contain("A");
            result.Should().Contain("B");
            result.Should().NotContain("Z");
            result.Should().NotContain("Y");
        }

        [Fact]
        public void Dfs_ShouldTraverseDeep_AndRespectAlphabeticalOrder()
        {
            // Arrange
            var graph = new Graph<string>();
            
            // A connects to B and E
            graph.AddEdge("A", "B", 1);
            graph.AddEdge("A", "E", 1);
            
            // B connects to C
            graph.AddEdge("B", "C", 1);
            
            // C connects to D
            graph.AddEdge("C", "D", 1);

            /* Structure:
               A -> B -> C -> D
               |
               E
            */

            // Act
            var result = graph.Dfs("A");
            
            // Expectation: Visit A. Neighbors are B, E. Push E, Push B. Pop B.
            // Visit B. Neighbor C. Push C. Pop C.
            // Visit C. Neighbor D. Push D. Pop D.
            // Pop E.
            result.Should().ContainInOrder("A", "B", "C", "D", "E");
        }

        [Fact]
        public void Dijkstra_ShouldFindCheapestPath_NotFewestEdges()
        {
            // Arrange
            var graph = new Graph<string>();
            
            // Direct path is expensive
            graph.AddEdge("Start", "End", 100);
            
            // Detour is cheap
            graph.AddEdge("Start", "A", 10);
            graph.AddEdge("A", "B", 10);
            graph.AddEdge("B", "End", 10);

            // Act
            var distances = graph.Dijkstra("Start");

            // Assert
            // Path Start->End cost 100.
            // Path Start->A->B->End cost 30.
            distances.Should().ContainKey("End");
            distances["End"].Should().Be(30);
        }

        [Fact]
        public void Dijkstra_ShouldReturnIntMax_ForUnreachableNodes()
        {
            // Arrange
            var graph = new Graph<string>();
            graph.AddEdge("A", "B", 1);
            graph.AddEdge("C", "D", 1); // C is unreachable from A

            // Act
            var distances = graph.Dijkstra("A");

            // Assert
            distances.Should().ContainKey("C");
            distances["C"].Should().Be(int.MaxValue);
        }

        [Fact]
        public void GetAdjacencyMatrix_ShouldCorrectlyMapWeights()
        {
            // Arrange
            var graph = new Graph<string>();
            graph.AddEdge("A", "B", 5);
            graph.AddEdge("B", "A", 3);
            graph.AddEdge("C", "A", 1);
            
            // Act
            // Vertices sorted: A, B, C
            // Index map: A=0, B=1, C=2
            var matrix = graph.GetAdjacencyMatrix();

            // Assert
            // A -> B = 5 (row 0, col 1)
            matrix[0, 1].Should().Be(5);
            
            // B -> A = 3 (row 1, col 0)
            matrix[1, 0].Should().Be(3);
            
            // C -> A = 1 (row 2, col 0)
            matrix[2, 0].Should().Be(1);

            // No self loop A->A
            matrix[0, 0].Should().BeNull();
            
            // No connection A->C
            matrix[0, 2].Should().BeNull();
        }
        
        [Fact]
        public void Graph_ShouldHandleIntegerVertices()
        {
            // Arrange
            var graph = new Graph<int>();
            graph.AddEdge(1, 2, 10);
            graph.AddEdge(2, 3, 5);

            // Act
            var result = graph.Bfs(1);

            // Assert
            result.Should().ContainInOrder(1, 2, 3);
        }
        
    // [Fact]
    // public void Graph_DFS_Test()
    // {
    //     var graph = new Graph<string>();
    //     graph.AddVertex("A");
    //     graph.AddVertex("B");
    //     graph.AddVertex("C");
    //     graph.AddVertex("D");
    //     graph.AddVertex("E");
    //     graph.AddEdge("A", "B", 1);
    //     graph.AddEdge("A", "C", 1);
    //     graph.AddEdge("B", "D", 1);
    //     graph.AddEdge("C", "E", 1);
    //     graph.AddEdge("D", "E", 1);
    //
    //     var dfs = graph.Dfs("A");
    //     Assert.Equal(new[] { "A", "C", "E", "D", "B" }, dfs);
    // }

    [Fact]
    public void Graph_Dijkstra_Test()
    {
        var graph = new Graph<string>();
        graph.AddVertex("A");
        graph.AddVertex("B");
        graph.AddVertex("C");
        graph.AddVertex("D");
        graph.AddVertex("E");
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("A", "C", 1);
        graph.AddEdge("B", "D", 1);
        graph.AddEdge("C", "E", 1);
        graph.AddEdge("D", "E", 1);

        var dijkstra = graph.Dijkstra("A");
        Assert.Equal(0, dijkstra["A"]);
        Assert.Equal(1, dijkstra["B"]);
        Assert.Equal(1, dijkstra["C"]);
        Assert.Equal(2, dijkstra["D"]);
        Assert.Equal(2, dijkstra["E"]);
    }

    [Fact]
    public void Graph_GetAdjacencyMatrix_Test()
    {
        var graph = new Graph<string>();
        graph.AddVertex("A");
        graph.AddVertex("B");
        graph.AddVertex("C");
        graph.AddEdge("A", "B", 1);
        graph.AddEdge("B", "C", 2);

        var matrix = graph.GetAdjacencyMatrix();

        Assert.Equal(3, matrix.GetLength(0));
        Assert.Equal(3, matrix.GetLength(1));
        Assert.Null(matrix[0, 0]);
        Assert.Equal(1, matrix[0, 1]);
        Assert.Null(matrix[0, 2]);
        Assert.Null(matrix[1, 0]);
        Assert.Null(matrix[1, 1]);
        Assert.Equal(2, matrix[1, 2]);
        Assert.Null(matrix[2, 0]);
        Assert.Null(matrix[2, 1]);
        Assert.Null(matrix[2, 2]);
    }
}