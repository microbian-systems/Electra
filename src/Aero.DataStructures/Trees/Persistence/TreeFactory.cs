using System;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence;

/// <summary>
/// Factory for creating storage backends.
/// </summary>
public static class StorageBackendFactory
{
    /// <summary>
    /// Creates an in-memory storage backend.
    /// </summary>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    /// <returns>A new in-memory storage backend.</returns>
    public static IStorageBackend CreateInMemory(int pageSize = 4096)
    {
        return new MemoryStorageBackend(pageSize);
    }

    /// <summary>
    /// Creates a disk-based storage backend.
    /// </summary>
    /// <param name="path">The file path for storage.</param>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    /// <returns>A new file-based storage backend.</returns>
    public static IStorageBackend CreateOnDisk(string path, int pageSize = 4096)
    {
        return new FileStorageBackend(path, pageSize);
    }

    /// <summary>
    /// Creates a memory-mapped file storage backend for high-performance random access.
    /// </summary>
    /// <param name="path">The file path for storage.</param>
    /// <param name="initialCapacityBytes">Initial file capacity in bytes. Grows automatically if needed.</param>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    /// <returns>A new memory-mapped storage backend.</returns>
    public static IStorageBackend CreateMemoryMapped(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        return new MemoryMappedStorageBackend(path, initialCapacityBytes, pageSize);
    }

    /// <summary>
    /// Creates a high-performance memory-mapped storage backend using unsafe pointers.
    /// Provides the fastest possible page read/write performance.
    /// </summary>
    /// <param name="path">The file path for storage.</param>
    /// <param name="initialCapacityBytes">Initial file capacity in bytes. Grows automatically if needed.</param>
    /// <param name="pageSize">The page size in bytes. Default is 4096.</param>
    /// <returns>A new unsafe memory-mapped storage backend.</returns>
    public static IStorageBackend CreateMmapUnsafe(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        return new MmapStorageBackend(path, initialCapacityBytes, pageSize);
    }

    /// <summary>
    /// Creates a fully safe memory-mapped storage backend using InlineArray for stack-allocated buffers.
    /// Zero heap allocations for page operations. Maximum page size: 16KB.
    /// </summary>
    /// <param name="path">The file path for storage.</param>
    /// <param name="initialCapacityBytes">Initial file capacity in bytes. Grows automatically if needed.</param>
    /// <param name="pageSize">The page size in bytes. Default is 4096. Maximum is 16384.</param>
    /// <returns>A new safe memory-mapped storage backend.</returns>
    public static IStorageBackend CreateSafeMmap(string path, long initialCapacityBytes = 0, int pageSize = 4096)
    {
        return new SafeMmapStorageBackend(path, initialCapacityBytes, pageSize);
    }
}

/// <summary>
/// Factory for creating persistent tree instances.
/// </summary>
public static class TreeFactory
{
    /// <summary>
    /// Creates a persistent min heap.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <returns>A new persistent min heap.</returns>
    public static IPriorityTree<T> CreateMinHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new IntSerializer() as INodeSerializer<T> ?? throw new InvalidOperationException("Type not supported");
        return new Trees.PersistentMinHeap<T>(storage, serializer);
    }

    /// <summary>
    /// Creates a persistent min heap with a custom serializer.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>A new persistent min heap.</returns>
    public static IPriorityTree<T> CreateMinHeap<T>(IStorageBackend storage, INodeSerializer<T> serializer)
        where T : unmanaged, IComparable<T>
    {
        return new Trees.PersistentMinHeap<T>(storage, serializer);
    }

    /// <summary>
    /// Creates a persistent max heap.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <returns>A new persistent max heap.</returns>
    public static IPriorityTree<T> CreateMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new IntSerializer() as INodeSerializer<T> ?? throw new InvalidOperationException("Type not supported");
        return new Trees.PersistentMaxHeap<T>(storage, serializer);
    }

    /// <summary>
    /// Creates a persistent max heap with a custom serializer.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>A new persistent max heap.</returns>
    public static IPriorityTree<T> CreateMaxHeap<T>(IStorageBackend storage, INodeSerializer<T> serializer)
        where T : unmanaged, IComparable<T>
    {
        return new Trees.PersistentMaxHeap<T>(storage, serializer);
    }

    /// <summary>
    /// Creates a persistent min-max heap.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <returns>A new persistent min-max heap.</returns>
    public static IDoubleEndedPriorityTree<T> CreateMinMaxHeap<T>(IStorageBackend storage)
        where T : unmanaged, IComparable<T>
    {
        var serializer = new IntSerializer() as INodeSerializer<T> ?? throw new InvalidOperationException("Type not supported");
        return new Trees.PersistentMinMaxHeap<T>(storage, serializer);
    }

    /// <summary>
    /// Creates a persistent min-max heap with a custom serializer.
    /// </summary>
    /// <typeparam name="T">The element type. Must be unmanaged and comparable.</typeparam>
    /// <param name="storage">The storage backend.</param>
    /// <param name="serializer">The serializer to use.</param>
    /// <returns>A new persistent min-max heap.</returns>
    public static IDoubleEndedPriorityTree<T> CreateMinMaxHeap<T>(IStorageBackend storage, INodeSerializer<T> serializer)
        where T : unmanaged, IComparable<T>
    {
        return new Trees.PersistentMinMaxHeap<T>(storage, serializer);
    }
}
