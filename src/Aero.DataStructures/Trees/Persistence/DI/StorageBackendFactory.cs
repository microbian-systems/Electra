using System;
using Aero.DataStructures.Trees.Persistence.Storage;

namespace Aero.DataStructures.Trees.Persistence.DI;

public static class StorageBackendFactory
{
    public static IStorageBackend CreateInMemory(int pageSize = 4096)
    {
        return new MemoryStorageBackend(pageSize);
    }

    public static IStorageBackend CreateOnDisk(string path, int pageSize = 4096)
    {
        return new FileStorageBackend(path, pageSize);
    }

    public static IZeroCopyStorageBackend CreateMmap(
        string path,
        long capacityBytes,
        int pageSize = 4096)
    {
        return new MmapStorageBackend(path, capacityBytes, pageSize);
    }
}
