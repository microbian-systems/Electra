using System;
using System.Threading;

namespace Aero.DataStructures.Trees.Persistence.Concurrency;

public static class TransactionContext
{
    private static readonly AsyncLocal<long> _current = new();

    public static long Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }

    public static IDisposable Scope(long txnId)
    {
        var previous = _current.Value;
        _current.Value = txnId;
        return new RestoreScope(previous);
    }

    private sealed class RestoreScope : IDisposable
    {
        private readonly long _previous;
        private bool _disposed;

        public RestoreScope(long previous)
        {
            _previous = previous;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _current.Value = _previous;
                _disposed = true;
            }
        }
    }
}
