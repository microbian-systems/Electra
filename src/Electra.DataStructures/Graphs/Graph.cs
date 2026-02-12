using System.Collections.Generic;
using System.Linq;

namespace Electra.DataStructures.Graphs;

public class Graph<T> where T : notnull
{
    // Using a Dictionary for Adjacency List
    private readonly Dictionary<T, Dictionary<T, int>> _adjacencyList = new();
    private readonly bool _isDirected;

    public Graph(bool isDirected = true)
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

    public void AddEdge(T source, T destination, int weight)
    {
        // Helper local function to reduce code duplication
        void AddDirectedEdge(T from, T to, int w)
        {
            if (!_adjacencyList.ContainsKey(from))
                _adjacencyList[from] = new Dictionary<T, int>();

            if (!_adjacencyList.ContainsKey(to))
                _adjacencyList[to] = new Dictionary<T, int>();

            _adjacencyList[from][to] = w;
        }

        AddDirectedEdge(source, destination, weight);

        if (!_isDirected)
        {
            AddDirectedEdge(destination, source, weight);
        }
    }

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

            // Removed .OrderBy for O(V+E) performance. 
            // Add it back if you specifically need alphabetical traversal.
            foreach (var neighbor in _adjacencyList[vertex].Keys)
            {
                if (visited.Add(neighbor)) // HashSet.Add returns false if already present
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

            // FIX: To visit A then B, we must Push B then A.
            // Therefore we sort Descending (Reverse Alphabetical).
            foreach (var neighbor in _adjacencyList[vertex].Keys.OrderByDescending(k => k))
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
        var distances = new Dictionary<T, int>();
        var priorityQueue = new PriorityQueue<(T vertex, int distance), int>();
        var visited = new HashSet<T>();

        foreach (var vertex in _adjacencyList.Keys)
        {
            distances[vertex] = int.MaxValue;
        }

        distances[startVertex] = 0;
        priorityQueue.Enqueue((startVertex, 0), 0);

        while (priorityQueue.Count > 0)
        {
            var (currentVertex, currentDistance) = priorityQueue.Dequeue();

            if (visited.Contains(currentVertex))
            {
                continue;
            }

            visited.Add(currentVertex);

            if (currentDistance > distances[currentVertex])
            {
                continue;
            }

            foreach (var neighbor in _adjacencyList[currentVertex])
            {
                var newDistance = currentDistance + neighbor.Value;

                if (newDistance < distances[neighbor.Key])
                {
                    distances[neighbor.Key] = newDistance;
                    priorityQueue.Enqueue((neighbor.Key, newDistance), newDistance);
                }
            }
        }

        return distances;
    }

    public int[,] GetAdjacencyMatrix()
    {
        var vertices = _adjacencyList.Keys.OrderBy(k => k).ToList();
        var vertexMap = new Dictionary<T, int>();
        for (int i = 0; i < vertices.Count; i++)
        {
            vertexMap[vertices[i]] = i;
        }

        var matrix = new int[vertices.Count, vertices.Count];
        for (int i = 0; i < vertices.Count; i++)
        {
            for (int j = 0; j < vertices.Count; j++)
            {
                var source = vertices[i];
                var destination = vertices[j];
                if (_adjacencyList.ContainsKey(source) && _adjacencyList[source].ContainsKey(destination))
                {
                    matrix[i, j] = _adjacencyList[source][destination];
                }
            }
        }

        return matrix;
    }
}

