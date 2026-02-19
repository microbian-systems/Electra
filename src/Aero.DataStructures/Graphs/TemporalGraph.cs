using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a temporal graph (time-evolving/dynamic graph) where vertices and edges
/// can appear, change, and disappear over time.
/// </summary>
/// <typeparam name="TVertex">The type of vertex identifiers. Must be non-nullable.</typeparam>
/// <typeparam name="TEdgeId">The type of edge identifiers. Must be non-nullable.</typeparam>
/// <typeparam name="TTime">The type representing time. Must implement IComparable.</typeparam>
/// <remarks>
/// <para>
/// Temporal graphs capture the dynamics of relationships over time. Unlike static graphs,
/// they record when relationships start and end, enabling analysis of how networks evolve.
/// </para>
/// <para>
/// Key concepts:
/// <list type="bullet">
/// <item><description>Temporal vertex: A vertex that may exist only during certain time intervals</description></item>
/// <item><description>Temporal edge: An edge with start and end times (duration)</description></item>
/// <item><description>Snapshot: The static graph at a specific point in time</description></item>
/// <item><description>Time interval: [start, end) or discrete time points</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Social network evolution (who follows whom over time)</description></item>
/// <item><description>Contact graphs (communication patterns)</description></item>
/// <item><description>Transportation networks (flights, routes)</description></item>
/// <item><description>Epidemic spreading models</description></item>
/// <item><description>Financial transaction networks</description></item>
/// <item><description>Citation networks with publication dates</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var socialNetwork = new TemporalGraph&lt;string, long, DateTime&gt;();
/// 
/// // Add users with their lifetime on the platform
/// socialNetwork.AddVertex("Alice", new DateTime(2020, 1, 1), new DateTime(2024, 1, 1));
/// socialNetwork.AddVertex("Bob", new DateTime(2020, 6, 1));
/// socialNetwork.AddVertex("Charlie", new DateTime(2021, 1, 1));
/// 
/// // Add friendships with timestamps
/// socialNetwork.AddEdge("Alice", "Bob", 1, new DateTime(2020, 6, 15), new DateTime(2023, 6, 15));
/// socialNetwork.AddEdge("Alice", "Charlie", 2, new DateTime(2021, 2, 1));
/// socialNetwork.AddEdge("Bob", "Charlie", 3, new DateTime(2021, 3, 1));
/// 
/// // Get snapshot at a specific time
/// var snapshot2021 = socialNetwork.GetSnapshot(new DateTime(2021, 6, 1));
/// // Alice-Bob, Alice-Charlie, Bob-Charlie all exist
/// 
/// var snapshot2023 = socialNetwork.GetSnapshot(new DateTime(2023, 12, 1));
/// // Alice-Bob ended, only Alice-Charlie and Bob-Charlie exist
/// 
/// // Find temporal paths (respecting time order)
/// var paths = socialNetwork.GetTemporalPaths("Alice", "Charlie", new DateTime(2021, 1, 1));
/// </code>
/// </example>
public class TemporalGraph<TVertex, TEdgeId, TTime>
    where TVertex : notnull
    where TEdgeId : notnull
    where TTime : IComparable<TTime>
{
    private readonly Dictionary<TVertex, TemporalVertex> _vertices = new();
    private readonly Dictionary<TEdgeId, TemporalEdge> _edges = new();
    private readonly Dictionary<TVertex, HashSet<TEdgeId>> _outEdges = new();
    private readonly Dictionary<TVertex, HashSet<TEdgeId>> _inEdges = new();

    /// <summary>
    /// Represents a time interval.
    /// </summary>
    public readonly struct TimeInterval
    {
        /// <summary>
        /// Gets the start time of the interval.
        /// </summary>
        public TTime Start { get; }

        /// <summary>
        /// Gets the end time of the interval (exclusive).
        /// </summary>
        public TTime? End { get; }

        /// <summary>
        /// Gets whether the interval is open-ended (no end time).
        /// </summary>
        public bool IsOpenEnded => End == null;

        /// <summary>
        /// Creates a new time interval.
        /// </summary>
        public TimeInterval(TTime start, TTime? end = default)
        {
            Start = start;
            End = end;
        }

        /// <summary>
        /// Checks if a time point is within this interval.
        /// </summary>
        public bool Contains(TTime time)
        {
            if (time.CompareTo(Start) < 0)
                return false;

            if (End != null && time.CompareTo(End) >= 0)
                return false;

            return true;
        }

        /// <summary>
        /// Checks if this interval overlaps with another.
        /// </summary>
        public bool Overlaps(TimeInterval other)
        {
            if (End != null && other.Start.CompareTo(End) >= 0)
                return false;

            if (other.End != null && Start.CompareTo(other.End) >= 0)
                return false;

            return true;
        }
    }

    /// <summary>
    /// Represents a temporal vertex with a lifetime.
    /// </summary>
    public class TemporalVertex
    {
        /// <summary>
        /// Gets or sets the vertex identifier.
        /// </summary>
        public TVertex Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the time interval during which this vertex exists.
        /// </summary>
        public TimeInterval Lifetime { get; set; }

        /// <summary>
        /// Gets or sets optional attributes.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// Checks if the vertex exists at the given time.
        /// </summary>
        public bool ExistsAt(TTime time) => Lifetime.Contains(time);
    }

    /// <summary>
    /// Represents a temporal edge with a lifetime.
    /// </summary>
    public class TemporalEdge
    {
        /// <summary>
        /// Gets or sets the edge identifier.
        /// </summary>
        public TEdgeId Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the source vertex.
        /// </summary>
        public TVertex Source { get; set; } = default!;

        /// <summary>
        /// Gets or sets the target vertex.
        /// </summary>
        public TVertex Target { get; set; } = default!;

        /// <summary>
        /// Gets or sets the time interval during which this edge exists.
        /// </summary>
        public TimeInterval Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the weight of the edge.
        /// </summary>
        public double Weight { get; set; } = 1.0;

        /// <summary>
        /// Gets or sets optional attributes.
        /// </summary>
        public Dictionary<string, object> Attributes { get; set; } = new();

        /// <summary>
        /// Checks if the edge exists at the given time.
        /// </summary>
        public bool ExistsAt(TTime time) => Lifetime.Contains(time);
    }

    /// <summary>
    /// Represents a point in a temporal path.
    /// </summary>
    public class TemporalPathPoint
    {
        /// <summary>
        /// Gets or sets the vertex at this point.
        /// </summary>
        public TVertex Vertex { get; set; } = default!;

        /// <summary>
        /// Gets or sets the arrival time at this vertex.
        /// </summary>
        public TTime ArrivalTime { get; set; } = default!;

        /// <summary>
        /// Gets or sets the edge used to reach this vertex (null for start).
        /// </summary>
        public TEdgeId? EdgeId { get; set; }

        /// <summary>
        /// Gets whether this point has an associated edge.
        /// </summary>
        public bool HasEdge { get; set; }
    }

    /// <summary>
    /// Gets the number of vertices in the temporal graph.
    /// </summary>
    public int VertexCount => _vertices.Count;

    /// <summary>
    /// Gets the number of edges in the temporal graph.
    /// </summary>
    public int EdgeCount => _edges.Count;

    /// <summary>
    /// Adds a vertex with a lifetime.
    /// </summary>
    /// <param name="vertex">The vertex identifier.</param>
    /// <param name="start">The start time.</param>
    /// <param name="end">The optional end time (null for open-ended).</param>
    /// <param name="attributes">Optional attributes.</param>
    /// <returns>The created temporal vertex.</returns>
    public TemporalVertex AddVertex(TVertex vertex, TTime start, TTime? end = default, 
        Dictionary<string, object>? attributes = null)
    {
        var temporalVertex = new TemporalVertex
        {
            Id = vertex,
            Lifetime = new TimeInterval(start, end),
            Attributes = attributes ?? new Dictionary<string, object>()
        };

        _vertices[vertex] = temporalVertex;
        _outEdges[vertex] = new HashSet<TEdgeId>();
        _inEdges[vertex] = new HashSet<TEdgeId>();

        return temporalVertex;
    }

    /// <summary>
    /// Adds an edge with a lifetime.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="target">The target vertex.</param>
    /// <param name="edgeId">The edge identifier.</param>
    /// <param name="start">The start time.</param>
    /// <param name="end">The optional end time (null for open-ended).</param>
    /// <param name="weight">The optional weight.</param>
    /// <param name="attributes">Optional attributes.</param>
    /// <returns>The created temporal edge.</returns>
    public TemporalEdge AddEdge(TVertex source, TVertex target, TEdgeId edgeId,
        TTime start, TTime? end = default, double weight = 1.0,
        Dictionary<string, object>? attributes = null)
    {
        if (!_vertices.ContainsKey(source))
            throw new ArgumentException($"Source vertex '{source}' not found.");

        if (!_vertices.ContainsKey(target))
            throw new ArgumentException($"Target vertex '{target}' not found.");

        var temporalEdge = new TemporalEdge
        {
            Id = edgeId,
            Source = source,
            Target = target,
            Lifetime = new TimeInterval(start, end),
            Weight = weight,
            Attributes = attributes ?? new Dictionary<string, object>()
        };

        _edges[edgeId] = temporalEdge;
        _outEdges[source].Add(edgeId);
        _inEdges[target].Add(edgeId);

        return temporalEdge;
    }

    /// <summary>
    /// Gets a vertex by its ID.
    /// </summary>
    public TemporalVertex? GetVertex(TVertex vertex) =>
        _vertices.TryGetValue(vertex, out var v) ? v : null;

    /// <summary>
    /// Gets an edge by its ID.
    /// </summary>
    public TemporalEdge? GetEdge(TEdgeId edgeId) =>
        _edges.TryGetValue(edgeId, out var e) ? e : null;

    /// <summary>
    /// Gets all outgoing edges from a vertex at a specific time.
    /// </summary>
    public IEnumerable<TemporalEdge> GetOutEdges(TVertex vertex, TTime time)
    {
        if (!_outEdges.TryGetValue(vertex, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
        {
            var edge = _edges[edgeId];
            if (edge.ExistsAt(time))
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all incoming edges to a vertex at a specific time.
    /// </summary>
    public IEnumerable<TemporalEdge> GetInEdges(TVertex vertex, TTime time)
    {
        if (!_inEdges.TryGetValue(vertex, out var edgeIds))
            yield break;

        foreach (var edgeId in edgeIds)
        {
            var edge = _edges[edgeId];
            if (edge.ExistsAt(time))
                yield return edge;
        }
    }

    /// <summary>
    /// Creates a static snapshot of the graph at a specific time.
    /// </summary>
    /// <param name="time">The time point for the snapshot.</param>
    /// <returns>A static directed graph representing the state at the given time.</returns>
    public DirectedGraph<TVertex> GetSnapshot(TTime time)
    {
        var snapshot = new DirectedGraph<TVertex>();

        foreach (var (id, vertex) in _vertices)
        {
            if (vertex.ExistsAt(time))
                snapshot.AddVertex(id);
        }

        foreach (var edge in _edges.Values)
        {
            if (edge.ExistsAt(time) && 
                _vertices[edge.Source].ExistsAt(time) && 
                _vertices[edge.Target].ExistsAt(time))
            {
                snapshot.AddEdge(edge.Source, edge.Target);
            }
        }

        return snapshot;
    }

    /// <summary>
    /// Gets the evolution of the graph over a time range.
    /// </summary>
    /// <param name="startTime">The start of the time range.</param>
    /// <param name="endTime">The end of the time range.</param>
    /// <param name="step">The number of snapshots to generate.</param>
    /// <returns>A list of time-snapshot pairs.</returns>
    public List<(TTime Time, DirectedGraph<TVertex> Graph)> GetEvolution(
        TTime startTime, TTime endTime, int steps)
    {
        var result = new List<(TTime, DirectedGraph<TVertex>)>();
        var times = GetTimePoints(startTime, endTime, steps);

        foreach (var time in times)
        {
            result.Add((time, GetSnapshot(time)));
        }

        return result;
    }

    private List<TTime> GetTimePoints(TTime start, TTime end, int steps)
    {
        var points = new List<TTime>();
        
        if (steps <= 1)
        {
            points.Add(start);
            return points;
        }

        dynamic startDynamic = start;
        dynamic endDynamic = end;
        dynamic stepSize = (endDynamic - startDynamic) / (steps - 1);

        for (int i = 0; i < steps; i++)
        {
            dynamic point = startDynamic + (stepSize * i);
            points.Add((TTime)point);
        }

        return points;
    }

    /// <summary>
    /// Finds all temporal paths from source to destination starting at or after the given time.
    /// A temporal path respects time ordering - you can only traverse edges that exist
    /// after your arrival time at each vertex.
    /// </summary>
    public IEnumerable<List<TemporalPathPoint>> GetTemporalPaths(
        TVertex source, TVertex destination, TTime startTime, int maxHops = 10)
    {
        if (!_vertices.TryGetValue(source, out var sourceVertex) || 
            !_vertices.TryGetValue(destination, out var destVertex))
            yield break;

        var paths = new List<List<TemporalPathPoint>>();
        var initialPath = new List<TemporalPathPoint>
        {
            new() { Vertex = source, ArrivalTime = startTime, EdgeId = default, HasEdge = false }
        };

        ExploreTemporalPaths(source, destination, startTime, initialPath, paths, maxHops, new HashSet<(TVertex, TTime)>());

        foreach (var path in paths)
            yield return path;
    }

    private void ExploreTemporalPaths(
        TVertex current, TVertex destination, TTime currentTime,
        List<TemporalPathPoint> currentPath, List<List<TemporalPathPoint>> results,
        int maxHops, HashSet<(TVertex, TTime)> visited)
    {
        if (currentPath.Count > maxHops)
            return;

        if (EqualityComparer<TVertex>.Default.Equals(current, destination))
        {
            results.Add(new List<TemporalPathPoint>(currentPath));
            return;
        }

        foreach (var edge in _edges.Values.Where(e => 
            e.Source.Equals(current) && 
            e.Lifetime.Start.CompareTo(currentTime) >= 0))
        {
            var edgeTime = edge.Lifetime.Start;
            var stateKey = (edge.Target, edgeTime);
            
            if (visited.Contains(stateKey))
                continue;

            visited.Add(stateKey);

            currentPath.Add(new TemporalPathPoint
            {
                Vertex = edge.Target,
                ArrivalTime = edgeTime,
                EdgeId = edge.Id,
                HasEdge = true
            });

            ExploreTemporalPaths(edge.Target, destination, edgeTime, 
                currentPath, results, maxHops, visited);

            currentPath.RemoveAt(currentPath.Count - 1);
            visited.Remove(stateKey);
        }
    }

    /// <summary>
    /// Gets the earliest arrival time at the destination from the source.
    /// </summary>
    public TTime? GetEarliestArrival(TVertex source, TVertex destination, TTime startTime)
    {
        if (!_vertices.ContainsKey(source) || !_vertices.ContainsKey(destination))
            return default;

        var earliestArrival = new Dictionary<TVertex, TTime>();
        var priorityQueue = new PriorityQueue<TVertex, TTime>();

        earliestArrival[source] = startTime;
        priorityQueue.Enqueue(source, startTime);

        while (priorityQueue.Count > 0)
        {
            var current = priorityQueue.Dequeue();
            var currentTime = earliestArrival[current];

            if (EqualityComparer<TVertex>.Default.Equals(current, destination))
                return currentTime;

            foreach (var edge in _edges.Values.Where(e => 
                e.Source.Equals(current) && 
                e.Lifetime.Start.CompareTo(currentTime) >= 0))
            {
                var arrivalTime = edge.Lifetime.Start;

                if (!earliestArrival.TryGetValue(edge.Target, out var existing) ||
                    arrivalTime.CompareTo(existing) < 0)
                {
                    earliestArrival[edge.Target] = arrivalTime;
                    priorityQueue.Enqueue(edge.Target, arrivalTime);
                }
            }
        }

        return default;
    }

    /// <summary>
    /// Finds all edges that were active during a time interval.
    /// </summary>
    public IEnumerable<TemporalEdge> GetEdgesInInterval(TTime start, TTime end)
    {
        var interval = new TimeInterval(start, end);

        foreach (var edge in _edges.Values)
        {
            if (edge.Lifetime.Overlaps(interval))
                yield return edge;
        }
    }

    /// <summary>
    /// Gets all unique time points where the graph changes.
    /// </summary>
    public List<TTime> GetChangePoints()
    {
        var points = new HashSet<TTime>();

        foreach (var vertex in _vertices.Values)
        {
            points.Add(vertex.Lifetime.Start);
            if (vertex.Lifetime.End != null)
                points.Add(vertex.Lifetime.End);
        }

        foreach (var edge in _edges.Values)
        {
            points.Add(edge.Lifetime.Start);
            if (edge.Lifetime.End != null)
                points.Add(edge.Lifetime.End);
        }

        return points.OrderBy(p => p).ToList();
    }

    /// <summary>
    /// Removes a vertex and all its edges.
    /// </summary>
    public bool RemoveVertex(TVertex vertex)
    {
        if (!_vertices.TryGetValue(vertex, out var temporalVertex))
            return false;

        var edgesToRemove = _outEdges[vertex].Union(_inEdges[vertex]).ToList();
        foreach (var edgeId in edgesToRemove)
            RemoveEdge(edgeId);

        _outEdges.Remove(vertex);
        _inEdges.Remove(vertex);
        _vertices.Remove(vertex);

        return true;
    }

    /// <summary>
    /// Removes an edge.
    /// </summary>
    public bool RemoveEdge(TEdgeId edgeId)
    {
        if (!_edges.TryGetValue(edgeId, out var edge))
            return false;

        _outEdges[edge.Source].Remove(edgeId);
        _inEdges[edge.Target].Remove(edgeId);
        _edges.Remove(edgeId);

        return true;
    }

    /// <summary>
    /// Clears all vertices and edges.
    /// </summary>
    public void Clear()
    {
        _vertices.Clear();
        _edges.Clear();
        _outEdges.Clear();
        _inEdges.Clear();
    }
}
