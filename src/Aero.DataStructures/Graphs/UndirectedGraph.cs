using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents an undirected graph where edges have no direction.
/// An edge between vertices A and B can be traversed in both directions.
/// </summary>
/// <typeparam name="T">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// In an undirected graph, each edge connects two vertices symmetrically.
/// If an edge exists between A and B, it is equivalent to an edge between B and A.
/// </para>
/// <para>
/// Mathematical properties:
/// <list type="bullet">
/// <item><description>Edges are unordered pairs: {A, B}</description></item>
/// <item><description>Maximum edges in a simple graph: n(n-1)/2 where n = vertex count</description></item>
/// <item><description>Sum of all vertex degrees = 2 × (number of edges)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var socialNetwork = new UndirectedGraph&lt;string&gt;();
/// socialNetwork.AddEdge("Alice", "Bob");
/// socialNetwork.AddEdge("Alice", "Charlie");
/// socialNetwork.AddEdge("Bob", "Charlie");
/// 
/// // Alice is connected to Bob and Charlie
/// var aliceFriends = socialNetwork.GetNeighbors("Alice"); // [Bob, Charlie]
/// 
/// // Since it's undirected, Bob is also connected to Alice
/// var bobFriends = socialNetwork.GetNeighbors("Bob"); // [Alice, Charlie]
/// </code>
/// </example>
public class UndirectedGraph<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _adjacencyList = new();
    private int _edgeCount;

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    public int VertexCount => _adjacencyList.Count;

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    public int EdgeCount => _edgeCount;

    /// <summary>
    /// Adds a vertex to the graph if it doesn't already exist.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added; false if it already existed.</returns>
    public bool AddVertex(T vertex)
    {
        if (_adjacencyList.ContainsKey(vertex))
            return false;
        
        _adjacencyList[vertex] = new HashSet<T>();
        return true;
    }

    /// <summary>
    /// Adds an undirected edge between two vertices.
    /// Both vertices are added if they don't exist.
    /// </summary>
    /// <param name="vertex1">The first vertex.</param>
    /// <param name="vertex2">The second vertex.</param>
    /// <exception cref="ArgumentException">Thrown when vertex1 equals vertex2 (self-loops not allowed in simple graphs).</exception>
    public void AddEdge(T vertex1, T vertex2)
    {
        if (EqualityComparer<T>.Default.Equals(vertex1, vertex2))
            throw new ArgumentException("Self-loops are not allowed in a simple undirected graph.");

        AddVertex(vertex1);
        AddVertex(vertex2);

        if (_adjacencyList[vertex1].Add(vertex2))
        {
            _adjacencyList[vertex2].Add(vertex1);
            _edgeCount++;
        }
    }

    /// <summary>
    /// Removes a vertex and all its incident edges from the graph.
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed; false if it didn't exist.</returns>
    public bool RemoveVertex(T vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var neighbors))
            return false;

        _edgeCount -= neighbors.Count;

        foreach (var neighbor in neighbors)
        {
            _adjacencyList[neighbor].Remove(vertex);
        }

        _adjacencyList.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes the edge between two vertices.
    /// </summary>
    /// <param name="vertex1">The first vertex.</param>
    /// <param name="vertex2">The second vertex.</param>
    /// <returns>True if the edge was removed; false if it didn't exist.</returns>
    public bool RemoveEdge(T vertex1, T vertex2)
    {
        if (!_adjacencyList.TryGetValue(vertex1, out var neighbors1))
            return false;

        if (!neighbors1.Remove(vertex2))
            return false;

        _adjacencyList[vertex2].Remove(vertex1);
        _edgeCount--;
        return true;
    }

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    public bool ContainsVertex(T vertex) => _adjacencyList.ContainsKey(vertex);

    /// <summary>
    /// Determines whether an edge exists between two vertices.
    /// </summary>
    public bool ContainsEdge(T vertex1, T vertex2) =>
        _adjacencyList.TryGetValue(vertex1, out var neighbors) && neighbors.Contains(vertex2);

    /// <summary>
    /// Gets the degree of a vertex (number of incident edges).
    /// </summary>
    /// <param name="vertex">The vertex to get the degree of.</param>
    /// <returns>The degree of the vertex, or 0 if the vertex doesn't exist.</returns>
    public int GetDegree(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets all vertices adjacent to the specified vertex.
    /// </summary>
    public IReadOnlyCollection<T> GetNeighbors(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IReadOnlyCollection<T> GetVertices() => _adjacencyList.Keys;

    /// <summary>
    /// Performs a breadth-first traversal starting from the specified vertex.
    /// </summary>
    public IEnumerable<T> BreadthFirstSearch(T startVertex)
    {
        if (!_adjacencyList.ContainsKey(startVertex))
            yield break;

        var visited = new HashSet<T> { startVertex };
        var queue = new Queue<T>();
        queue.Enqueue(startVertex);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var neighbor in _adjacencyList[current])
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
    }

    /// <summary>
    /// Performs a depth-first traversal starting from the specified vertex.
    /// </summary>
    public IEnumerable<T> DepthFirstSearch(T startVertex)
    {
        if (!_adjacencyList.ContainsKey(startVertex))
            yield break;

        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(startVertex);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if (!visited.Add(current))
                continue;

            yield return current;

            foreach (var neighbor in _adjacencyList[current])
            {
                if (!visited.Contains(neighbor))
                    stack.Push(neighbor);
            }
        }
    }

    /// <summary>
    /// Finds the shortest path between two vertices using BFS.
    /// </summary>
    /// <returns>A list of vertices representing the path, or empty if no path exists.</returns>
    public List<T> GetShortestPath(T source, T destination)
    {
        if (!_adjacencyList.ContainsKey(source) || !_adjacencyList.ContainsKey(destination))
            return new List<T>();

        if (EqualityComparer<T>.Default.Equals(source, destination))
            return new List<T> { source };

        var visited = new HashSet<T> { source };
        var queue = new Queue<T>();
        var parentMap = new Dictionary<T, T>();

        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in _adjacencyList[current])
            {
                if (!visited.Add(neighbor))
                    continue;

                parentMap[neighbor] = current;

                if (EqualityComparer<T>.Default.Equals(neighbor, destination))
                {
                    var path = new List<T>();
                    var node = destination;
                    while (parentMap.TryGetValue(node, out var parent))
                    {
                        path.Add(node);
                        node = parent;
                    }
                    path.Add(source);
                    path.Reverse();
                    return path;
                }

                queue.Enqueue(neighbor);
            }
        }

        return new List<T>();
    }

    /// <summary>
    /// Determines whether the graph is connected (all vertices reachable from any vertex).
    /// </summary>
    public bool IsConnected()
    {
        if (_adjacencyList.Count == 0)
            return true;

        var startVertex = GetFirstVertex();
        var visitedCount = 0;

        foreach (var _ in DepthFirstSearch(startVertex))
            visitedCount++;

        return visitedCount == _adjacencyList.Count;
    }

    /// <summary>
    /// Gets all connected components of the graph.
    /// </summary>
    public IEnumerable<HashSet<T>> GetConnectedComponents()
    {
        var visited = new HashSet<T>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            if (visited.Contains(vertex))
                continue;

            var component = new HashSet<T>();
            foreach (var v in DepthFirstSearch(vertex))
            {
                component.Add(v);
                visited.Add(v);
            }

            yield return component;
        }
    }

    /// <summary>
    /// Clears all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _adjacencyList.Clear();
        _edgeCount = 0;
    }

    private T GetFirstVertex()
    {
        foreach (var key in _adjacencyList.Keys)
            return key;
        throw new InvalidOperationException("Graph is empty");
    }
}
