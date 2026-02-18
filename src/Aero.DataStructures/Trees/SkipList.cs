using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a SkipList.
/// </summary>
/// <typeparam name="T">The type of the value stored in the node.</typeparam>
public class SkipListNode<T>
{
    /// <summary>
    /// Gets the value stored in the node.
    /// </summary>
    public T Value { get; }

    /// <summary>
    /// Gets the forward pointers at each level.
    /// </summary>
    public SkipListNode<T>[] Forward { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipListNode{T}"/> class.
    /// </summary>
    /// <param name="value">The value to store.</param>
    /// <param name="level">The level of the node.</param>
    public SkipListNode(T value, int level)
    {
        Value = value;
        Forward = new SkipListNode<T>[level];
    }
}

/// <summary>
/// Represents a node wrapper for ITreeNode interface compatibility.
/// </summary>
/// <typeparam name="T">The type of the value.</typeparam>
public class SkipListNodeWrapper<T> : ITreeNode<T>
{
    private readonly SkipListNode<T> _node;

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipListNodeWrapper{T}"/> class.
    /// </summary>
    /// <param name="node">The underlying SkipList node.</param>
    public SkipListNodeWrapper(SkipListNode<T> node)
    {
        _node = node;
    }

    /// <inheritdoc />
    public T Value
    {
        get => _node.Value;
        set => throw new NotSupportedException("Cannot modify SkipList node value directly");
    }

    /// <inheritdoc />
    public IEnumerable<ITreeNode<T>> Children
    {
        get
        {
            // SkipList nodes don't have children in the traditional tree sense
            yield break;
        }
    }
}

/// <summary>
/// Represents a SkipList, a probabilistic data structure that allows fast search within an ordered sequence of elements.
/// </summary>
/// <typeparam name="T">The type of the values in the list, must be comparable.</typeparam>
public class SkipList<T> : ITree<T> where T : IComparable<T>
{
    private const int MaxLevel = 32;
    private const double Probability = 0.5;
    private readonly Random _random = new();
    private readonly SkipListNode<T> _head;
    private int _level;

    /// <summary>
    /// Gets the number of elements in the SkipList.
    /// </summary>
    public int Count { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="SkipList{T}"/> class.
    /// </summary>
    public SkipList()
    {
        _head = new SkipListNode<T>(default, MaxLevel);
        _level = 1;
        Count = 0;
    }

    /// <inheritdoc />
    public void Insert(T value)
    {
        var update = new SkipListNode<T>[MaxLevel];
        var current = _head;

        // Find position to insert
        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Forward[i] != null && current.Forward[i].Value.CompareTo(value) < 0)
            {
                current = current.Forward[i];
            }
            update[i] = current;
        }

        current = current.Forward[0];

        // Don't insert duplicates
        if (current != null && current.Value.CompareTo(value) == 0)
        {
            return;
        }

        int newLevel = RandomLevel();
        if (newLevel > _level)
        {
            for (int i = _level; i < newLevel; i++)
            {
                update[i] = _head;
            }
            _level = newLevel;
        }

        var newNode = new SkipListNode<T>(value, newLevel);
        for (int i = 0; i < newLevel; i++)
        {
            newNode.Forward[i] = update[i].Forward[i];
            update[i].Forward[i] = newNode;
        }

        Count++;
    }

    /// <inheritdoc />
    public void Delete(T value)
    {
        var update = new SkipListNode<T>[MaxLevel];
        var current = _head;

        // Find the node and its predecessors
        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Forward[i] != null && current.Forward[i].Value.CompareTo(value) < 0)
            {
                current = current.Forward[i];
            }
            update[i] = current;
        }

        current = current.Forward[0];

        // Node not found
        if (current == null || current.Value.CompareTo(value) != 0)
        {
            return;
        }

        // Remove references to the node
        for (int i = 0; i < _level; i++)
        {
            if (update[i].Forward[i] != current)
            {
                break;
            }
            update[i].Forward[i] = current.Forward[i];
        }

        // Decrease level if necessary
        while (_level > 1 && _head.Forward[_level - 1] == null)
        {
            _level--;
        }

        Count--;
    }

    /// <inheritdoc />
    public ITreeNode<T> Find(T value)
    {
        var node = FindNode(value);
        return node != null ? new SkipListNodeWrapper<T>(node) : null;
    }

    /// <summary>
    /// Searches for a value and returns true if found.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>True if the value is found, otherwise false.</returns>
    public bool Contains(T value)
    {
        return FindNode(value) != null;
    }

    private SkipListNode<T> FindNode(T value)
    {
        var current = _head;

        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Forward[i] != null && current.Forward[i].Value.CompareTo(value) < 0)
            {
                current = current.Forward[i];
            }
        }

        current = current.Forward[0];

        if (current != null && current.Value.CompareTo(value) == 0)
        {
            return current;
        }

        return null;
    }

    /// <summary>
    /// Finds the smallest value greater than or equal to the specified value.
    /// </summary>
    /// <param name="value">The value to search for.</param>
    /// <returns>The smallest value greater than or equal to the specified value, or default if none exists.</returns>
    public T FindGreaterOrEqual(T value)
    {
        var current = _head;

        for (int i = _level - 1; i >= 0; i--)
        {
            while (current.Forward[i] != null && current.Forward[i].Value.CompareTo(value) < 0)
            {
                current = current.Forward[i];
            }
        }

        current = current.Forward[0];
        return current != null ? current.Value : default;
    }

    /// <summary>
    /// Returns all values in the SkipList in sorted order.
    /// </summary>
    /// <returns>An enumerable of all values in sorted order.</returns>
    public IEnumerable<T> GetAllValues()
    {
        var current = _head.Forward[0];
        while (current != null)
        {
            yield return current.Value;
            current = current.Forward[0];
        }
    }

    private int RandomLevel()
    {
        int level = 1;
        while (_random.NextDouble() < Probability && level < MaxLevel)
        {
            level++;
        }
        return level;
    }
}
