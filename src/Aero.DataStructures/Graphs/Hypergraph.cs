using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a hypergraph where edges (hyperedges) can connect any number of vertices,
/// not just two. This generalizes the standard graph concept.
/// </summary>
/// <typeparam name="TVertex">The type of vertices in the hypergraph. Must be non-nullable.</typeparam>
/// <typeparam name="TVertex">The type of hyperedge identifiers. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// In a hypergraph, a hyperedge is a set of vertices that can have any cardinality.
/// A regular graph is a special case where all hyperedges have exactly 2 vertices.
/// </para>
/// <para>
/// Key concepts:
/// <list type="bullet">
/// <item><description>Hyperedge: A set of vertices (can be any size ≥ 1)</description></item>
/// <item><description>Rank: The maximum cardinality of any hyperedge</description></item>
/// <item><description>Incidence: A vertex is incident to a hyperedge if it belongs to the hyperedge</description></item>
/// <item><description>k-uniform: All hyperedges have exactly k vertices</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Social group modeling (groups can have any number of members)</description></item>
/// <item><description>Co-authorship networks (papers have multiple authors)</description></item>
/// <item><description>Chemical reactions (multiple reactants/products)</description></item>
/// <item><description>Circuit design (nets connecting multiple components)</description></item>
/// <item><description>Constraint satisfaction problems</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var research = new Hypergraph&lt;string, int&gt;();
/// 
/// // Add authors
/// research.AddVertex("Alice");
/// research.AddVertex("Bob");
/// research.AddVertex("Charlie");
/// research.AddVertex("Diana");
/// 
/// // Add hyperedges representing papers with multiple authors
/// research.AddHyperedge(1, new[] { "Alice", "Bob" }, "Paper 1: Alice and Bob");
/// research.AddHyperedge(2, new[] { "Alice", "Bob", "Charlie" }, "Paper 2: Three authors");
/// research.AddHyperedge(3, new[] { "Charlie", "Diana" }, "Paper 3: Charlie and Diana");
/// 
/// // Find all papers Alice co-authored
/// var alicePapers = research.GetIncidentHyperedges("Alice");
/// 
/// // Find co-authors of Alice (other vertices in her hyperedges)
/// var coauthors = research.GetNeighbors("Alice"); // Bob, Charlie
/// 
/// // Check if a set of authors collaborated on a paper
/// var hasJointPaper = research.HasHyperedge(new[] { "Alice", "Bob", "Charlie" }); // true
/// </code>
/// </example>
public class Hypergraph<TVertex, THyperedgeId>
    where TVertex : notnull
    where THyperedgeId : notnull
{
    private readonly HashSet<TVertex> _vertices = new();
    private readonly Dictionary<THyperedgeId, Hyperedge> _hyperedges = new();
    private readonly Dictionary<TVertex, HashSet<THyperedgeId>> _incidence = new();

    /// <summary>
    /// Represents a hyperedge in the hypergraph.
    /// </summary>
    public class Hyperedge
    {
        /// <summary>
        /// Gets or sets the unique identifier of the hyperedge.
        /// </summary>
        public THyperedgeId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the set of vertices in this hyperedge.
        /// </summary>
        public HashSet<TVertex> Vertices { get; set; } = new();

        /// <summary>
        /// Gets or sets the weight of the hyperedge.
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets optional data associated with the hyperedge.
        /// </summary>
        public object? Data { get; set; }

        /// <summary>
        /// Gets the cardinality (number of vertices) of this hyperedge.
        /// </summary>
        public int Cardinality => Vertices.Count;
    }

    /// <summary>
    /// Gets the number of vertices in the hypergraph.
    /// </summary>
    public int VertexCount => _vertices.Count;

    /// <summary>
    /// Gets the number of hyperedges in the hypergraph.
    /// </summary>
    public int HyperedgeCount => _hyperedges.Count;

    /// <summary>
    /// Gets the rank of the hypergraph (maximum hyperedge cardinality).
    /// </summary>
    public int Rank => _hyperedges.Values.Max(h => h.Cardinality);

    /// <summary>
    /// Gets all vertices in the hypergraph.
    /// </summary>
    public IReadOnlyCollection<TVertex> Vertices => _vertices;

    /// <summary>
    /// Adds a vertex to the hypergraph.
    /// </summary>
    /// <param name="vertex">The vertex to add.</param>
    /// <returns>True if the vertex was added; false if it already existed.</returns>
    public bool AddVertex(TVertex vertex)
    {
        if (_vertices.Contains(vertex))
            return false;

        _vertices.Add(vertex);
        _incidence[vertex] = new HashSet<THyperedgeId>();
        return true;
    }

    /// <summary>
    /// Adds multiple vertices to the hypergraph.
    /// </summary>
    public void AddVertices(IEnumerable<TVertex> vertices)
    {
        foreach (var vertex in vertices)
            AddVertex(vertex);
    }

    /// <summary>
    /// Adds a hyperedge connecting the specified vertices.
    /// </summary>
    /// <param name="id">The unique identifier of the hyperedge.</param>
    /// <param name="vertices">The vertices in the hyperedge.</param>
    /// <param name="data">Optional data associated with the hyperedge.</param>
    /// <param name="weight">Optional weight for the hyperedge.</param>
    /// <returns>The created hyperedge, or null if ID already exists.</returns>
    public Hyperedge? AddHyperedge(THyperedgeId id, IEnumerable<TVertex> vertices, 
        object? data = null, double weight = 1.0)
    {
        if (_hyperedges.ContainsKey(id))
            return null;

        var vertexSet = new HashSet<TVertex>(vertices);
        
        if (vertexSet.Count == 0)
            throw new ArgumentException("Hyperedge must contain at least one vertex.");

        foreach (var vertex in vertexSet)
            AddVertex(vertex);

        var hyperedge = new Hyperedge
        {
            Id = id,
            Vertices = vertexSet,
            Data = data,
            Weight = weight
        };

        _hyperedges[id] = hyperedge;

        foreach (var vertex in vertexSet)
            _incidence[vertex].Add(id);

        return hyperedge;
    }

    /// <summary>
    /// Removes a vertex and all incident hyperedges from the hypergraph.
    /// </summary>
    public bool RemoveVertex(TVertex vertex)
    {
        if (!_vertices.Contains(vertex))
            return false;

        var incidentHyperedges = _incidence[vertex].ToList();
        foreach (var hyperedgeId in incidentHyperedges)
            RemoveHyperedge(hyperedgeId);

        _vertices.Remove(vertex);
        _incidence.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes a hyperedge from the hypergraph.
    /// </summary>
    public bool RemoveHyperedge(THyperedgeId id)
    {
        if (!_hyperedges.TryGetValue(id, out var hyperedge))
            return false;

        foreach (var vertex in hyperedge.Vertices)
            _incidence[vertex].Remove(id);

        _hyperedges.Remove(id);
        return true;
    }

    /// <summary>
    /// Determines whether the hypergraph contains the specified vertex.
    /// </summary>
    public bool ContainsVertex(TVertex vertex) => _vertices.Contains(vertex);

    /// <summary>
    /// Determines whether the hypergraph contains a hyperedge with the specified ID.
    /// </summary>
    public bool ContainsHyperedge(THyperedgeId id) => _hyperedges.ContainsKey(id);

    /// <summary>
    /// Gets a hyperedge by its ID.
    /// </summary>
    public Hyperedge? GetHyperedge(THyperedgeId id) =>
        _hyperedges.TryGetValue(id, out var hyperedge) ? hyperedge : null;

    /// <summary>
    /// Gets all hyperedges incident to a vertex.
    /// </summary>
    public IReadOnlyCollection<Hyperedge> GetIncidentHyperedges(TVertex vertex)
    {
        if (!_incidence.TryGetValue(vertex, out var hyperedgeIds))
            return new List<Hyperedge>();

        return hyperedgeIds.Select(id => _hyperedges[id]).ToList();
    }

    /// <summary>
    /// Gets the degree of a vertex (number of incident hyperedges).
    /// </summary>
    public int GetDegree(TVertex vertex) =>
        _incidence.TryGetValue(vertex, out var hyperedges) ? hyperedges.Count : 0;

    /// <summary>
    /// Gets all vertices that share at least one hyperedge with the specified vertex.
    /// </summary>
    public HashSet<TVertex> GetNeighbors(TVertex vertex)
    {
        var neighbors = new HashSet<TVertex>();

        if (!_incidence.TryGetValue(vertex, out var hyperedgeIds))
            return neighbors;

        foreach (var hyperedgeId in hyperedgeIds)
        {
            foreach (var v in _hyperedges[hyperedgeId].Vertices)
            {
                if (!v.Equals(vertex))
                    neighbors.Add(v);
            }
        }

        return neighbors;
    }

    /// <summary>
    /// Checks if there exists a hyperedge containing exactly the specified vertices.
    /// </summary>
    public bool HasHyperedge(IEnumerable<TVertex> vertices)
    {
        var vertexSet = new HashSet<TVertex>(vertices);
        return _hyperedges.Values.Any(h => h.Vertices.SetEquals(vertexSet));
    }

    /// <summary>
    /// Checks if there exists a hyperedge containing all the specified vertices (and possibly more).
    /// </summary>
    public bool HasHyperedgeContaining(IEnumerable<TVertex> vertices)
    {
        var vertexSet = new HashSet<TVertex>(vertices);
        return _hyperedges.Values.Any(h => vertexSet.IsSubsetOf(h.Vertices));
    }

    /// <summary>
    /// Gets all hyperedges of a specific cardinality.
    /// </summary>
    public IEnumerable<Hyperedge> GetHyperedgesByCardinality(int cardinality) =>
        _hyperedges.Values.Where(h => h.Cardinality == cardinality);

    /// <summary>
    /// Gets all hyperedges in the hypergraph.
    /// </summary>
    public IEnumerable<Hyperedge> GetAllHyperedges() => _hyperedges.Values;

    /// <summary>
    /// Converts the hypergraph to a standard graph by replacing each hyperedge
    /// with a clique (complete subgraph) among its vertices.
    /// </summary>
    public UndirectedGraph<TVertex> ToCliqueGraph()
    {
        var graph = new UndirectedGraph<TVertex>();

        foreach (var vertex in _vertices)
            graph.AddVertex(vertex);

        foreach (var hyperedge in _hyperedges.Values)
        {
            var vertexList = hyperedge.Vertices.ToList();
            for (int i = 0; i < vertexList.Count; i++)
            {
                for (int j = i + 1; j < vertexList.Count; j++)
                {
                    graph.AddEdge(vertexList[i], vertexList[j]);
                }
            }
        }

        return graph;
    }

    /// <summary>
    /// Converts the hypergraph to a bipartite graph (vertices vs hyperedges).
    /// </summary>
    public BipartiteGraph<TVertex> ToBipartiteGraph()
    {
        var bipartite = new BipartiteGraph<TVertex>();

        var hyperedgeNodes = _hyperedges.Keys.Select(id => (TVertex)(object)$"HE_{id}"!);

        foreach (var vertex in _vertices)
            bipartite.AddVertexToSetU(vertex);

        foreach (var hyperedgeId in _hyperedges.Keys)
        {
            if (hyperedgeId is TVertex heVertex)
                bipartite.AddVertexToSetV(heVertex);
        }

        foreach (var (id, hyperedge) in _hyperedges)
        {
            if (id is TVertex heVertex)
            {
                foreach (var vertex in hyperedge.Vertices)
                    bipartite.AddEdge(vertex, heVertex);
            }
        }

        return bipartite;
    }

    /// <summary>
    /// Finds the dual hypergraph where vertices become hyperedges and vice versa.
    /// </summary>
    public Hypergraph<THyperedgeId, TVertex> GetDual()
    {
        var dual = new Hypergraph<THyperedgeId, TVertex>();

        foreach (var hyperedgeId in _hyperedges.Keys)
            dual.AddVertex(hyperedgeId);

        foreach (var vertex in _vertices)
        {
            var incidentHyperedges = _incidence[vertex];
            dual.AddHyperedge(vertex, incidentHyperedges);
        }

        return dual;
    }

    /// <summary>
    /// Performs a traversal starting from a vertex, exploring connected hyperedges.
    /// </summary>
    public IEnumerable<TVertex> BreadthFirstTraversal(TVertex start)
    {
        if (!_vertices.Contains(start))
            yield break;

        var visited = new HashSet<TVertex> { start };
        var queue = new Queue<TVertex>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            yield return current;

            foreach (var hyperedgeId in _incidence[current])
            {
                foreach (var neighbor in _hyperedges[hyperedgeId].Vertices)
                {
                    if (visited.Add(neighbor))
                        queue.Enqueue(neighbor);
                }
            }
        }
    }

    /// <summary>
    /// Finds connected components in the hypergraph.
    /// </summary>
    public IEnumerable<HashSet<TVertex>> GetConnectedComponents()
    {
        var visited = new HashSet<TVertex>();

        foreach (var vertex in _vertices)
        {
            if (visited.Contains(vertex))
                continue;

            var component = new HashSet<TVertex>();
            foreach (var v in BreadthFirstTraversal(vertex))
            {
                component.Add(v);
                visited.Add(v);
            }

            yield return component;
        }
    }

    /// <summary>
    /// Clears all vertices and hyperedges from the hypergraph.
    /// </summary>
    public void Clear()
    {
        _vertices.Clear();
        _hyperedges.Clear();
        _incidence.Clear();
    }
}

// Helper extension for BipartiteGraph
file static class HypergraphExtensions
{
    // This is just a placeholder - the actual BipartiteGraph doesn't have this exact signature
}
