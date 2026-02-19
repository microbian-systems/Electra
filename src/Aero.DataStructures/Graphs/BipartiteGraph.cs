using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a bipartite graph where vertices can be divided into two disjoint sets
/// such that every edge connects a vertex from one set to a vertex from the other set.
/// </summary>
/// <typeparam name="T">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// A bipartite graph (or bigraph) is a graph whose vertices can be divided into two 
/// independent sets U and V such that every edge connects a vertex in U to one in V.
/// Equivalently, a graph is bipartite if and only if it has no odd-length cycles.
/// </para>
/// <para>
/// Key properties:
/// <list type="bullet">
/// <item><description>No edges within the same partition</description></item>
/// <item><description>No odd-length cycles</description></item>
/// <item><description>2-colorable (chromatic number is 2)</description></item>
/// <item><description>Maximum matching equals minimum vertex cover (König's theorem)</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Job assignment problems</description></item>
/// <item><description>Stable marriage problem</description></item>
/// <item><description>Recommendation systems (users - items)</description></item>
/// <item><description>Scheduling problems</description></item>
/// <item><description>Social network matching</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var jobAssignment = new BipartiteGraph&lt;string&gt;();
/// 
/// // Partition 1: Workers
/// jobAssignment.AddVertexToSetU("Alice");
/// jobAssignment.AddVertexToSetU("Bob");
/// jobAssignment.AddVertexToSetU("Charlie");
/// 
/// // Partition 2: Jobs
/// jobAssignment.AddVertexToSetV("Frontend");
/// jobAssignment.AddVertexToSetV("Backend");
/// jobAssignment.AddVertexToSetV("DevOps");
/// 
/// // Edges represent which workers can do which jobs
/// jobAssignment.AddEdge("Alice", "Frontend");
/// jobAssignment.AddEdge("Alice", "Backend");
/// jobAssignment.AddEdge("Bob", "Backend");
/// jobAssignment.AddEdge("Bob", "DevOps");
/// jobAssignment.AddEdge("Charlie", "Frontend");
/// 
/// var matching = jobAssignment.FindMaximumMatching();
/// // Returns maximum assignment of workers to jobs
/// </code>
/// </example>
public class BipartiteGraph<T> where T : notnull
{
    private readonly HashSet<T> _setU = new();
    private readonly HashSet<T> _setV = new();
    private readonly Dictionary<T, HashSet<T>> _adjacencyList = new();
    private int _edgeCount;

    /// <summary>
    /// Gets the number of vertices in partition U.
    /// </summary>
    public int SetUCount => _setU.Count;

    /// <summary>
    /// Gets the number of vertices in partition V.
    /// </summary>
    public int SetVCount => _setV.Count;

    /// <summary>
    /// Gets the total number of vertices in the graph.
    /// </summary>
    public int VertexCount => _setU.Count + _setV.Count;

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    public int EdgeCount => _edgeCount;

    /// <summary>
    /// Gets all vertices in partition U.
    /// </summary>
    public IReadOnlyCollection<T> SetU => _setU;

    /// <summary>
    /// Gets all vertices in partition V.
    /// </summary>
    public IReadOnlyCollection<T> SetV => _setV;

    /// <summary>
    /// Adds a vertex to partition U.
    /// </summary>
    /// <param name="vertex">The vertex to add to partition U.</param>
    /// <returns>True if the vertex was added; false if it already exists in either partition.</returns>
    public bool AddVertexToSetU(T vertex)
    {
        if (_setU.Contains(vertex) || _setV.Contains(vertex))
            return false;

        _setU.Add(vertex);
        _adjacencyList[vertex] = new HashSet<T>();
        return true;
    }

    /// <summary>
    /// Adds a vertex to partition V.
    /// </summary>
    /// <param name="vertex">The vertex to add to partition V.</param>
    /// <returns>True if the vertex was added; false if it already exists in either partition.</returns>
    public bool AddVertexToSetV(T vertex)
    {
        if (_setU.Contains(vertex) || _setV.Contains(vertex))
            return false;

        _setV.Add(vertex);
        _adjacencyList[vertex] = new HashSet<T>();
        return true;
    }

    /// <summary>
    /// Adds an edge between a vertex in U and a vertex in V.
    /// </summary>
    /// <param name="vertexU">A vertex from partition U.</param>
    /// <param name="vertexV">A vertex from partition V.</param>
    /// <exception cref="ArgumentException">
    /// Thrown when both vertices are from the same partition or vertices don't exist.
    /// </exception>
    public void AddEdge(T vertexU, T vertexV)
    {
        var uInSetU = _setU.Contains(vertexU);
        var vInSetV = _setV.Contains(vertexV);
        var uInSetV = _setV.Contains(vertexU);
        var vInSetU = _setU.Contains(vertexV);

        if ((uInSetU && vInSetU) || (uInSetV && vInSetV))
            throw new ArgumentException("Cannot add edge within the same partition.");

        if (uInSetU && vInSetV)
        {
            if (_adjacencyList[vertexU].Add(vertexV))
            {
                _adjacencyList[vertexV].Add(vertexU);
                _edgeCount++;
            }
        }
        else if (uInSetV && vInSetU)
        {
            if (_adjacencyList[vertexV].Add(vertexU))
            {
                _adjacencyList[vertexU].Add(vertexV);
                _edgeCount++;
            }
        }
        else
        {
            throw new ArgumentException("Both vertices must exist in the graph.");
        }
    }

    /// <summary>
    /// Gets the partition that contains the specified vertex.
    /// </summary>
    /// <param name="vertex">The vertex to check.</param>
    /// <returns>'U' if in partition U, 'V' if in partition V, or null if not in graph.</returns>
    public char? GetPartition(T vertex)
    {
        if (_setU.Contains(vertex)) return 'U';
        if (_setV.Contains(vertex)) return 'V';
        return null;
    }

    /// <summary>
    /// Gets all neighbors of a vertex (which will be in the opposite partition).
    /// </summary>
    public IReadOnlyCollection<T> GetNeighbors(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Determines whether an edge exists between two vertices.
    /// </summary>
    public bool ContainsEdge(T vertexU, T vertexV) =>
        _adjacencyList.TryGetValue(vertexU, out var neighbors) && neighbors.Contains(vertexV);

    /// <summary>
    /// Determines whether the graph contains the specified vertex.
    /// </summary>
    public bool ContainsVertex(T vertex) => _setU.Contains(vertex) || _setV.Contains(vertex);

    /// <summary>
    /// Removes a vertex and all its incident edges from the graph.
    /// </summary>
    public bool RemoveVertex(T vertex)
    {
        if (!_adjacencyList.TryGetValue(vertex, out var neighbors))
            return false;

        _edgeCount -= neighbors.Count;

        foreach (var neighbor in neighbors)
            _adjacencyList[neighbor].Remove(vertex);

        _adjacencyList.Remove(vertex);
        _setU.Remove(vertex);
        _setV.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    public bool RemoveEdge(T vertexU, T vertexV)
    {
        if (!_adjacencyList.TryGetValue(vertexU, out var neighbors))
            return false;

        if (!neighbors.Remove(vertexV))
            return false;

        _adjacencyList[vertexV].Remove(vertexU);
        _edgeCount--;
        return true;
    }

    /// <summary>
    /// Finds a maximum matching using the Hopcroft-Karp algorithm.
    /// </summary>
    /// <returns>A dictionary representing the matching, where each key-value pair is a matched edge.</returns>
    /// <remarks>
    /// <para>
    /// A matching is a set of edges without common vertices.
    /// A maximum matching has the largest possible number of edges.
    /// </para>
    /// <para>
    /// Time complexity: O(E * sqrt(V))
    /// </para>
    /// </remarks>
    public Dictionary<T, T> FindMaximumMatching()
    {
        var pairU = new Dictionary<T, T>();
        var pairV = new Dictionary<T, T>();
        var dist = new Dictionary<T, int>();

        foreach (var u in _setU)
            pairU[u] = default!;

        foreach (var v in _setV)
            pairV[v] = default!;

        int result = 0;

        while (BfsForMatching(pairU, pairV, dist))
        {
            foreach (var u in _setU)
            {
                if (EqualityComparer<T>.Default.Equals(pairU[u], default!) && DfsForMatching(u, pairU, pairV, dist))
                    result++;
            }
        }

        var matching = new Dictionary<T, T>();
        foreach (var (u, v) in pairU)
        {
            if (!EqualityComparer<T>.Default.Equals(v, default!))
                matching[u] = v;
        }

        return matching;
    }

    private bool BfsForMatching(Dictionary<T, T> pairU, Dictionary<T, T> pairV, Dictionary<T, int> dist)
    {
        var queue = new Queue<T>();
        var nilDist = int.MaxValue;

        foreach (var u in _setU)
        {
            if (EqualityComparer<T>.Default.Equals(pairU[u], default!) || pairU[u] == null)
            {
                dist[u] = 0;
                queue.Enqueue(u);
            }
            else
            {
                dist[u] = int.MaxValue;
            }
        }

        while (queue.Count > 0)
        {
            var u = queue.Dequeue();

            if (dist[u] < nilDist)
            {
                foreach (var v in _adjacencyList[u])
                {
                    var pairVOfV = pairV[v];
                    if (EqualityComparer<T>.Default.Equals(pairVOfV, default!) || pairVOfV == null)
                    {
                        if (nilDist == int.MaxValue)
                        {
                            nilDist = dist[u] + 1;
                        }
                    }
                    else if (dist[pairVOfV] == int.MaxValue)
                    {
                        dist[pairVOfV] = dist[u] + 1;
                        queue.Enqueue(pairVOfV);
                    }
                }
            }
        }

        return nilDist != int.MaxValue;
    }

    private bool DfsForMatching(T u, Dictionary<T, T> pairU, Dictionary<T, T> pairV, Dictionary<T, int> dist)
    {
        if (EqualityComparer<T>.Default.Equals(u, default!) || u == null)
            return true;

        foreach (var v in _adjacencyList[u])
        {
            var pairVOfV = pairV[v];
            if ((EqualityComparer<T>.Default.Equals(pairVOfV, default!) || pairVOfV == null) || 
                (dist.TryGetValue(pairVOfV, out var d) && d == dist[u] + 1 && DfsForMatching(pairVOfV, pairU, pairV, dist)))
            {
                pairU[u] = v;
                pairV[v] = u;
                return true;
            }
        }

        dist[u] = int.MaxValue;
        return false;
    }

    /// <summary>
    /// Finds a perfect matching if one exists.
    /// A perfect matching matches every vertex.
    /// </summary>
    /// <returns>A perfect matching, or null if one doesn't exist.</returns>
    public Dictionary<T, T>? FindPerfectMatching()
    {
        if (_setU.Count != _setV.Count)
            return null;

        var matching = FindMaximumMatching();
        if (matching.Count == _setU.Count)
            return matching;

        return null;
    }

    /// <summary>
    /// Checks if the graph has a perfect matching.
    /// </summary>
    public bool HasPerfectMatching()
    {
        if (_setU.Count != _setV.Count)
            return false;

        return FindMaximumMatching().Count == _setU.Count;
    }

    /// <summary>
    /// Finds the minimum vertex cover using König's theorem.
    /// </summary>
    /// <returns>A set of vertices that forms a minimum vertex cover.</returns>
    /// <remarks>
    /// In a bipartite graph, the size of minimum vertex cover equals the size of maximum matching.
    /// </remarks>
    public HashSet<T> FindMinimumVertexCover()
    {
        var matching = FindMaximumMatching();
        var pairU = new Dictionary<T, T>();
        var pairV = new Dictionary<T, T>();

        foreach (var u in _setU)
            pairU[u] = default!;

        foreach (var v in _setV)
            pairV[v] = default!;

        foreach (var (u, v) in matching)
        {
            pairU[u] = v;
            pairV[v] = u;
        }

        var visitedU = new HashSet<T>();
        var visitedV = new HashSet<T>();
        var queue = new Queue<T>();

        foreach (var u in _setU)
        {
            if (EqualityComparer<T>.Default.Equals(pairU[u], default!))
            {
                visitedU.Add(u);
                queue.Enqueue(u);
            }
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (_setU.Contains(current))
            {
                foreach (var v in _adjacencyList[current])
                {
                    if (!visitedV.Contains(v) && !EqualityComparer<T>.Default.Equals(pairU[current], v))
                    {
                        visitedV.Add(v);
                        queue.Enqueue(v);
                    }
                }
            }
            else
            {
                var v = current;
                if (!EqualityComparer<T>.Default.Equals(pairV[v], default!) && !visitedU.Contains(pairV[v]))
                {
                    visitedU.Add(pairV[v]);
                    queue.Enqueue(pairV[v]);
                }
            }
        }

        var cover = new HashSet<T>();
        foreach (var u in _setU)
        {
            if (!visitedU.Contains(u))
                cover.Add(u);
        }
        foreach (var v in _setV)
        {
            if (visitedV.Contains(v))
                cover.Add(v);
        }

        return cover;
    }

    /// <summary>
    /// Clears all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _setU.Clear();
        _setV.Clear();
        _adjacencyList.Clear();
        _edgeCount = 0;
    }
}
