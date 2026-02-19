using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a simple graph - an unweighted graph that can be either directed or undirected,
/// with no self-loops and no multiple edges between the same pair of vertices.
/// </summary>
/// <typeparam name="T">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// A simple graph is the most basic graph type. It has no edge weights and no parallel edges.
/// This is the standard mathematical definition of a graph used in most graph theory.
/// </para>
/// <para>
/// Properties of a simple graph:
/// <list type="bullet">
/// <item><description>No self-loops (edges from a vertex to itself)</description></item>
/// <item><description>No multiple edges (at most one edge between any two vertices)</description></item>
/// <item><description>Unweighted (all edges have equal "cost")</description></item>
/// <item><description>Can be directed or undirected</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Social networks (friendships, followers)</description></item>
/// <item><description>Computer networks (connections between nodes)</description></item>
/// <item><description>Dependency analysis</description></item>
/// <item><description>Path finding (unweighted shortest path)</description></item>
/// <item><description>Graph coloring problems</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Create an undirected simple graph (friendship network)
/// var friends = new SimpleGraph&lt;string&gt;(directed: false);
/// friends.AddEdge("Alice", "Bob");
/// friends.AddEdge("Alice", "Charlie");
/// friends.AddEdge("Bob", "Charlie");
/// 
/// // This would throw an exception (self-loop not allowed)
/// // friends.AddEdge("Alice", "Alice");
/// 
/// // This would have no effect (edge already exists)
/// friends.AddEdge("Alice", "Bob"); 
/// 
/// // Find degrees of separation
/// var path = friends.GetShortestPath("Alice", "Bob");
/// 
/// // Create a directed simple graph (Twitter-like following)
/// var twitter = new SimpleGraph&lt;string&gt;(directed: true);
/// twitter.AddEdge("Alice", "Bob");   // Alice follows Bob
/// twitter.AddEdge("Bob", "Alice");   // Bob follows Alice (different edge!)
/// </code>
/// </example>
public class SimpleGraph<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _adjacencyList = new();
    private readonly bool _isDirected;
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
    /// Gets whether the graph is directed.
    /// </summary>
    public bool IsDirected => _isDirected;

    /// <summary>
    /// Initializes a new instance of the <see cref="SimpleGraph{T}"/> class.
    /// </summary>
    /// <param name="directed">Whether the graph is directed (true) or undirected (false).</param>
    public SimpleGraph(bool directed = false)
    {
        _isDirected = directed;
    }

    /// <summary>
    /// Adds a vertex to the graph.
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
    /// Adds an edge between two vertices.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="target">The target vertex.</param>
    /// <exception cref="ArgumentException">Thrown when attempting to add a self-loop.</exception>
    /// <returns>True if the edge was added; false if it already existed.</returns>
    public bool AddEdge(T source, T target)
    {
        if (EqualityComparer<T>.Default.Equals(source, target))
            throw new ArgumentException("Self-loops are not allowed in a simple graph.");

        AddVertex(source);
        AddVertex(target);

        if (_adjacencyList[source].Contains(target))
            return false;

        _adjacencyList[source].Add(target);
        _edgeCount++;

        if (!_isDirected)
            _adjacencyList[target].Add(source);

        return true;
    }

    /// <summary>
    /// Removes a vertex and all its incident edges.
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed; false if it didn't exist.</returns>
    public bool RemoveVertex(T vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var neighbors))
            return false;

        var removedEdges = neighbors.Count;
        _adjacencyList.Remove(vertex);

        foreach (var edges in _adjacencyList.Values)
        {
            if (edges.Remove(vertex))
                removedEdges++;
        }

        _edgeCount -= _isDirected ? removedEdges : removedEdges / 2;
        return true;
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="target">The target vertex.</param>
    /// <returns>True if the edge was removed; false if it didn't exist.</returns>
    public bool RemoveEdge(T source, T target)
    {
        if (!_adjacencyList.TryGetValue(source, out var neighbors))
            return false;

        if (!neighbors.Remove(target))
            return false;

        if (!_isDirected)
            _adjacencyList[target].Remove(source);

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
    public bool ContainsEdge(T source, T target) =>
        _adjacencyList.TryGetValue(source, out var neighbors) && neighbors.Contains(target);

    /// <summary>
    /// Gets the degree of a vertex.
    /// For directed graphs, this returns the total degree (in-degree + out-degree).
    /// </summary>
    public int GetDegree(T vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var neighbors))
            return 0;

        if (_isDirected)
        {
            var outDegree = neighbors.Count;
            var inDegree = _adjacencyList.Values.Sum(n => n.Contains(vertex) ? 1 : 0);
            return outDegree + inDegree;
        }

        return neighbors.Count;
    }

    /// <summary>
    /// Gets the in-degree of a vertex (for directed graphs).
    /// </summary>
    public int GetInDegree(T vertex)
    {
        if (!_isDirected)
            throw new InvalidOperationException("In-degree is only defined for directed graphs.");

        return _adjacencyList.Values.Sum(n => n.Contains(vertex) ? 1 : 0);
    }

    /// <summary>
    /// Gets the out-degree of a vertex (for directed graphs).
    /// </summary>
    public int GetOutDegree(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets all neighbors of a vertex.
    /// For directed graphs, returns outgoing neighbors.
    /// </summary>
    public IReadOnlyCollection<T> GetNeighbors(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IReadOnlyCollection<T> GetVertices() => _adjacencyList.Keys;

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IEnumerable<(T Source, T Target)> GetEdges()
    {
        var seen = _isDirected ? null : new HashSet<(T, T)>();

        foreach (var (source, neighbors) in _adjacencyList)
        {
            foreach (var target in neighbors)
            {
                if (_isDirected)
                {
                    yield return (source, target);
                }
                else
                {
                    var edge = source.GetHashCode() <= target.GetHashCode() 
                        ? (source, target) 
                        : (target, source);

                    if (seen!.Add(edge))
                        yield return edge;
                }
            }
        }
    }

    /// <summary>
    /// Performs a breadth-first traversal starting from the specified vertex.
    /// </summary>
    public IEnumerable<T> BreadthFirstSearch(T start)
    {
        if (!_adjacencyList.ContainsKey(start))
            yield break;

        var visited = new HashSet<T> { start };
        var queue = new Queue<T>();
        queue.Enqueue(start);

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
    public IEnumerable<T> DepthFirstSearch(T start)
    {
        if (!_adjacencyList.ContainsKey(start))
            yield break;

        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        stack.Push(start);

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
    /// Returns empty list if no path exists.
    /// </summary>
    public List<T> GetShortestPath(T source, T target)
    {
        if (!_adjacencyList.ContainsKey(source) || !_adjacencyList.ContainsKey(target))
            return new List<T>();

        if (EqualityComparer<T>.Default.Equals(source, target))
            return new List<T> { source };

        var visited = new HashSet<T> { source };
        var parent = new Dictionary<T, T>();
        var queue = new Queue<T>();
        queue.Enqueue(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in _adjacencyList[current])
            {
                if (!visited.Add(neighbor))
                    continue;

                parent[neighbor] = current;

                if (EqualityComparer<T>.Default.Equals(neighbor, target))
                {
                    var path = new List<T>();
                    var node = target;
                    while (parent.TryGetValue(node, out var p))
                    {
                        path.Add(node);
                        node = p;
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
    /// Checks if the graph is connected (for undirected graphs).
    /// </summary>
    public bool IsConnected()
    {
        if (_isDirected)
            throw new InvalidOperationException("IsConnected is only defined for undirected graphs.");

        if (_adjacencyList.Count == 0)
            return true;

        var start = GetFirstVertex();
        var count = 0;
        foreach (var _ in BreadthFirstSearch(start))
            count++;

        return count == _adjacencyList.Count;
    }

    /// <summary>
    /// Checks if the directed graph is strongly connected.
    /// </summary>
    public bool IsStronglyConnected()
    {
        if (!_isDirected)
            throw new InvalidOperationException("IsStronglyConnected is only defined for directed graphs.");

        if (_adjacencyList.Count == 0)
            return true;

        var start = GetFirstVertex();

        var count = 0;
        foreach (var _ in BreadthFirstSearch(start))
            count++;

        if (count != _adjacencyList.Count)
            return false;

        var reversed = GetReversedGraph();
        count = 0;
        foreach (var _ in reversed.BreadthFirstSearch(start))
            count++;

        return count == _adjacencyList.Count;
    }

    /// <summary>
    /// Checks if the graph contains a cycle.
    /// </summary>
    public bool HasCycle()
    {
        if (_isDirected)
            return HasCycleDirected();

        return HasCycleUndirected();
    }

    private bool HasCycleUndirected()
    {
        var visited = new HashSet<T>();
        foreach (var vertex in _adjacencyList.Keys)
        {
            if (!visited.Contains(vertex) && HasCycleUndirectedDfs(vertex, default, visited))
                return true;
        }
        return false;
    }

    private bool HasCycleUndirectedDfs(T vertex, T parent, HashSet<T> visited)
    {
        visited.Add(vertex);

        foreach (var neighbor in _adjacencyList[vertex])
        {
            if (!visited.Contains(neighbor))
            {
                if (HasCycleUndirectedDfs(neighbor, vertex, visited))
                    return true;
            }
            else if (!EqualityComparer<T>.Default.Equals(neighbor, parent))
            {
                return true;
            }
        }
        return false;
    }

    private bool HasCycleDirected()
    {
        var visited = new HashSet<T>();
        var recursionStack = new HashSet<T>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            if (HasCycleDirectedDfs(vertex, visited, recursionStack))
                return true;
        }
        return false;
    }

    private bool HasCycleDirectedDfs(T vertex, HashSet<T> visited, HashSet<T> recursionStack)
    {
        if (recursionStack.Contains(vertex))
            return true;

        if (visited.Contains(vertex))
            return false;

        visited.Add(vertex);
        recursionStack.Add(vertex);

        foreach (var neighbor in _adjacencyList[vertex])
        {
            if (HasCycleDirectedDfs(neighbor, visited, recursionStack))
                return true;
        }

        recursionStack.Remove(vertex);
        return false;
    }

    /// <summary>
    /// Checks if the graph is bipartite (2-colorable).
    /// </summary>
    public bool IsBipartite()
    {
        if (_adjacencyList.Count == 0)
            return true;

        var colors = new Dictionary<T, bool>();
        foreach (var start in _adjacencyList.Keys)
        {
            if (colors.ContainsKey(start))
                continue;

            var queue = new Queue<T>();
            queue.Enqueue(start);
            colors[start] = false;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var currentColor = colors[current];

                foreach (var neighbor in _adjacencyList[current])
                {
                    if (colors.TryGetValue(neighbor, out var neighborColor))
                    {
                        if (neighborColor == currentColor)
                            return false;
                    }
                    else
                    {
                        colors[neighbor] = !currentColor;
                        queue.Enqueue(neighbor);
                    }
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Gets connected components (for undirected graphs).
    /// </summary>
    public IEnumerable<HashSet<T>> GetConnectedComponents()
    {
        if (_isDirected)
            throw new InvalidOperationException("GetConnectedComponents is for undirected graphs. Use GetStronglyConnectedComponents for directed graphs.");

        var visited = new HashSet<T>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            if (visited.Contains(vertex))
                continue;

            var component = new HashSet<T>();
            foreach (var v in BreadthFirstSearch(vertex))
            {
                component.Add(v);
                visited.Add(v);
            }

            yield return component;
        }
    }

    /// <summary>
    /// Gets strongly connected components (for directed graphs).
    /// </summary>
    public IEnumerable<HashSet<T>> GetStronglyConnectedComponents()
    {
        if (!_isDirected)
            throw new InvalidOperationException("GetStronglyConnectedComponents is for directed graphs.");

        var indices = new Dictionary<T, int>();
        var lowlinks = new Dictionary<T, int>();
        var onStack = new HashSet<T>();
        var stack = new Stack<T>();
        var index = 0;
        var components = new List<HashSet<T>>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            if (!indices.ContainsKey(vertex))
                StrongConnect(vertex);
        }

        void StrongConnect(T vertex)
        {
            indices[vertex] = index;
            lowlinks[vertex] = index;
            index++;
            stack.Push(vertex);
            onStack.Add(vertex);

            foreach (var neighbor in _adjacencyList[vertex])
            {
                if (!indices.ContainsKey(neighbor))
                {
                    StrongConnect(neighbor);
                    lowlinks[vertex] = Math.Min(lowlinks[vertex], lowlinks[neighbor]);
                }
                else if (onStack.Contains(neighbor))
                {
                    lowlinks[vertex] = Math.Min(lowlinks[vertex], indices[neighbor]);
                }
            }

            if (lowlinks[vertex] == indices[vertex])
            {
                var component = new HashSet<T>();
                T w;
                do
                {
                    w = stack.Pop();
                    onStack.Remove(w);
                    component.Add(w);
                } while (!EqualityComparer<T>.Default.Equals(w, vertex));

                components.Add(component);
            }
        }

        return components;
    }

    private SimpleGraph<T> GetReversedGraph()
    {
        var reversed = new SimpleGraph<T>(true);

        foreach (var vertex in _adjacencyList.Keys)
            reversed.AddVertex(vertex);

        foreach (var (source, neighbors) in _adjacencyList)
        {
            foreach (var target in neighbors)
                reversed.AddEdge(target, source);
        }

        return reversed;
    }

    private T GetFirstVertex()
    {
        foreach (var key in _adjacencyList.Keys)
            return key;
        throw new InvalidOperationException("Graph is empty");
    }

    /// <summary>
    /// Clears all vertices and edges.
    /// </summary>
    public void Clear()
    {
        _adjacencyList.Clear();
        _edgeCount = 0;
    }
}
