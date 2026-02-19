using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a directed graph (digraph) where edges have a specific direction.
/// An edge from A to B is different from an edge from B to A.
/// </summary>
/// <typeparam name="T">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// In a directed graph, each edge has a direction indicated by an arrow.
/// If there's an edge from A to B, it doesn't mean there's an edge from B to A.
/// </para>
/// <para>
/// Key concepts:
/// <list type="bullet">
/// <item><description>Out-degree: Number of edges leaving a vertex</description></item>
/// <item><description>In-degree: Number of edges entering a vertex</description></item>
/// <item><description>Successors: Vertices reachable via outgoing edges</description></item>
/// <item><description>Predecessors: Vertices that can reach this vertex</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications: Web page linking, social media following, task dependencies,
/// control flow graphs, state machines.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var twitter = new DirectedGraph&lt;string&gt;();
/// twitter.AddEdge("Alice", "Bob");   // Alice follows Bob
/// twitter.AddEdge("Alice", "Charlie"); // Alice follows Charlie
/// twitter.AddEdge("Bob", "Alice");   // Bob follows Alice (separate relationship!)
/// 
/// var aliceFollowing = twitter.GetOutNeighbors("Alice"); // [Bob, Charlie]
/// var aliceFollowers = twitter.GetInNeighbors("Alice");  // [Bob]
/// 
/// // Bob doesn't follow Charlie
/// var bobFollowing = twitter.GetOutNeighbors("Bob"); // [Alice]
/// </code>
/// </example>
public class DirectedGraph<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _outEdges = new();
    private readonly Dictionary<T, HashSet<T>> _inEdges = new();
    private int _edgeCount;

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    public int VertexCount => _outEdges.Count;

    /// <summary>
    /// Gets the number of directed edges in the graph.
    /// </summary>
    public int EdgeCount => _edgeCount;

    /// <summary>
    /// Adds a vertex to the graph if it doesn't already exist.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added; false if it already existed.</returns>
    public bool AddVertex(T vertex)
    {
        if (_outEdges.ContainsKey(vertex))
            return false;

        _outEdges[vertex] = new HashSet<T>();
        _inEdges[vertex] = new HashSet<T>();
        return true;
    }

    /// <summary>
    /// Adds a directed edge from source to destination.
    /// Both vertices are added if they don't exist.
    /// </summary>
    /// <param name="source">The source vertex (tail of the arrow).</param>
    /// <param name="destination">The destination vertex (head of the arrow).</param>
    public void AddEdge(T source, T destination)
    {
        AddVertex(source);
        AddVertex(destination);

        if (_outEdges[source].Add(destination))
        {
            _inEdges[destination].Add(source);
            _edgeCount++;
        }
    }

    /// <summary>
    /// Removes a vertex and all its incident edges (both incoming and outgoing).
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed; false if it didn't exist.</returns>
    public bool RemoveVertex(T vertex)
    {
        if (!_outEdges.TryGetValue(vertex, out var outNeighbors))
            return false;

        _edgeCount -= outNeighbors.Count;
        _edgeCount -= _inEdges[vertex].Count;

        foreach (var neighbor in outNeighbors)
            _inEdges[neighbor].Remove(vertex);

        foreach (var neighbor in _inEdges[vertex])
            _outEdges[neighbor].Remove(vertex);

        _outEdges.Remove(vertex);
        _inEdges.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes the directed edge from source to destination.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <returns>True if the edge was removed; false if it didn't exist.</returns>
    public bool RemoveEdge(T source, T destination)
    {
        if (!_outEdges.TryGetValue(source, out var outNeighbors))
            return false;

        if (!outNeighbors.Remove(destination))
            return false;

        _inEdges[destination].Remove(source);
        _edgeCount--;
        return true;
    }

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    public bool ContainsVertex(T vertex) => _outEdges.ContainsKey(vertex);

    /// <summary>
    /// Determines whether a directed edge exists from source to destination.
    /// </summary>
    public bool ContainsEdge(T source, T destination) =>
        _outEdges.TryGetValue(source, out var neighbors) && neighbors.Contains(destination);

    /// <summary>
    /// Gets the out-degree of a vertex (number of outgoing edges).
    /// </summary>
    public int GetOutDegree(T vertex) =>
        _outEdges.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets the in-degree of a vertex (number of incoming edges).
    /// </summary>
    public int GetInDegree(T vertex) =>
        _inEdges.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets all vertices that can be reached directly from the specified vertex (successors).
    /// </summary>
    public IReadOnlyCollection<T> GetOutNeighbors(T vertex) =>
        _outEdges.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices that can directly reach the specified vertex (predecessors).
    /// </summary>
    public IReadOnlyCollection<T> GetInNeighbors(T vertex) =>
        _inEdges.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IReadOnlyCollection<T> GetVertices() => _outEdges.Keys;

    /// <summary>
    /// Performs a breadth-first traversal starting from the specified vertex,
    /// following outgoing edges.
    /// </summary>
    public IEnumerable<T> BreadthFirstSearch(T startVertex)
    {
        if (!_outEdges.ContainsKey(startVertex))
            yield break;

        var visited = new HashSet<T> { startVertex };
        var queue = new Queue<T>();
        queue.Enqueue(startVertex);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var neighbor in _outEdges[current])
            {
                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }
    }

    /// <summary>
    /// Performs a depth-first traversal starting from the specified vertex,
    /// following outgoing edges.
    /// </summary>
    public IEnumerable<T> DepthFirstSearch(T startVertex)
    {
        if (!_outEdges.ContainsKey(startVertex))
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

            foreach (var neighbor in _outEdges[current])
            {
                if (!visited.Contains(neighbor))
                    stack.Push(neighbor);
            }
        }
    }

    /// <summary>
    /// Performs a topological sort of the graph.
    /// </summary>
    /// <returns>
    /// A list of vertices in topological order, or empty if the graph contains a cycle.
    /// </returns>
    /// <remarks>
    /// Uses Kahn's algorithm based on in-degrees.
    /// </remarks>
    public List<T> TopologicalSort()
    {
        var inDegree = new Dictionary<T, int>();
        foreach (var vertex in _outEdges.Keys)
            inDegree[vertex] = _inEdges[vertex].Count;

        var queue = new Queue<T>();
        foreach (var (vertex, degree) in inDegree)
        {
            if (degree == 0)
                queue.Enqueue(vertex);
        }

        var result = new List<T>();

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            result.Add(current);

            foreach (var neighbor in _outEdges[current])
            {
                if (--inDegree[neighbor] == 0)
                    queue.Enqueue(neighbor);
            }
        }

        return result.Count == _outEdges.Count ? result : new List<T>();
    }

    /// <summary>
    /// Detects whether the graph contains any cycles.
    /// </summary>
    public bool HasCycle()
    {
        var visited = new HashSet<T>();
        var recursionStack = new HashSet<T>();

        foreach (var vertex in _outEdges.Keys)
        {
            if (HasCycleDfs(vertex, visited, recursionStack))
                return true;
        }

        return false;
    }

    private bool HasCycleDfs(T vertex, HashSet<T> visited, HashSet<T> recursionStack)
    {
        if (recursionStack.Contains(vertex))
            return true;

        if (visited.Contains(vertex))
            return false;

        visited.Add(vertex);
        recursionStack.Add(vertex);

        foreach (var neighbor in _outEdges[vertex])
        {
            if (HasCycleDfs(neighbor, visited, recursionStack))
                return true;
        }

        recursionStack.Remove(vertex);
        return false;
    }

    /// <summary>
    /// Finds all vertices reachable from the specified source vertex.
    /// </summary>
    public HashSet<T> GetReachableVertices(T source)
    {
        var reachable = new HashSet<T>();

        if (!_outEdges.ContainsKey(source))
            return reachable;

        foreach (var vertex in BreadthFirstSearch(source))
            reachable.Add(vertex);

        return reachable;
    }

    /// <summary>
    /// Finds all strongly connected components using Kosaraju's algorithm.
    /// </summary>
    public IEnumerable<HashSet<T>> GetStronglyConnectedComponents()
    {
        var visited = new HashSet<T>();
        var finishOrder = new List<T>();

        void Dfs1(T v)
        {
            if (visited.Contains(v)) return;
            visited.Add(v);
            foreach (var neighbor in _outEdges[v])
                Dfs1(neighbor);
            finishOrder.Add(v);
        }

        foreach (var vertex in _outEdges.Keys)
            Dfs1(vertex);

        finishOrder.Reverse();
        visited.Clear();

        void Dfs2(T v, HashSet<T> component)
        {
            if (visited.Contains(v)) return;
            visited.Add(v);
            component.Add(v);
            foreach (var neighbor in _inEdges[v])
                Dfs2(neighbor, component);
        }

        foreach (var vertex in finishOrder)
        {
            if (visited.Contains(vertex)) continue;
            var component = new HashSet<T>();
            Dfs2(vertex, component);
            yield return component;
        }
    }

    /// <summary>
    /// Clears all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _outEdges.Clear();
        _inEdges.Clear();
        _edgeCount = 0;
    }
}
