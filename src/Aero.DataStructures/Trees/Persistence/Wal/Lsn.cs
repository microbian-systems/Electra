using System;

namespace Aero.DataStructures.Trees.Persistence.Wal;

public readonly record struct Lsn(ulong Value) : IComparable<Lsn>
{
    public static readonly Lsn Zero = new(0);
    public static readonly Lsn MinValue = new(1);
    public static readonly Lsn MaxValue = new(ulong.MaxValue);

    public bool IsNull => Value == 0;

    public int CompareTo(Lsn other) => Value.CompareTo(other.Value);

    public static bool operator <(Lsn a, Lsn b) => a.Value < b.Value;
    public static bool operator >(Lsn a, Lsn b) => a.Value > b.Value;
    public static bool operator <=(Lsn a, Lsn b) => a.Value <= b.Value;
    public static bool operator >=(Lsn a, Lsn b) => a.Value >= b.Value;

    public Lsn Next() => new(Value + 1);

    public override string ToString() => $"LSN({Value})";
}
