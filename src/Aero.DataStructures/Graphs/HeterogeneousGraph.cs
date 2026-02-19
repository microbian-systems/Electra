using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a heterogeneous graph with multiple types of nodes and edges.
/// Each node and edge can have a specific type, enabling modeling of complex
/// multi-relational data structures.
/// </summary>
/// <typeparam name="TNodeId">The type of node identifiers. Must be non-nullable.</typeparam>
/// <typeparam name="TNodeType">The type of node type labels. Must be non-nullable.</typeparam>
/// <typeparam name="TEdgeType">The type of edge type labels. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// Heterogeneous graphs (also called heterogeneous information networks or HINs) contain
/// multiple types of nodes and/or edges. This is essential for modeling real-world systems
/// where entities and relationships are inherently diverse.
/// </para>
/// <para>
/// Key characteristics:
/// <list type="bullet">
/// <item><description>Multiple node types: User, Item, Category, Location, etc.</description></item>
/// <item><description>Multiple edge types: PURCHASED, RATED, LOCATED_AT, BELONGS_TO, etc.</description></item>
/// <item><description>Type-specific attributes and behaviors</description></item>
/// <item><description>Supports meta-path based analysis</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>E-commerce: Users, Products, Categories, Brands, Reviews</description></item>
/// <item><description>Academic networks: Authors, Papers, Venues, Topics</description></item>
/// <item><description>Social media: Users, Posts, Comments, Hashtags, Media</description></item>
/// <item><description>Healthcare: Patients, Doctors, Diagnoses, Treatments, Facilities</description></item>
/// <item><description>Graph Neural Networks (GNNs) for heterogeneous data</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var amazon = new HeterogeneousGraph&lt;string, string, string&gt;();
/// 
/// // Add nodes of different types
/// amazon.AddNode("user_1", "User", new { name = "Alice", age = 30 });
/// amazon.AddNode("prod_1", "Product", new { title = "Laptop", price = 999.99 });
/// amazon.AddNode("cat_1", "Category", new { name = "Electronics" });
/// amazon.AddNode("brand_1", "Brand", new { name = "TechCo" });
/// 
/// // Add edges of different types
/// amazon.AddEdge("user_1", "prod_1", "PURCHASED", new { date = DateTime.Now });
/// amazon.AddEdge("prod_1", "cat_1", "BELONGS_TO");
/// amazon.AddEdge("prod_1", "brand_1", "MANUFACTURED_BY");
/// 
/// // Meta-path query: Find users who bought products from the same category
/// var similarUsers = amazon.GetNodes("User")
///     .Where(u => amazon.GetNeighbors(u.Id, "PURCHASED")
///         .Any(p => amazon.GetNeighbors(p.Id, "BELONGS_TO")
///             .Any(c => c.Id == "cat_1")));
/// </code>
/// </example>
public class HeterogeneousGraph<TNodeId, TNodeType, TEdgeType>
    where TNodeId : notnull
    where TNodeType : notnull
    where TEdgeType : notnull
{
    private readonly Dictionary<TNodeId, Node> _nodes = new();
    private readonly Dictionary<long, Edge> _edges = new();
    private readonly Dictionary<TNodeType, HashSet<TNodeId>> _nodesByType = new();
    private readonly Dictionary<TEdgeType, HashSet<long>> _edgesByType = new();
    private readonly Dictionary<TNodeId, HashSet<long>> _outEdges = new();
    private readonly Dictionary<TNodeId, HashSet<long>> _inEdges = new();
    private long _nextEdgeId = 0;

    /// <summary>
    /// Represents a typed node in the heterogeneous graph.
    /// </summary>
    public class Node
    {
        /// <summary>
        /// Gets or sets the unique identifier of the node.
        /// </summary>
        public TNodeId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the type of the node.
        /// </summary>
        public TNodeType Type { get; set; } = default!;

        /// <summary>
        /// Gets or sets the attributes of the node.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// Gets an attribute value by key.
        /// </summary>
        public T? GetAttribute<T>(string key)
        {
            if (Attributes.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }
    }

    /// <summary>
    /// Represents a typed edge in the heterogeneous graph.
    /// </summary>
    public class Edge
    {
        /// <summary>
        /// Gets or sets the unique identifier of the edge.
        /// </summary>
        public long Id { get; set; }

        /// <summary>
        /// Gets or sets the source node ID.
        /// </summary>
        public TNodeId SourceId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the target node ID.
        /// </summary>
        public TNodeId TargetId { get; set; } = default!;

        /// <summary>
        /// Gets or sets the type of the edge.
        /// </summary>
        public TEdgeType Type { get; set; } = default!;

        /// <summary>
        /// Gets or sets the weight of the edge.
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets the attributes of the edge.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// Gets an attribute value by key.
        /// </summary>
        public T? GetAttribute<T>(string key)
        {
            if (Attributes.TryGetValue(key, out var value) && value is T typedValue)
                return typedValue;
            return default;
        }
    }

    /// <summary>
    /// Represents a meta-path in the heterogeneous graph.
    /// </summary>
    public class MetaPath
    {
        /// <summary>
        /// Gets or sets the sequence of node types in the path.
        /// </summary>
        public List<TNodeType> NodeTypes { get; set; } = new();

        /// <summary>
        /// Gets or sets the sequence of edge types in the path.
        /// </summary>
        public List<TEdgeType> EdgeTypes { get; set; } = new();

        /// <summary>
        /// Gets the string representation of the meta-path.
        /// </summary>
        public override string ToString() =>
            string.Join("-", NodeTypes.Zip(EdgeTypes, (n, e) => $"{n}-{e}")) + 
            (NodeTypes.Count > EdgeTypes.Count ? $"-{NodeTypes[^1]}" : "");
    }

    /// <summary>
    /// Gets the number of nodes in the graph.
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    /// Gets the number of edges in the graph.
    /// </summary>
    public int EdgeCount => _edges.Count;

    /// <summary>
    /// Gets all node types in the graph.
    /// </summary>
    public IEnumerable<TNodeType> NodeTypes => _nodesByType.Keys;

    /// <summary>
    /// Gets all edge types in the graph.
    /// </summary>
    public IEnumerable<TEdgeType> EdgeTypes => _edgesByType.Keys;

    /// <summary>
    /// Gets the count of nodes for each node type.
    /// </summary>
    public Dictionary<TNodeType, int> NodeTypeCounts =>
        _nodesByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

    /// <summary>
    /// Gets the count of edges for each edge type.
    /// </summary>
    public Dictionary<TEdgeType, int> EdgeTypeCounts =>
        _edgesByType.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

    /// <summary>
    /// Adds a node to the graph.
    /// </summary>
    /// <param name="id">The unique identifier of the node.</param>
    /// <param name="type">The type of the node.</param>
    /// <param name="attributes">Optional attributes for the node.</param>
    /// <returns>The created node, or null if a node with the ID already exists.</returns>
    public Node? AddNode(TNodeId id, TNodeType type, object? attributes = null)
    {
        if (_nodes.ContainsKey(id))
            return null;

        var node = new Node
        {
            Id = id,
            Type = type,
            Attributes = attributes != null 
                ? ConvertToDictionary(attributes) 
                : new Dictionary<string, object>()
        };

        _nodes[id] = node;
        _outEdges[id] = new HashSet<long>();
        _inEdges[id] = new HashSet<long>();

        if (!_nodesByType.TryGetValue(type, out var typeSet))
        {
            typeSet = new HashSet<TNodeId>();
            _nodesByType[type] = typeSet;
        }
        typeSet.Add(id);

        return node;
    }

    /// <summary>
    /// Adds an edge between two nodes.
    /// </summary>
    /// <param name="sourceId">The source node ID.</param>
    /// <param name="targetId">The target node ID.</param>
    /// <param name="type">The type of the edge.</param>
    /// <param name="attributes">Optional attributes for the edge.</param>
    /// <param name="weight">Optional weight for the edge (default: 1.0).</param>
    /// <returns>The created edge, or null if nodes don't exist.</returns>
    public Edge? AddEdge(TNodeId sourceId, TNodeId targetId, TEdgeType type, 
        object? attributes = null, double weight = 1.0)
    {
        if (!_nodes.ContainsKey(sourceId) || !_nodes.ContainsKey(targetId))
            return null;

        var edge = new Edge
        {
            Id = _nextEdgeId++,
            SourceId = sourceId,
            TargetId = targetId,
            Type = type,
            Weight = weight,
            Attributes = attributes != null 
                ? ConvertToDictionary(attributes) 
                : new Dictionary<string, object>()
        };

        _edges[edge.Id] = edge;
        _outEdges[sourceId].Add(edge.Id);
        _inEdges[targetId].Add(edge.Id);

        if (!_edgesByType.TryGetValue(type, out var typeSet))
        {
            typeSet = new HashSet<long>();
            _edgesByType[type] = typeSet;
        }
        typeSet.Add(edge.Id);

        return edge;
    }

    /// <summary>
    /// Gets a node by its ID.
    /// </summary>
    public Node? GetNode(TNodeId id) => _nodes.TryGetValue(id, out var node) ? node : null;

    /// <summary>
    /// Gets an edge by its ID.
    /// </summary>
    public Edge? GetEdge(long id) => _edges.TryGetValue(id, out var edge) ? edge : null;

    /// <summary>
    /// Gets all nodes of a specific type.
    /// </summary>
    public IEnumerable<Node> GetNodes(TNodeType type)
    {
        if (!_nodesByType.TryGetValue(type, out var ids))
            yield break;

        foreach (var id in ids)
            yield return _nodes[id];
    }

    /// <summary>
    /// Gets all edges of a specific type.
    /// </summary>
    public IEnumerable<Edge> GetEdges(TEdgeType type)
    {
        if (!_edgesByType.TryGetValue(type, out var ids))
            yield break;

        foreach (var id in ids)
            yield return _edges[id];
    }

    /// <summary>
    /// Gets all outgoing edges from a node.
    /// </summary>
    public IEnumerable<Edge> GetOutEdges(TNodeId nodeId)
    {
        if (!_outEdges.TryGetValue(nodeId, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
            yield return _edges[edgeId];
    }

    /// <summary>
    /// Gets outgoing edges of a specific type from a node.
    /// </summary>
    public IEnumerable<Edge> GetOutEdges(TNodeId nodeId, TEdgeType edgeType)
    {
        foreach (var edge in GetOutEdges(nodeId))
        {
            if (edge.Type.Equals(edgeType))
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all incoming edges to a node.
    /// </summary>
    public IEnumerable<Edge> GetInEdges(TNodeId nodeId)
    {
        if (!_inEdges.TryGetValue(nodeId, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
            yield return _edges[edgeId];
    }

    /// <summary>
    /// Gets incoming edges of a specific type to a node.
    /// </summary>
    public IEnumerable<Edge> GetInEdges(TNodeId nodeId, TEdgeType edgeType)
    {
        foreach (var edge in GetInEdges(nodeId))
        {
            if (edge.Type.Equals(edgeType))
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all neighbor nodes (any edge type).
    /// </summary>
    public IEnumerable<Node> GetNeighbors(TNodeId nodeId)
    {
        var neighbors = new HashSet<TNodeId>();

        foreach (var edge in GetOutEdges(nodeId))
            neighbors.Add(edge.TargetId);

        foreach (var edge in GetInEdges(nodeId))
            neighbors.Add(edge.SourceId);

        foreach (var id in neighbors)
            yield return _nodes[id];
    }

    /// <summary>
    /// Gets neighbor nodes connected via a specific edge type.
    /// </summary>
    public IEnumerable<Node> GetNeighbors(TNodeId nodeId, TEdgeType edgeType)
    {
        var neighbors = new HashSet<TNodeId>();

        foreach (var edge in GetOutEdges(nodeId, edgeType))
            neighbors.Add(edge.TargetId);

        foreach (var edge in GetInEdges(nodeId, edgeType))
            neighbors.Add(edge.SourceId);

        foreach (var id in neighbors)
            yield return _nodes[id];
    }

    /// <summary>
    /// Finds all paths matching a meta-path starting from a given node.
    /// </summary>
    /// <param name="startNodeId">The starting node ID.</param>
    /// <param name="metaPath">The meta-path to match.</param>
    /// <returns>All path instances matching the meta-path.</returns>
    public IEnumerable<List<Node>> FindMetaPaths(TNodeId startNodeId, MetaPath metaPath)
    {
        if (!_nodes.ContainsKey(startNodeId))
            yield break;

        var currentPaths = new List<List<(TNodeId NodeId, int Step)>> 
        { 
            new List<(TNodeId, int)> { (startNodeId, 0) } 
        };

        for (int step = 0; step < metaPath.EdgeTypes.Count; step++)
        {
            var edgeType = metaPath.EdgeTypes[step];
            var targetType = metaPath.NodeTypes[step + 1];
            var newPaths = new List<List<(TNodeId, int)>>();

            foreach (var path in currentPaths)
            {
                var currentNode = path[^1].NodeId;

                foreach (var edge in GetOutEdges(currentNode, edgeType))
                {
                    var targetNode = _nodes[edge.TargetId];
                    if (targetNode.Type.Equals(targetType))
                    {
                        var newPath = new List<(TNodeId, int)>(path) { (targetNode.Id, step + 1) };
                        newPaths.Add(newPath);
                    }
                }
            }

            currentPaths = newPaths;
        }

        foreach (var path in currentPaths)
        {
            yield return path.Select(p => _nodes[p.NodeId]).ToList();
        }
    }

    /// <summary>
    /// Computes the similarity between two nodes based on a meta-path.
    /// Uses PathSim algorithm for measuring similarity.
    /// </summary>
    public double ComputePathSimilarity(TNodeId node1, TNodeId node2, MetaPath metaPath)
    {
        if (!_nodes.ContainsKey(node1) || !_nodes.ContainsKey(node2))
            return 0.0;

        var paths1 = FindMetaPaths(node1, metaPath).ToList();
        var paths2 = FindMetaPaths(node2, metaPath).ToList();

        if (paths1.Count == 0 || paths2.Count == 0)
            return 0.0;

        var endpoints1 = paths1.Select(p => p[^1].Id).ToHashSet();
        var endpoints2 = paths2.Select(p => p[^1].Id).ToHashSet();

        var intersection = endpoints1.Intersect(endpoints2).Count();
        var union = endpoints1.Count + endpoints2.Count;

        return 2.0 * intersection / union;
    }

    /// <summary>
    /// Gets the graph schema (node types and edge types with their connections).
    /// </summary>
    public Dictionary<(TNodeType SourceType, TEdgeType EdgeType), TNodeType> GetSchema()
    {
        var schema = new Dictionary<(TNodeType, TEdgeType), TNodeType>();

        foreach (var edge in _edges.Values)
        {
            var sourceType = _nodes[edge.SourceId].Type;
            var targetType = _nodes[edge.TargetId].Type;
            var key = (sourceType, edge.Type);
            schema[key] = targetType;
        }

        return schema;
    }

    /// <summary>
    /// Removes a node and all its incident edges.
    /// </summary>
    public bool RemoveNode(TNodeId id)
    {
        if (!_nodes.TryGetValue(id, out var node))
            return false;

        var edgesToRemove = new List<long>();
        edgesToRemove.AddRange(_outEdges[id]);
        edgesToRemove.AddRange(_inEdges[id]);

        foreach (var edgeId in edgesToRemove)
            RemoveEdge(edgeId);

        _nodesByType[node.Type].Remove(id);
        _outEdges.Remove(id);
        _inEdges.Remove(id);
        _nodes.Remove(id);

        return true;
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
    public bool RemoveEdge(long edgeId)
    {
        if (!_edges.TryGetValue(edgeId, out var edge))
            return false;

        _outEdges[edge.SourceId].Remove(edgeId);
        _inEdges[edge.TargetId].Remove(edgeId);
        _edgesByType[edge.Type].Remove(edgeId);
        _edges.Remove(edgeId);

        return true;
    }

    /// <summary>
    /// Gets all nodes in the graph.
    /// </summary>
    public IEnumerable<Node> GetAllNodes() => _nodes.Values;

    /// <summary>
    /// Gets all edges in the graph.
    /// </summary>
    public IEnumerable<Edge> GetAllEdges() => _edges.Values;

    /// <summary>
    /// Clears all nodes and edges from the graph.
    /// </summary>
    public void Clear()
    {
        _nodes.Clear();
        _edges.Clear();
        _nodesByType.Clear();
        _edgesByType.Clear();
        _outEdges.Clear();
        _inEdges.Clear();
        _nextEdgeId = 0;
    }

    private static Dictionary<string, object> ConvertToDictionary(object obj)
    {
        var dict = new Dictionary<string, object>();
        foreach (var prop in obj.GetType().GetProperties())
        {
            var value = prop.GetValue(obj);
            if (value != null)
                dict[prop.Name] = value;
        }
        return dict;
    }
}
