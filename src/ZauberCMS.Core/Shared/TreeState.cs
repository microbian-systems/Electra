using System.Collections.Concurrent;

namespace ZauberCMS.Core.Shared;

public class TreeState
{
    // ConcurrentDictionary to store the IDs of expanded nodes
    private readonly ConcurrentDictionary<string, byte> _expandedNodeIds = new();

    // Initialize the cache
    public readonly ConcurrentDictionary<string, bool> HasChildrenCache = new();

    public event Action<object?>? OnTreeValueChanged;

    private object? _treeValue;

    public object? TreeValue
    {
        get => _treeValue;
        set
        {
            if (_treeValue != value)
            {
                _treeValue = value;
                OnTreeValueChanged?.Invoke(_treeValue);
            }
        }
    }

    // Method to expand a node
    public void NodeExpanded(string nodeId)
    {
        _expandedNodeIds[nodeId] = 0;
    }

    // Method to collapse a node
    public void NodeCollapsed(string nodeId)
    {
        _expandedNodeIds.TryRemove(nodeId, out _);
    }

    // Method to check if a node is expanded
    public bool IsNodeExpanded(string nodeId)
    {
        return _expandedNodeIds.ContainsKey(nodeId);
    }

    // Method to clear all nodes
    public void ClearNodes()
    {
        _expandedNodeIds.Clear();
    }

    public bool HasChildren(string nodeId)
    {
        return HasChildrenCache.TryGetValue(nodeId, out var hasChildren) && hasChildren;
    }
    
    public void SetChildren(string nodeId, bool hasChildren)
    {
        HasChildrenCache[nodeId] = hasChildren;
    }

    public void ClearChildCache(string? contentId)
    {
        if (contentId != null)
        {
            HasChildrenCache.TryRemove(contentId, out _);
        }
        else
        {
            HasChildrenCache.Clear();
        }
    }
    
    public string? CurrentSection { get; set; }
}