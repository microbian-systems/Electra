using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

public enum HeapType
{
    MinHeap,
    MaxHeap
}

/// <summary>
/// Represents a Binary Heap, a complete binary tree that satisfies the heap property.
/// </summary>
/// <typeparam name="T">The type of the values in the heap, must be comparable.</typeparam>
public class BinaryHeap<T> where T : IComparable<T>
{
    private readonly List<T> _items = new();
    private readonly HeapType _heapType;

    public BinaryHeap(HeapType heapType)
    {
        _heapType = heapType;
    }

    public int Count => _items.Count;

    public void Insert(T item)
    {
        _items.Add(item);
        HeapifyUp(Count - 1);
    }

    public T Peek()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");
        return _items[0];
    }

    public T Extract()
    {
        if (Count == 0) throw new InvalidOperationException("Heap is empty.");

        var item = _items[0];
        _items[0] = _items[Count - 1];
        _items.RemoveAt(Count - 1);

        HeapifyDown(0);

        return item;
    }

    private void HeapifyUp(int index)
    {
        if (index == 0) return;
        var parentIndex = (index - 1) / 2;

        if (Compare(_items[index], _items[parentIndex]))
        {
            Swap(index, parentIndex);
            HeapifyUp(parentIndex);
        }
    }

    private void HeapifyDown(int index)
    {
        var leftChild = 2 * index + 1;
        var rightChild = 2 * index + 2;
        var targetIndex = index;

        if (leftChild < Count && Compare(_items[leftChild], _items[targetIndex]))
        {
            targetIndex = leftChild;
        }
        if (rightChild < Count && Compare(_items[rightChild], _items[targetIndex]))
        {
            targetIndex = rightChild;
        }

        if (targetIndex != index)
        {
            Swap(index, targetIndex);
            HeapifyDown(targetIndex);
        }
    }

    private bool Compare(T item1, T item2)
    {
        var result = item1.CompareTo(item2);
        return _heapType == HeapType.MinHeap ? result < 0 : result > 0;
    }

    private void Swap(int index1, int index2)
    {
        (_items[index1], _items[index2]) = (_items[index2], _items[index1]);
    }
}