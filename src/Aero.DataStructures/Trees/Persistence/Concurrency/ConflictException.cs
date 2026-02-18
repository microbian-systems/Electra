using System;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public sealed class ConflictException : Exception
{
    public long TransactionId { get; }
    public long ConflictingPageId { get; }

    public ConflictException(long transactionId, long conflictingPageId)
        : base($"Transaction {transactionId} conflicts on page {conflictingPageId}.")
    {
        TransactionId = transactionId;
        ConflictingPageId = conflictingPageId;
    }
}
