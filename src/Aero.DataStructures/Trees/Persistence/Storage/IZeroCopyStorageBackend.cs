using System;
using System.Runtime.CompilerServices;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// Optional capability interface that extends IStorageBackend with zero-copy span access.
/// Only backends that can provide direct memory access (like memory-mapped files) implement this.
/// </summary>
/// <remarks>
/// Trees can check for this interface at runtime and take the fast path when available.
/// This design maintains the IStorageBackend abstraction while allowing zero-copy access
/// as an optional capability that specific backends can advertise.
/// </remarks>
public interface IZeroCopyStorageBackend : IStorageBackend
{
    /// <summary>
    /// Returns a span directly into the underlying storage memory.
    /// The span is only valid for the lifetime of the backend.
    /// Caller must not hold the span across await boundaries.
    /// </summary>
    /// <param name="pageId">The ID of the page to access.</param>
    /// <returns>A span directly into the mapped memory region.</returns>
    /// <exception cref="PageNotFoundException">Thrown if the page ID does not exist.</exception>
    Span<byte> GetPageSpan(long pageId);

    /// <summary>
    /// Returns a reference to a typed struct directly in mapped memory.
    /// Writes through this reference go directly to mapped pages.
    /// The reference is only valid for the lifetime of the backend.
    /// </summary>
    /// <typeparam name="T">The unmanaged struct type to map.</typeparam>
    /// <param name="pageId">The ID of the page to access.</param>
    /// <returns>A reference to the struct directly in mapped memory.</returns>
    /// <exception cref="PageNotFoundException">Thrown if the page ID does not exist.</exception>
    ref T GetPageRef<T>(long pageId) where T : unmanaged;
}

/// <summary>
/// Extension methods for IZeroCopyStorageBackend to provide safe, composable access patterns.
/// </summary>
public static class ZeroCopyStorageBackendExtensions
{
    /// <summary>
    /// Gets a span and automatically validates the page exists.
    /// </summary>
    public static Span<byte> GetPageSpanSafe(this IZeroCopyStorageBackend backend, long pageId)
    {
        if (pageId < 0 || pageId >= backend.PageCount)
            throw new PageNotFoundException(pageId);
        
        return backend.GetPageSpan(pageId);
    }

    /// <summary>
    /// Gets a typed reference and automatically validates the page exists.
    /// </summary>
    public static ref T GetPageRefSafe<T>(this IZeroCopyStorageBackend backend, long pageId) where T : unmanaged
    {
        if (pageId < 0 || pageId >= backend.PageCount)
            throw new PageNotFoundException(pageId);
        
        return ref backend.GetPageRef<T>(pageId);
    }
}
