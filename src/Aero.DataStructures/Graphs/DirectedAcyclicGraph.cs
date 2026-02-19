using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Graphs;

/// <summary>
/// Represents a Directed Acyclic Graph (DAG) - a directed graph with no directed cycles.
/// DAGs are fundamental in representing dependencies, scheduling, and causal relationships.
/// </summary>
/// <typeparam name="T">The type of vertices in the graph. Must be non-nullable.</typeparam>
/// <remarks>
/// <para>
/// A DAG has the important property that it can be linearly ordered through topological sorting.
/// This makes DAGs ideal for representing dependencies where one task must complete before another.
/// </para>
/// <para>
/// Key properties:
/// <list type="bullet">
/// <item><description>No directed cycles exist</description></item>
/// <item><description>Has at least one topological ordering</description></item>
/// <item><description>Has at least one source (vertex with in-degree 0)</description></item>
/// <item><description>Has at least one sink (vertex with out-degree 0)</description></item>
/// </list>
/// </para>
/// <para>
/// Common applications:
/// <list type="bullet">
/// <item><description>Build systems (make, msbuild, gradle)</description></item>
/// <item><description>Task scheduling and project management (PERT/CPM)</description></item>
/// <item><description>Data processing pipelines</description></item>
/// <item><description>Git commit history</description></item>
/// <item><description>Spreadsheet cell dependencies</description></item>
/// <item><description>Bayesian networks</description></item>
/// </list>
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var buildSystem = new DirectedAcyclicGraph&lt;string&gt;();
/// buildSystem.AddEdge("compile", "test");
/// buildSystem.AddEdge("compile", "package");
/// buildSystem.AddEdge("test", "deploy");
/// buildSystem.AddEdge("package", "deploy");
/// 
/// var buildOrder = buildSystem.TopologicalSort();
/// // Possible order: [compile, test, package, deploy]
/// // Or: [compile, package, test, deploy]
/// 
/// // This would throw an exception (would create a cycle):
/// // buildSystem.AddEdge("deploy", "compile");
/// </code>
/// </example>
public class DirectedAcyclicGraph<T> where T : notnull
{
    private readonly Dictionary<T, HashSet<T>> _outEdges = new();
    private readonly Dictionary<T, HashSet<T>> _inEdges = new();
    private int _edgeCount;

    /// <summary>
    /// Gets the number of vertices in the graph.
    /// </summary>
    public int VertexCount => _outEdges.Count;

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
        if (_outEdges.ContainsKey(vertex))
            return false;

        _outEdges[vertex] = new HashSet<T>();
        _inEdges[vertex] = new HashSet<T>();
        return true;
    }

    /// <summary>
    /// Adds a directed edge from source to destination.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when adding the edge would create a cycle.
    /// </exception>
    public void AddEdge(T source, T destination)
    {
        AddVertex(source);
        AddVertex(destination);

        if (WouldCreateCycle(source, destination))
            throw new InvalidOperationException(
                $"Adding edge from {source} to {destination} would create a cycle.");

        if (_outEdges[source].Add(destination))
        {
            _inEdges[destination].Add(source);
            _edgeCount++;
        }
    }

    /// <summary>
    /// Attempts to add an edge without throwing an exception if it would create a cycle.
    /// </summary>
    /// <param name="source">The source vertex.</param>
    /// <param name="destination">The destination vertex.</param>
    /// <returns>True if the edge was added; false if it would create a cycle or edge already exists.</returns>
    public bool TryAddEdge(T source, T destination)
    {
        AddVertex(source);
        AddVertex(destination);

        if (WouldCreateCycle(source, destination))
            return false;

        if (_outEdges[source].Add(destination))
        {
            _inEdges[destination].Add(source);
            _edgeCount++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks whether adding an edge from source to destination would create a cycle.
    /// </summary>
    public bool WouldCreateCycle(T source, T destination)
    {
        if (!_outEdges.ContainsKey(destination))
            return false;

        return CanReach(destination, source);
    }

    /// <summary>
    /// Determines whether there is a path from source to destination.
    /// </summary>
    public bool CanReach(T source, T destination)
    {
        if (!_outEdges.ContainsKey(source))
            return false;

        if (EqualityComparer<T>.Default.Equals(source, destination))
            return true;

        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        queue.Enqueue(source);
        visited.Add(source);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            foreach (var neighbor in _outEdges[current])
            {
                if (EqualityComparer<T>.Default.Equals(neighbor, destination))
                    return true;

                if (visited.Add(neighbor))
                    queue.Enqueue(neighbor);
            }
        }

        return false;
    }

    /// <summary>
    /// Removes a vertex and all its incident edges from the graph.
    /// </summary>
    public bool RemoveVertex(T vertex)
    {
        if (!_outEdges.TryGetValue(vertex, out var outNeighbors))
            return false;

        _edgeCount -= outNeighbors.Count + _inEdges[vertex].Count;

        foreach (var neighbor in outNeighbors)
            _inEdges[neighbor].Remove(vertex);

        foreach (var neighbor in _inEdges[vertex])
            _outEdges[neighbor].Remove(vertex);

        _outEdges.Remove(vertex);
        _inEdges.Remove(vertex);
        return true;
    }

    /// <summary>
    /// Removes an edge from the graph.
    /// </summary>
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
    /// Determines whether an edge exists from source to destination.
    /// </summary>
    public bool ContainsEdge(T source, T destination) =>
        _outEdges.TryGetValue(source, out var neighbors) && neighbors.Contains(destination);

    /// <summary>
    /// Gets all vertices that can be directly reached from the specified vertex.
    /// </summary>
    public IReadOnlyCollection<T> GetSuccessors(T vertex) =>
        _outEdges.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices that can directly reach the specified vertex.
    /// </summary>
    public IReadOnlyCollection<T> GetPredecessors(T vertex) =>
        _inEdges.TryGetValue(vertex, out var neighbors) ? neighbors : new HashSet<T>();

    /// <summary>
    /// Gets all vertices in the graph.
    /// </summary>
    public IReadOnlyCollection<T> GetVertices() => _outEdges.Keys;

    /// <summary>
    /// Gets the in-degree of a vertex.
    /// </summary>
    public int GetInDegree(T vertex) =>
        _inEdges.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets the out-degree of a vertex.
    /// </summary>
    public int GetOutDegree(T vertex) =>
        _outEdges.TryGetValue(vertex, out var neighbors) ? neighbors.Count : 0;

    /// <summary>
    /// Gets all source vertices (vertices with in-degree 0).
    /// </summary>
    public IEnumerable<T> GetSources() =>
        _outEdges.Keys.Where(v => _inEdges[v].Count == 0);

    /// <summary>
    /// Gets all sink vertices (vertices with out-degree 0).
    /// </summary>
    public IEnumerable<T> GetSinks() =>
        _outEdges.Keys.Where(v => _outEdges[v].Count == 0);

    /// <summary>
    /// Performs a topological sort of the DAG.
    /// </summary>
    /// <returns>A list of vertices in topological order.</returns>
    /// <remarks>
    /// Uses Kahn's algorithm. In a DAG, this will always return all vertices.
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

        return result;
    }

    /// <summary>
    /// Gets all possible topological orderings (for small graphs).
    /// </summary>
    /// <remarks>
    /// Warning: This can be expensive for large graphs as the number of 
    /// topological orderings can be exponential.
    /// </remarks>
    public IEnumerable<List<T>> GetAllTopologicalSorts()
    {
        var inDegree = new Dictionary<T, int>();
        foreach (var vertex in _outEdges.Keys)
            inDegree[vertex] = _inEdges[vertex].Count;

        return GenerateTopologicalSorts(new List<T>(), new HashSet<T>(), inDegree);
    }

    private IEnumerable<List<T>> GenerateTopologicalSorts(
        List<T> current, HashSet<T> visited, Dictionary<T, int> inDegree)
    {
        if (current.Count == _outEdges.Count)
        {
            yield return new List<T>(current);
            yield break;
        }

        foreach (var vertex in _outEdges.Keys)
        {
            if (visited.Contains(vertex) || inDegree[vertex] > 0)
                continue;

            current.Add(vertex);
            visited.Add(vertex);

            foreach (var neighbor in _outEdges[vertex])
                inDegree[neighbor]--;

            foreach (var sort in GenerateTopologicalSorts(current, visited, inDegree))
                yield return sort;

            current.RemoveAt(current.Count - 1);
            visited.Remove(vertex);

            foreach (var neighbor in _outEdges[vertex])
                inDegree[neighbor]++;
        }
    }

    /// <summary>
    /// Gets the longest path from any source to each vertex.
    /// </summary>
    public Dictionary<T, int> GetLongestPathLengths()
    {
        var longestPath = new Dictionary<T, int>();
        var sorted = TopologicalSort();

        foreach (var vertex in sorted)
        {
            longestPath[vertex] = 0;
            foreach (var predecessor in _inEdges[vertex])
            {
                if (longestPath.TryGetValue(predecessor, out var predPath))
                    longestPath[vertex] = Math.Max(longestPath[vertex], predPath + 1);
            }
        }

        return longestPath;
    }

    /// <summary>
    /// Finds the longest path in the DAG.
    /// </summary>
    public List<T> GetLongestPath()
    {
        var sorted = TopologicalSort();
        var longestPath = new Dictionary<T, int>();
        var previous = new Dictionary<T, T>();

        foreach (var vertex in sorted)
        {
            longestPath[vertex] = 0;
            foreach (var predecessor in _inEdges[vertex])
            {
                if (longestPath.TryGetValue(predecessor, out var predPath))
                {
                    if (predPath + 1 > longestPath[vertex])
                    {
                        longestPath[vertex] = predPath + 1;
                        previous[vertex] = predecessor;
                    }
                }
            }
        }

        var endVertex = longestPath.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key;
        if (EqualityComparer<T>.Default.Equals(endVertex, default))
            return new List<T>();

        var path = new List<T>();
        var current = endVertex;
        while (true)
        {
            path.Add(current);
            if (!previous.TryGetValue(current, out var prev))
                break;
            current = prev;
        }

        path.Reverse();
        return path;
    }

    /// <summary>
    /// Gets all ancestors of a vertex (all vertices that can reach this vertex).
    /// </summary>
    public HashSet<T> GetAncestors(T vertex)
    {
        var ancestors = new HashSet<T>();
        if (!_inEdges.ContainsKey(vertex))
            return ancestors;

        var queue = new Queue<T>();
        foreach (var predecessor in _inEdges[vertex])
            queue.Enqueue(predecessor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (ancestors.Add(current))
            {
                foreach (var predecessor in _inEdges[current])
                    queue.Enqueue(predecessor);
            }
        }

        return ancestors;
    }

    /// <summary>
    /// Gets all descendants of a vertex (all vertices reachable from this vertex).
    /// </summary>
    public HashSet<T> GetDescendants(T vertex)
    {
        var descendants = new HashSet<T>();
        if (!_outEdges.ContainsKey(vertex))
            return descendants;

        var queue = new Queue<T>();
        foreach (var successor in _outEdges[vertex])
            queue.Enqueue(successor);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            if (descendants.Add(current))
            {
                foreach (var successor in _outEdges[current])
                    queue.Enqueue(successor);
            }
        }

        return descendants;
    }

    /// <summary>
    /// Gets the transitive closure of the DAG.
    /// </summary>
    public DirectedAcyclicGraph<T> GetTransitiveClosure()
    {
        var closure = new DirectedAcyclicGraph<T>();
        var sorted = TopologicalSort();
        sorted.Reverse();
        var reachable = new Dictionary<T, HashSet<T>>();

        foreach (var vertex in sorted)
        {
            reachable[vertex] = new HashSet<T>();
            closure.AddVertex(vertex);
            
            foreach (var successor in _outEdges[vertex])
            {
                closure.AddEdge(vertex, successor);
                reachable[vertex].Add(successor);

                if (reachable.TryGetValue(successor, out var successorReachable))
                {
                    foreach (var indirect in successorReachable)
                    {
                        closure.TryAddEdge(vertex, indirect);
                        reachable[vertex].Add(indirect);
                    }
                }
            }
        }

        return closure;
    }

    /// <summary>
    /// Finds the Lowest Common Ancestors of two vertices.
    /// </summary>
    public HashSet<T> GetLowestCommonAncestors(T a, T b)
    {
        var ancestorsA = GetAncestors(a);
        ancestorsA.Add(a);

        var ancestorsB = GetAncestors(b);
        ancestorsB.Add(b);

        var commonAncestors = new HashSet<T>(ancestorsA);
        commonAncestors.IntersectWith(ancestorsB);

        if (commonAncestors.Count == 0)
            return new HashSet<T>();

        var lcas = new HashSet<T>();
        foreach (var ancestor in commonAncestors)
        {
            var isLca = true;
            foreach (var successor in _outEdges[ancestor])
            {
                if (commonAncestors.Contains(successor))
                {
                    isLca = false;
                    break;
                }
            }
            if (isLca)
                lcas.Add(ancestor);
        }

        return lcas;
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
