namespace Aero.DataStructures.Graphs;

public class AdjacencyListGraph<T> where T : notnull
{
    private readonly Dictionary<T, Dictionary<T, int>> _adjacencyList = new();
    private readonly bool _isDirected;
    
    public const int NoEdge = int.MinValue;
    public const int Unreachable = int.MaxValue;

    public int VertexCount => _adjacencyList.Count;
    public int EdgeCount { get; private set; }
    public bool IsDirected => _isDirected;

    public AdjacencyListGraph(bool isDirected = true)
    {
        _isDirected = isDirected;
    }

    public void AddVertex(T vertex)
    {
        if (!_adjacencyList.ContainsKey(vertex))
        {
            _adjacencyList[vertex] = new Dictionary<T, int>();
        }
    }

    public bool RemoveVertex(T vertex)
    {
        if (!_adjacencyList.ContainsKey(vertex))
            return false;

        var edgeCount = _adjacencyList[vertex].Count;
        _adjacencyList.Remove(vertex);

        foreach (var edges in _adjacencyList.Values)
        {
            if (edges.Remove(vertex))
                edgeCount++;
        }

        EdgeCount -= _isDirected ? edgeCount : edgeCount / 2;
        return true;
    }

    public void AddEdge(T source, T destination, int weight)
    {
        AddVertex(source);
        AddVertex(destination);

        if (!_adjacencyList[source].ContainsKey(destination))
            EdgeCount++;

        _adjacencyList[source][destination] = weight;

        if (!_isDirected)
        {
            _adjacencyList[destination][source] = weight;
        }
    }

    public bool RemoveEdge(T source, T destination)
    {
        if (!_adjacencyList.TryGetValue(source, out var edges))
            return false;

        if (!edges.Remove(destination))
            return false;

        EdgeCount--;

        if (!_isDirected && _adjacencyList.TryGetValue(destination, out var reverseEdges))
        {
            reverseEdges.Remove(source);
        }

        return true;
    }

    public bool ContainsVertex(T vertex) => _adjacencyList.ContainsKey(vertex);

    public bool ContainsEdge(T source, T destination) =>
        _adjacencyList.TryGetValue(source, out var edges) && edges.ContainsKey(destination);

    public bool TryGetWeight(T source, T destination, out int weight)
    {
        weight = default;
        if (_adjacencyList.TryGetValue(source, out var edges) && edges.TryGetValue(destination, out weight))
            return true;
        return false;
    }

    public IReadOnlyCollection<T> GetVertices() => _adjacencyList.Keys;

    public IReadOnlyDictionary<T, int> GetNeighbors(T vertex) =>
        _adjacencyList.TryGetValue(vertex, out var edges) ? edges : new Dictionary<T, int>();

    public List<T> Bfs(T startVertex)
    {
        if (!_adjacencyList.ContainsKey(startVertex)) return new List<T>();

        var visited = new HashSet<T>();
        var queue = new Queue<T>();
        var result = new List<T>();

        queue.Enqueue(startVertex);
        visited.Add(startVertex);

        while (queue.Count > 0)
        {
            var vertex = queue.Dequeue();
            result.Add(vertex);

            foreach (var neighbor in _adjacencyList[vertex].Keys)
            {
                if (visited.Add(neighbor))
                {
                    queue.Enqueue(neighbor);
                }
            }
        }

        return result;
    }

    public List<T> Dfs(T startVertex)
    {
        if (!_adjacencyList.ContainsKey(startVertex)) return new List<T>();

        var visited = new HashSet<T>();
        var stack = new Stack<T>();
        var result = new List<T>();

        stack.Push(startVertex);

        while (stack.Count > 0)
        {
            var vertex = stack.Pop();

            if (!visited.Add(vertex)) continue;

            result.Add(vertex);

            foreach (var neighbor in _adjacencyList[vertex].Keys)
            {
                if (!visited.Contains(neighbor))
                {
                    stack.Push(neighbor);
                }
            }
        }

        return result;
    }

    public Dictionary<T, int> Dijkstra(T startVertex)
    {
        if (!_adjacencyList.ContainsKey(startVertex))
            throw new ArgumentException($"Vertex '{startVertex}' not found in graph.", nameof(startVertex));

        var distances = new Dictionary<T, int>();
        var priorityQueue = new PriorityQueue<T, int>();
        var visited = new HashSet<T>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            distances[vertex] = Unreachable;
        }

        distances[startVertex] = 0;
        priorityQueue.Enqueue(startVertex, 0);

        while (priorityQueue.Count > 0)
        {
            var currentVertex = priorityQueue.Dequeue();

            if (!visited.Add(currentVertex))
                continue;

            var currentDistance = distances[currentVertex];

            foreach (var (neighbor, weight) in _adjacencyList[currentVertex])
            {
                if (currentDistance > Unreachable - weight)
                    continue;

                var newDistance = currentDistance + weight;

                if (newDistance < distances[neighbor])
                {
                    distances[neighbor] = newDistance;
                    priorityQueue.Enqueue(neighbor, newDistance);
                }
            }
        }

        return distances;
    }

    public int?[,] GetAdjacencyMatrix()
    {
        var vertices = _adjacencyList.Keys.OrderBy(k => k).ToList();
        var vertexMap = new Dictionary<T, int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            vertexMap[vertices[i]] = i;
        }

        var matrix = new int?[vertices.Count, vertices.Count];

        for (int i = 0; i < vertices.Count; i++)
        {
            var source = vertices[i];
            foreach (var (destination, weight) in _adjacencyList[source])
            {
                if (vertexMap.TryGetValue(destination, out var j))
                {
                    matrix[i, j] = weight;
                }
            }
        }

        return matrix;
    }

    public void Clear()
    {
        _adjacencyList.Clear();
        EdgeCount = 0;
    }
}

[Obsolete("Use AdjacencyListGraph<T> instead. This class will be removed in a future version.")]
public class Graph<T> : AdjacencyListGraph<T> where T : notnull
{
    public Graph(bool isDirected = true) : base(isDirected) { }
}

