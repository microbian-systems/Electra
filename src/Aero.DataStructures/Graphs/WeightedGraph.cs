using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a weighted graph where each edge has an associated numeric weight.
/// Supports both directed and undirected configurations.
/// </summary>
/// <typeparam name="TVertex">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <typeparam name="TWeight">The type of edge weights. Must implement IComparable.</typeparam>
/// <remarks>
/// <para>
/// Weighted graphs are fundamental in many applications where the "cost" or "distance"
/// between vertices matters. The weight can represent distance, cost, time, capacity, etc.
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Road networks (weight = distance or travel time)</description></item>
/// <item><description>Network routing (weight = latency or bandwidth)</description></item>
/// <item><description>Flight connections (weight = ticket price)</description></item>
/// <item><description>Social networks (weight = interaction frequency)</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var roadMap = new WeightedGraph&lt;string, int&gt;(directed: false);
/// roadMap.AddEdge("New York", "Boston", 215);
/// roadMap.AddEdge("New York", "Philadelphia", 97);
/// roadMap.AddEdge("Philadelphia", "Washington DC", 140);
/// 
/// var distances = roadMap.Dijkstra("New York");
/// // distances["Boston"] = 215
/// // distances["Philadelphia"] = 97
/// // distances["Washington DC"] = 237 (via Philadelphia)
/// </code>
/// </example>
public class WeightedGraph<TVertex, TWeight> 
    where TVertex : notnull
    where TWeight : IComparable<TWeight>
{
    private readonly Dictionary<TVertex, Dictionary<TVertex, TWeight>> _adjacencyList = new();
    private readonly bool _isDirected;
    private int _edgeCount;

    /// <summary>
    /// Gets a zero weight value for the weight type.
    /// </summary>
    public static TWeight ZeroWeight => (TWeight)(dynamic)0;

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
    /// Initializes a new instance of the <see cref="WeightedGraph{TVertex, TWeight}"/> class.
    /// </summary>
    /// <param name="directed">Whether the graph is directed (true) or undirected (false).</param>
    public WeightedGraph(bool directed = false)
    {
        _isDirected = directed;
    }

    /// <summary>
    /// Adds a vertex to the graph if it doesn't already exist.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added; false if it already existed.</returns>
    public bool AddVertex(TVertex vertex)
    {
        if (_adjacencyList.ContainsKey(vertex))
            return false;

        _adjacencyList[vertex] = new Dictionary<TVertex, TWeight>();
        return true;
    }

    /// <summary>
    /// Adds a weighted edge between two vertices.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <param name="weight">The weight of the edge.</param>
    public void AddEdge(TVertex source, TVertex destination, TWeight weight)
    {
        AddVertex(source);
        AddVertex(destination);

        var isNewEdge = !_adjacencyList[source].ContainsKey(destination);
        _adjacencyList[source][destination] = weight;

        if (!_isDirected)
            _adjacencyList[destination][source] = weight;

        if (isNewEdge)
            _edgeCount++;
    }

    /// <summary>
    /// Removes a vertex and all its incident edges from the graph.
    /// </summary>
    /// <param name="vertex">The vertex to remove.</param>
    /// <returns>True if the vertex was removed; false if it didn't exist.</returns>
    public bool RemoveVertex(TVertex vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var edges))
            return false;

        var removedEdges = edges.Count;
        _adjacencyList.Remove(vertex);

        foreach (var edgesDict in _adjacencyList.Values)
        {
            if (edgesDict.Remove(vertex))
                removedEdges++;
        }

        _edgeCount -= _isDirected ? removedEdges : removedEdges / 2;
        return true;
    }

    /// <summary>
    /// Removes the edge between two vertices.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <returns>True if the edge was removed; false if it didn't exist.</returns>
    public bool RemoveEdge(TVertex source, TVertex destination)
    {
        if (!_adjacencyList.TryGetValue(source, out var edges))
            return false;

        if (!edges.Remove(destination))
            return false;

        if (!_isDirected)
            _adjacencyList[destination].Remove(source);

        _edgeCount--;
        return true;
    }

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    public bool ContainsVertex(TVertex vertex) => _adjacencyList.ContainsKey(vertex);

    /// <summary>
    /// Determines whether an edge exists between two vertices.
    /// </summary>
    public bool ContainsEdge(TVertex source, TVertex destination) =>
        _adjacencyList.TryGetValue(source, out var edges) && edges.ContainsKey(destination);

    /// <summary>
    /// Gets the weight of the edge between two vertices.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <param name="weight">When this method returns, contains the weight if the edge exists; otherwise, the default value.</param>
    /// <returns>True if the edge exists; false otherwise.</returns>
    public bool TryGetWeight(TVertex source, TVertex destination, out TWeight weight)
    {
        weight = default!;
        if (_adjacencyList.TryGetValue(source, out var edges) && edges.TryGetValue(destination, out weight))
            return true;
        return false;
    }

    /// <summary>
    /// Gets all edges in the graph with their weights.
    /// </summary>
    public IEnumerable<(TVertex Source, TVertex Destination, TWeight Weight)> GetEdges()
    {
        var seen = new HashSet<(TVertex, TVertex)>();

        foreach (var (source, edges) in _adjacencyList)
        {
            foreach (var (destination, weight) in edges)
            {
                var edge = _isDirected 
                    ? (source, destination) 
                    : (Min(source, destination), Max(source, destination));

                if (seen.Add(edge))
                    yield return (source, destination, weight);
            }
        }
    }

    /// <summary>
    /// Gets all neighbors of a vertex with their edge weights.
    /// </summary>
    public IReadOnlyDictionary<TVertex, TWeight> GetNeighborsWithWeights(TVertex vertex) =>
        _adjacencyList.TryGetValue(vertex, out var edges) ? edges : new Dictionary<TVertex, TWeight>();

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IReadOnlyCollection<TVertex> GetVertices() => _adjacencyList.Keys;

    /// <summary>
    /// Computes the shortest paths from a source vertex to all other vertices using Dijkstra's algorithm.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <returns>A dictionary mapping each vertex to its shortest distance from the source.</returns>
    /// <exception cref="ArgumentException">Thrown when source vertex is not in the graph.</exception>
    public Dictionary<TVertex, TWeight> Dijkstra(TVertex source)
    {
        if (!_adjacencyList.ContainsKey(source))
            throw new ArgumentException($"Vertex '{source}' not found in graph.", nameof(source));

        var distances = new Dictionary<TVertex, TWeight>();
        var visited = new HashSet<TVertex>();
        var priorityQueue = new PriorityQueue<TVertex, TWeight>();

        foreach (var vertex in _adjacencyList.Keys)
            distances[vertex] = MaxValue();

        distances[source] = ZeroWeight;
        priorityQueue.Enqueue(source, ZeroWeight);

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            if (!visited.Add(current))
                continue;

            foreach (var (neighbor, edgeWeight) in _adjacencyList[current])
            {
                var newDistance = Add(distances[current], edgeWeight);

                if (newDistance.CompareTo(distances[neighbor]) < 0)
                {
                    distances[neighbor] = newDistance;
                    priorityQueue.Enqueue(neighbor, newDistance);
                }
            }
        }

        return distances;
    }

    /// <summary>
    /// Finds the shortest path between two vertices using Dijkstra's algorithm with path reconstruction.
    /// </summary>
    /// <returns>A tuple containing the path and total weight, or default if no path exists.</returns>
    public (List<TVertex> Path, TWeight TotalWeight) GetShortestPath(TVertex source, TVertex destination)
    {
        if (!_adjacencyList.ContainsKey(source) || !_adjacencyList.ContainsKey(destination))
            return (new List<TVertex>(), default!);

        var distances = new Dictionary<TVertex, TWeight>();
        var previous = new Dictionary<TVertex, TVertex>();
        var visited = new HashSet<TVertex>();
        var priorityQueue = new PriorityQueue<TVertex, TWeight>();

        foreach (var vertex in _adjacencyList.Keys)
            distances[vertex] = MaxValue();

        distances[source] = ZeroWeight;
        priorityQueue.Enqueue(source, ZeroWeight);

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();

            if (!visited.Add(current))
                continue;

            if (EqualityComparer<TVertex>.Default.Equals(current, destination))
                break;

            foreach (var (neighbor, edgeWeight) in _adjacencyList[current])
            {
                var newDistance = Add(distances[current], edgeWeight);

                if (newDistance.CompareTo(distances[neighbor]) < 0)
                {
                    distances[neighbor] = newDistance;
                    previous[neighbor] = current;
                    priorityQueue.Enqueue(neighbor, newDistance);
                }
            }
        }

        if (!previous.ContainsKey(destination) && !EqualityComparer<TVertex>.Default.Equals(source, destination))
            return (new List<TVertex>(), default!);

        var path = new List<TVertex>();
        var node = destination;
        while (!EqualityComparer<TVertex>.Default.Equals(node, source))
        {
            path.Add(node);
            node = previous[node];
        }
        path.Add(source);
        path.Reverse();

        return (path, distances[destination]);
    }

    /// <summary>
    /// Finds the Minimum Spanning Tree using Prim's algorithm.
    /// Only applicable to undirected graphs.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when called on a directed graph.</exception>
    public WeightedGraph<TVertex, TWeight> GetMinimumSpanningTree()
    {
        if (_isDirected)
            throw new InvalidOperationException("MST is only defined for undirected graphs.");

        if (_adjacencyList.Count == 0)
            return new WeightedGraph<TVertex, TWeight>(false);

        var mst = new WeightedGraph<TVertex, TWeight>(false);
        var inMst = new HashSet<TVertex>();
        var firstVertex = GetFirstVertex();
        
        mst.AddVertex(firstVertex);
        inMst.Add(firstVertex);

        var edgeQueue = new PriorityQueue<(TVertex From, TVertex To, TWeight Weight), TWeight>();

        foreach (var (neighbor, weight) in _adjacencyList[firstVertex])
            edgeQueue.Enqueue((firstVertex, neighbor, weight), weight);

        while (edgeQueue.Count > 0 && inMst.Count < _adjacencyList.Count)
        {
            var (from, to, weight) = edgeQueue.Dequeue();

            if (inMst.Contains(to))
                continue;

            mst.AddEdge(from, to, weight);
            inMst.Add(to);

            foreach (var (neighbor, neighborWeight) in _adjacencyList[to])
            {
                if (!inMst.Contains(neighbor))
                    edgeQueue.Enqueue((to, neighbor, neighborWeight), neighborWeight);
            }
        }

        return mst;
    }

    /// <summary>
    /// Clears all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _adjacencyList.Clear();
        _edgeCount = 0;
    }

    private static TWeight MaxValue()
    {
        var type = typeof(TWeight);
        if (type == typeof(int)) return (TWeight)(object)int.MaxValue;
        if (type == typeof(long)) return (TWeight)(object)long.MaxValue;
        if (type == typeof(double)) return (TWeight)(object)double.PositiveInfinity;
        if (type == typeof(float)) return (TWeight)(object)float.PositiveInfinity;
        if (type == typeof(decimal)) return (TWeight)(object)decimal.MaxValue;
        return default!;
    }

    private static TWeight Add(TWeight a, TWeight b)
    {
        dynamic da = a, db = b;
        return da + db;
    }

    private static TVertex Min(TVertex a, TVertex b) =>
        Comparer<TVertex>.Default.Compare(a, b) <= 0 ? a : b;

    private static TVertex Max(TVertex a, TVertex b) =>
        Comparer<TVertex>.Default.Compare(a, b) >= 0 ? a : b;

    private TVertex GetFirstVertex()
    {
        foreach (var key in _adjacencyList.Keys)
            return key;
        throw new InvalidOperationException("Graph is empty");
    }
}
