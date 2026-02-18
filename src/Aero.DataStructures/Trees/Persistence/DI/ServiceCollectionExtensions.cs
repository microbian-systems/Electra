using System;
using Aero.DataStructures.Trees.Persistence.Concurrency;
using Aero.DataStructures.Trees.Persistence.Interfaces;
using Aero.DataStructures.Trees.Persistence.Serialization;
using Aero.DataStructures.Trees.Persistence.Storage;
using Aero.DataStructures.Trees.Persistence.Trees;
using Aero.DataStructures.Trees.Persistence.Vacuum;
using Aero.DataStructures.Trees.Persistence.Wal;
using Microsoft.Extensions.DependencyInjection;

namespace Aero.DataStructures.Trees.Persistence.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddInMemoryMinHeap<T>(
        this IServiceCollection services)
        where T : unmanaged, IComparable<T>
    {
        services.AddSingleton<IStorageBackend>(sp => new MemoryStorageBackend());
        services.AddSingleton<INodeSerializer<T>, PrimitiveSerializer<T>>();
        services.AddSingleton<IPriorityTree<T>, PersistentMinHeap<T>>();
        return services;
    }

    public static IServiceCollection AddDiskBPlusTree<TKey, TValue>(
        this IServiceCollection services,
        string filePath)
        where TKey : unmanaged, IComparable<TKey>
        where TValue : unmanaged
    {
        services.AddSingleton<IStorageBackend>(sp => new FileStorageBackend(filePath));
        services.AddSingleton<IOrderedTree<TKey>, PersistentBPlusTree<TKey, TValue>>();
        services.AddSingleton<IVacuumable>(sp => (IVacuumable)sp.GetRequiredService<IOrderedTree<TKey>>());
        return services;
    }

    public static IServiceCollection AddMmapBPlusTree<TKey, TValue>(
        this IServiceCollection services,
        string filePath,
        long capacityBytes)
        where TKey : unmanaged, IComparable<TKey>
        where TValue : unmanaged
    {
        services.AddSingleton<IStorageBackend>(sp => new MmapStorageBackend(filePath, capacityBytes));
        services.AddSingleton<IOrderedTree<TKey>, PersistentBPlusTree<TKey, TValue>>();
        services.AddSingleton<IVacuumable>(sp => (IVacuumable)sp.GetRequiredService<IOrderedTree<TKey>>());
        return services;
    }

    public static IServiceCollection AddAutoVacuum(
        this IServiceCollection services,
        Action<AutoVacuumOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<AutoVacuumOptions>(_ => { });

        services.AddHostedService<AutoVacuumService>();
        return services;
    }

    public static IServiceCollection AddWal(
        this IServiceCollection services,
        string walPath,
        IsolationLevel isolation = IsolationLevel.ReadCommitted)
    {
        services.AddSingleton<IConcurrencyStrategy>(_ => isolation switch
        {
            IsolationLevel.SnapshotMVCC => new MvccConcurrencyStrategy(),
            IsolationLevel.OptimisticOCC => new OccConcurrencyStrategy(),
            _ => new NoIsolationStrategy()
        });

        services.AddSingleton<IWalStorageBackend>(sp =>
        {
            var inner = sp.GetRequiredService<IStorageBackend>();
            var concurrency = sp.GetRequiredService<IConcurrencyStrategy>();
            return WalStorageBackendFactory.CreateAsync(inner, walPath, concurrency)
                .GetAwaiter().GetResult();
        });

        services.AddSingleton<IStorageBackend>(sp => sp.GetRequiredService<IWalStorageBackend>());

        return services;
    }

    public static IServiceCollection AddCheckpointService(
        this IServiceCollection services,
        Action<CheckpointOptions>? configure = null)
    {
        if (configure is not null)
            services.Configure(configure);
        else
            services.Configure<CheckpointOptions>(_ => { });

        services.AddHostedService<CheckpointService>();
        return services;
    }
}
