using System;

namespace Aero.DataStructures.Trees.Persistence.Storage;

/// <summary>
/// Exception thrown when a requested page does not exist in the storage backend.
/// </summary>
public sealed class PageNotFoundException : Exception
{
    /// <summary>
    /// Gets the ID of the page that was not found.
    /// </summary>
    public long PageId { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="PageNotFoundException"/> class.
    /// </summary>
    /// <param name="pageId">The ID of the page that was not found.</param>
    public PageNotFoundException(long pageId)
        : base($"Page {pageId} does not exist.")
    {
        PageId = pageId;
    }
}

/// <summary>
/// Exception thrown when data written to a page does not match the expected page size.
/// </summary>
public sealed class PageSizeMismatchException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PageSizeMismatchException"/> class.
    /// </summary>
    /// <param name="expected">The expected page size in bytes.</param>
    /// <param name="actual">The actual data size in bytes.</param>
    public PageSizeMismatchException(int expected, int actual)
        : base($"Expected {expected} bytes, got {actual}.")
    {
    }
}
