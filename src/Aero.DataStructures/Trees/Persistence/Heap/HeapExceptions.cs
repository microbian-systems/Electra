using System;

namespace Aero.DataStructures.Trees.Persistence.Heap;

public sealed class RecordDeletedException : Exception
{
    public HeapAddress Address { get; }

    public RecordDeletedException(HeapAddress address)
        : base($"Record at address ({address.PageId}, {address.SlotIndex}) has been deleted.")
    {
        Address = address;
    }
}

public sealed class DuplicateKeyException : Exception
{
    public Guid Id { get; }

    public DuplicateKeyException(Guid id)
        : base($"A document with ID '{id}' already exists.")
    {
        Id = id;
    }
}
