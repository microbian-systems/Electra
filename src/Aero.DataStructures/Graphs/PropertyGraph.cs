using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a labeled property graph where both vertices and edges can have
/// types (labels) and arbitrary properties (key-value pairs).
/// </summary>
/// <typeparam name="TVertexId">The type of vertex identifiers. Must be non-nullable.</typeparam>
/// <typeparam name="TEdgeId">The type of edge identifiers. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// Property graphs are the most expressive graph model, combining the features of
/// attributed graphs with labeled graphs. They are the foundation of graph databases
/// like Neo4j, Amazon Neptune, and Azure Cosmos DB.
/// </para>
/// <para>
/// Key features:
/// <list type="bullet">
/// <item><description>Vertices have types (labels) like "Person", "Company", "Product"</description></item>
/// <item><description>Edges have types (relationship names) like "KNOWS", "WORKS_FOR", "BOUGHT"</description></item>
/// <item><description>Both can have arbitrary properties: {name: "Alice", age: 30}</description></item>
/// <item><description>Supports rich queries and pattern matching</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Social networks with rich metadata</description></item>
/// <item><description>Knowledge graphs and ontologies</description></item>
/// <item><description>Fraud detection with relationship analysis</description></item>
/// <item><description>Recommendation engines</description></item>
/// <item><description>Master data management</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var socialGraph = new PropertyGraph&lt;string, long&gt;();
/// 
/// // Add vertices with labels and properties
/// socialGraph.AddVertex("alice", "Person", new Dictionary&lt;string, object&gt;
/// {
///     ["name"] = "Alice Johnson",
///     ["age"] = 30,
///     ["email"] = "alice@example.com"
/// });
/// 
/// socialGraph.AddVertex("acme", "Company", new Dictionary&lt;string, object&gt;
/// {
///     ["name"] = "Acme Corp",
///     ["founded"] = 2010
/// });
/// 
/// // Add edge with relationship type and properties
/// socialGraph.AddEdge("alice", "acme", 1, "WORKS_FOR", new Dictionary&lt;string, object&gt;
/// {
///     ["since"] = 2020,
///     ["position"] = "Engineer"
/// });
/// 
/// // Query: Find all employees of Acme Corp
/// var employees = socialGraph.GetVerticesByLabel("Person")
///     .Where(v => socialGraph.GetOutEdges(v.Id, "WORKS_FOR")
///         .Any(e => e.TargetId.Equals("acme")));
/// </code>
/// </example>
public class PropertyGraph<TVertexId, TEdgeId> 
    where TVertexId : notnull
    where TEdgeId : notnull
{
    private readonly Dictionary<TVertexId, Vertex> _vertices = new();
    private readonly Dictionary<TEdgeId, Edge> _edges = new();
    private readonly Dictionary<string, HashSet<TVertexId>> _verticesByLabel = new();
    private readonly Dictionary<TVertexId, HashSet<TEdgeId>> _outEdges = new();
    private readonly Dictionary<TVertexId, HashSet<TEdgeId>> _inEdges = new();

    /// <summary>
    /// Represents a vertex in the property graph.
    /// </summary>
    public class Vertex
    {
        /// <summary>
        /// Gets or sets the unique identifier of the vertex.
        /// </summary>
        public TVertexId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the label (type) of the vertex.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the properties of the vertex.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Gets a property value by key.
        /// </summary>
        public T? GetProperty<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Sets a property value.
        /// </summary>
        public void SetProperty(string key, object value) => Properties[key] = value;
    }

    /// <summary>
    /// Represents an edge in the property graph.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Gets or sets the unique identifier of the edge.
        /// </summary>
        public TEdgeId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the source vertex ID.
        /// </summary>
        public TVertexId SourceId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the target vertex ID.
        /// </summary>
        public TVertexId TargetId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the relationship type (label) of the edge.
        /// </summary>
        public string Label { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the properties of the edge.
        /// </summary>
        public Dictionary<string, object> Properties { get; set; } = new();

        /// <summary>
        /// Gets a property value by key.
        /// </summary>
        public T? GetProperty<T>(string key)
        {
            if (Properties.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }

        /// <summary>
        /// Sets a property value.
        /// </summary>
        public void SetProperty(string key, object value) => Properties[key] = value;
    }

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    public int VertexCount => _vertices.Count;

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    public int EdgeCount => _edges.Count;

    /// <summary>
    /// Gets all vertex labels used in the graph.
    /// </summary>
    public IEnumerable<string> VertexLabels => _verticesByLabel.Keys;

    /// <summary>
    /// Adds a vertex to the graph.
    /// </summary>
    /// <param name="id">The unique identifier of the vertex.</param>
    /// <param name="label">The label (type) of the vertex.</param>
    /// <param name="properties">Optional properties for the vertex.</param>
    /// <returns>The created vertex, or null if a vertex with the ID already exists.</returns>
    public Vertex? AddVertex(TVertexId id, string label, Dictionary<string, object>? properties = null)
    {
        if (_vertices.ContainsKey(id))
            return null;

        var vertex = new Vertex
        {
            Id = id,
            Label = label,
            Properties = properties != null ? new Dictionary<string, object>(properties) : new Dictionary<string, object>()
        };

        _vertices[id] = vertex;
        _outEdges[id] = new HashSet<TEdgeId>();
        _inEdges[id] = new HashSet<TEdgeId>();

        if (!_verticesByLabel.TryGetValue(label, out var labelSet))
        {
            labelSet = new HashSet<TVertexId>();
            _verticesByLabel[label] = labelSet;
        }
        labelSet.Add(id);

        return vertex;
    }

    /// <summary>
    /// Adds an edge between two vertices.
    /// </summary>
    /// <param name="sourceId">The source vertex ID.</param>
    /// <param name="targetId">The target vertex ID.</param>
    /// <param name="edgeId">The unique identifier of the edge.</param>
    /// <param name="label">The relationship type (label) of the edge.</param>
    /// <param name="properties">Optional properties for the edge.</param>
    /// <returns>The created edge, or null if vertices don't exist or edge ID is duplicate.</returns>
    public Edge? AddEdge(TVertexId sourceId, TVertexId targetId, TEdgeId edgeId, string label, 
        Dictionary<string, object>? properties = null)
    {
        if (!_vertices.ContainsKey(sourceId) || !_vertices.ContainsKey(targetId))
            return null;

        if (_edges.ContainsKey(edgeId))
            return null;

        var edge = new Edge
        {
            Id = edgeId,
            SourceId = sourceId,
            TargetId = targetId,
            Label = label,
            Properties = properties != null ? new Dictionary<string, object>(properties) : new Dictionary<string, object>()
        };

        _edges[edgeId] = edge;
        _outEdges[sourceId].Add(edgeId);
        _inEdges[targetId].Add(edgeId);

        return edge;
    }

    /// <summary>
    /// Gets a vertex by its ID.
    /// </summary>
    public Vertex? GetVertex(TVertexId id) => _vertices.TryGetValue(id, out var vertex) ? vertex : null;

    /// <summary>
    /// Gets an edge by its ID.
    /// </summary>
    public Edge? GetEdge(TEdgeId id) => _edges.TryGetValue(id, out var edge) ? edge : null;

    /// <summary>
    /// Gets all vertices with a specific label.
    /// </summary>
    public IEnumerable<Vertex> GetVerticesByLabel(string label)
    {
        if (!_verticesByLabel.TryGetValue(label, out var ids))
            yield break;

        foreach (var id in ids)
            yield return _vertices[id];
    }

    /// <summary>
    /// Gets all outgoing edges from a vertex.
    /// </summary>
    public IEnumerable<Edge> GetOutEdges(TVertexId vertexId)
    {
        if (!_outEdges.TryGetValue(vertexId, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
            yield return _edges[edgeId];
    }

    /// <summary>
    /// Gets all outgoing edges with a specific label from a vertex.
    /// </summary>
    public IEnumerable<Edge> GetOutEdges(TVertexId vertexId, string label)
    {
        foreach (var edge in GetOutEdges(vertexId))
        {
            if (edge.Label == label)
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all incoming edges to a vertex.
    /// </summary>
    public IEnumerable<Edge> GetInEdges(TVertexId vertexId)
    {
        if (!_inEdges.TryGetValue(vertexId, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
            yield return _edges[edgeId];
    }

    /// <summary>
    /// Gets all incoming edges with a specific label to a vertex.
    /// </summary>
    public IEnumerable<Edge> GetInEdges(TVertexId vertexId, string label)
    {
        foreach (var edge in GetInEdges(vertexId))
        {
            if (edge.Label == label)
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all neighbors of a vertex reachable via outgoing edges.
    /// </summary>
    public IEnumerable<Vertex> GetOutNeighbors(TVertexId vertexId)
    {
        foreach (var edge in GetOutEdges(vertexId))
            yield return _vertices[edge.TargetId];
    }

    /// <summary>
    /// Gets all neighbors of a vertex reachable via outgoing edges with a specific label.
    /// </summary>
    public IEnumerable<Vertex> GetOutNeighbors(TVertexId vertexId, string label)
    {
        foreach (var edge in GetOutEdges(vertexId, label))
            yield return _vertices[edge.TargetId];
    }

    /// <summary>
    /// Removes a vertex and all its incident edges.
    /// </summary>
    public bool RemoveVertex(TVertexId id)
    {
        if (!_vertices.TryGetValue(id, out var vertex))
            return false;

        var edgesToRemove = new List<TEdgeId>();
        edgesToRemove.AddRange(_outEdges[id]);
        edgesToRemove.AddRange(_inEdges[id]);

        foreach (var edgeId in edgesToRemove)
            RemoveEdge(edgeId);

        _verticesByLabel[vertex.Label].Remove(id);
        _outEdges.Remove(id);
        _inEdges.Remove(id);
        _vertices.Remove(id);

        return true;
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    public bool RemoveEdge(TEdgeId id)
    {
        if (!_edges.TryGetValue(id, out var edge))
            return false;

        _outEdges[edge.SourceId].Remove(id);
        _inEdges[edge.TargetId].Remove(id);
        _edges.Remove(id);

        return true;
    }

    /// <summary>
    /// Finds all vertices that match a property condition.
    /// </summary>
    public IEnumerable<Vertex> FindVertices(string propertyKey, object value)
    {
        foreach (var vertex in _vertices.Values)
        {
            if (vertex.Properties.TryGetValue(propertyKey, out var propValue) && 
                Equals(propValue, value))
            {
                yield return vertex;
            }
        }
    }

    /// <summary>
    /// Finds all edges that match a property condition.
    /// </summary>
    public IEnumerable<Edge> FindEdges(string propertyKey, object value)
    {
        foreach (var edge in _edges.Values)
        {
            if (edge.Properties.TryGetValue(propertyKey, out var propValue) && 
                Equals(propValue, value))
            {
                yield return edge;
            }
        }
    }

    /// <summary>
    /// Performs a simple pattern match: (source)-[edgeLabel]->(target)
    /// </summary>
    public IEnumerable<(Vertex Source, Edge Edge, Vertex Target)> MatchPattern(
        TVertexId sourceId, string edgeLabel)
    {
        foreach (var edge in GetOutEdges(sourceId, edgeLabel))
        {
            yield return (_vertices[sourceId], edge, _vertices[edge.TargetId]);
        }
    }

    /// <summary>
    /// Performs a two-hop pattern match: (source)-[label1]->()-[label2]->(target)
    /// </summary>
    public IEnumerable<(Vertex Intermediate, Vertex Target)> MatchTwoHop(
        TVertexId sourceId, string firstEdgeLabel, string secondEdgeLabel)
    {
        foreach (var firstEdge in GetOutEdges(sourceId, firstEdgeLabel))
        {
            var intermediate = _vertices[firstEdge.TargetId];
            foreach (var secondEdge in GetOutEdges(intermediate.Id, secondEdgeLabel))
            {
                yield return (intermediate, _vertices[secondEdge.TargetId]);
            }
        }
    }

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IEnumerable<Vertex> GetAllVertices() => _vertices.Values;

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IEnumerable<Edge> GetAllEdges() => _edges.Values;

    /// <summary>
    /// Clears all vertices and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _vertices.Clear();
        _edges.Clear();
        _verticesByLabel.Clear();
        _outEdges.Clear();
        _inEdges.Clear();
    }
}
