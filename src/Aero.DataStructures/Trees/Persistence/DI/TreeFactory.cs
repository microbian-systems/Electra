using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;
using Aero.DataStructures.Trees.Persistence.Trees;

namespace Aero.DataStructures.Trees.Persistence.DI;

public static class TreeFactory
{
    public static IPriorityTree<T> CreateMinHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new PrimitiveSerializer<T>();
        return new PersistentMinHeap<T>(storage, serializer);
    }

    public static IPriorityTree<T> CreateMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new PrimitiveSerializer<T>();
        return new PersistentMaxHeap<T>(storage, serializer);
    }

    public static IDoubleEndedPriorityTree<T> CreateMinMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new PrimitiveSerializer<T>();
        return new PersistentMinMaxHeap<T>(storage, serializer);
    }

    public static IOrderedTree<TKey> CreateBPlusTree<TKey, TValue>(IStorageBackend storage)
        where TKey : unmanaged, IComparable<TKey>
        where TValue : unmanaged
    {
        return new PersistentBPlusTree<TKey, TValue>(storage);
    }
}
