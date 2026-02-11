using System;

namespace EventSourcing.Library.Infrastructure
{
    /// <summary>
    /// Exception thrown when a concurrency conflict is detected during event persistence.
    /// Indicates that the aggregate has been modified by another process.
    /// </summary>
    public class ConcurrencyException : Exception
    {
        public string AggregateId { get; }
        public int ExpectedVersion { get; }
        public int ActualVersion { get; }

        public ConcurrencyException(string aggregateId, int expectedVersion, int actualVersion)
            : base($"Concurrency conflict for aggregate '{aggregateId}'. Expected version {expectedVersion}, but current version is {actualVersion}")
        {
            AggregateId = aggregateId;
            ExpectedVersion = expectedVersion;
            ActualVersion = actualVersion;
        }

        public ConcurrencyException(string message) : base(message)
        {
        }

        public ConcurrencyException(string message, Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}
