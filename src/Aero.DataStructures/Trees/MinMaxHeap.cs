using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a MinMaxHeap, a double-ended heap that supports efficient access to both
/// the minimum and maximum elements.
/// </summary>
/// <remarks>
/// In a MinMaxHeap:
/// - Even levels (0, 2, 4...) are "min levels" where nodes are smaller than their descendants
/// - Odd levels (1, 3, 5...) are "max levels" where nodes are larger than their descendants
/// This allows O(1) access to both min and max, and O(log n) insertion and deletion.
/// </remarks>
/// <typeparam name="T">The type of elements in the heap, must be comparable.</typeparam>
public class MinMaxHeap<T> : IHeap<T> where T : IComparable<T>
{
    private readonly List<T> _items = new();

    /// <summary>
    /// Gets the number of elements in the heap.
    /// </summary>
    public int Count => _items.Count;

    /// <summary>
    /// Inserts an item into the heap.
    /// </summary>
    /// <param name="item">The item to insert.</param>
    public void Insert(T item)
    {
        _items.Add(item);
        BubbleUp(Count - 1);
    }

    /// <summary>
    /// Returns the minimum element without removing it.
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T PeekMin()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");
        return _items[0];
    }

    /// <summary>
    /// Returns the maximum element without removing it.
    /// </summary>
    /// <returns>The maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T PeekMax()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");
        if (Count == 1) return _items[0];
        if (Count == 2) return _items[1];
        return _items[1].CompareTo(_items[2]) > 0 ? _items[1] : _items[2];
    }

    /// <summary>
    /// Returns the minimum element without removing it (IHeap implementation).
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T Peek()
    {
        return PeekMin();
    }

    /// <summary>
    /// Removes and returns the minimum element.
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T DeleteMin()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");
        return Delete(0);
    }

    /// <summary>
    /// Removes and returns the maximum element.
    /// </summary>
    /// <returns>The maximum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T DeleteMax()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");
        if (Count == 1) return Delete(0);
        
        int maxIndex;
        if (Count == 2)
        {
            maxIndex = 1;
        }
        else
        {
            maxIndex = _items[1].CompareTo(_items[2]) > 0 ? 1 : 2;
        }
        
        return Delete(maxIndex);
    }

    /// <summary>
    /// Removes and returns the minimum element (IHeap implementation).
    /// </summary>
    /// <returns>The minimum element.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the heap is empty.</exception>
    public T Extract()
    {
        return DeleteMin();
    }

    private T Delete(int index)
    {
        var item = _items[index];
        var lastItem = _items[Count - 1];
        _items.RemoveAt(Count - 1);
        
        if (Count > 0 && index < Count)
        {
            _items[index] = lastItem;
            TrickleDown(index);
        }
        
        return item;
    }

    private void BubbleUp(int index)
    {
        if (index == 0) return;
        
        int parentIndex = (index - 1) / 2;
        int level = GetLevel(index);
        
        if (IsMinLevel(level))
        {
            // On min level: check if greater than parent (which is on max level)
            if (_items[index].CompareTo(_items[parentIndex]) > 0)
            {
                Swap(index, parentIndex);
                BubbleUpMax(parentIndex);
            }
            else
            {
                BubbleUpMin(index);
            }
        }
        else
        {
            // On max level: check if less than parent (which is on min level)
            if (_items[index].CompareTo(_items[parentIndex]) < 0)
            {
                Swap(index, parentIndex);
                BubbleUpMin(parentIndex);
            }
            else
            {
                BubbleUpMax(index);
            }
        }
    }

    private void BubbleUpMin(int index)
    {
        if (index == 0) return;
        
        int grandparentIndex = (index - 3) / 4;
        if (grandparentIndex >= 0 && _items[index].CompareTo(_items[grandparentIndex]) < 0)
        {
            Swap(index, grandparentIndex);
            BubbleUpMin(grandparentIndex);
        }
    }

    private void BubbleUpMax(int index)
    {
        if (index < 3) return; // No grandparent at levels 0 or 1
        
        int grandparentIndex = (index - 3) / 4;
        if (grandparentIndex >= 0 && _items[index].CompareTo(_items[grandparentIndex]) > 0)
        {
            Swap(index, grandparentIndex);
            BubbleUpMax(grandparentIndex);
        }
    }

    private void TrickleDown(int index)
    {
        int level = GetLevel(index);
        
        if (IsMinLevel(level))
        {
            TrickleDownMin(index);
        }
        else
        {
            TrickleDownMax(index);
        }
    }

    private void TrickleDownMin(int index)
    {
        int minIndex = FindMinDescendant(index);
        
        if (minIndex == -1) return; // No descendants
        
        int parentOfMin = (minIndex - 1) / 2;
        
        if (_items[minIndex].CompareTo(_items[index]) < 0)
        {
            Swap(minIndex, index);
            
            // If we swapped with a grandchild, we might need to restore heap property
            if (parentOfMin != index && GetLevel(parentOfMin) > GetLevel(index))
            {
                if (_items[minIndex].CompareTo(_items[parentOfMin]) > 0)
                {
                    Swap(minIndex, parentOfMin);
                }
                TrickleDownMin(minIndex);
            }
        }
    }

    private void TrickleDownMax(int index)
    {
        int maxIndex = FindMaxDescendant(index);
        
        if (maxIndex == -1) return; // No descendants
        
        int parentOfMax = (maxIndex - 1) / 2;
        
        if (_items[maxIndex].CompareTo(_items[index]) > 0)
        {
            Swap(maxIndex, index);
            
            // If we swapped with a grandchild, we might need to restore heap property
            if (parentOfMax != index && GetLevel(parentOfMax) > GetLevel(index))
            {
                if (_items[maxIndex].CompareTo(_items[parentOfMax]) < 0)
                {
                    Swap(maxIndex, parentOfMax);
                }
                TrickleDownMax(maxIndex);
            }
        }
    }

    private int FindMinDescendant(int index)
    {
        int leftChild = 2 * index + 1;
        int rightChild = 2 * index + 2;
        int leftGrandchild1 = 2 * leftChild + 1;
        int leftGrandchild2 = 2 * leftChild + 2;
        int rightGrandchild1 = 2 * rightChild + 1;
        int rightGrandchild2 = 2 * rightChild + 2;
        
        int minIndex = -1;
        
        // Check children and grandchildren
        var candidates = new[] { leftChild, rightChild, leftGrandchild1, leftGrandchild2, rightGrandchild1, rightGrandchild2 };
        
        foreach (var candidate in candidates)
        {
            if (candidate < Count)
            {
                if (minIndex == -1 || _items[candidate].CompareTo(_items[minIndex]) < 0)
                {
                    minIndex = candidate;
                }
            }
        }
        
        return minIndex;
    }

    private int FindMaxDescendant(int index)
    {
        int leftChild = 2 * index + 1;
        int rightChild = 2 * index + 2;
        int leftGrandchild1 = 2 * leftChild + 1;
        int leftGrandchild2 = 2 * leftChild + 2;
        int rightGrandchild1 = 2 * rightChild + 1;
        int rightGrandchild2 = 2 * rightChild + 2;
        
        int maxIndex = -1;
        
        // Check children and grandchildren
        var candidates = new[] { leftChild, rightChild, leftGrandchild1, leftGrandchild2, rightGrandchild1, rightGrandchild2 };
        
        foreach (var candidate in candidates)
        {
            if (candidate < Count)
            {
                if (maxIndex == -1 || _items[candidate].CompareTo(_items[maxIndex]) > 0)
                {
                    maxIndex = candidate;
                }
            }
        }
        
        return maxIndex;
    }

    private int GetLevel(int index)
    {
        return (int)Math.Floor(Math.Log2(index + 1));
    }

    private bool IsMinLevel(int level)
    {
        return level % 2 == 0;
    }

    private void Swap(int i, int j)
    {
        (_items[i], _items[j]) = (_items[j], _items[i]);
    }
}
