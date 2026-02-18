using System;
using System.Runtime.InteropServices;

namespace Aero.DataStructures.Trees.Persistence.Indexes;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CompositeKey<TField, TId>(TField Field, TId Id)
    : IComparable<CompositeKey<TField, TId>>
    where TField : unmanaged, IComparable<TField>
    where TId : unmanaged, IComparable<TId>
{
    public int CompareTo(CompositeKey<TField, TId> other)
    {
        var cmp = Field.CompareTo(other.Field);
        return cmp != 0 ? cmp : Id.CompareTo(other.Id);
    }

    public static CompositeKey<TField, TId> RangeLo(TField field) =>
        new(field, default);

    public static CompositeKey<TField, Guid> RangeHi(TField field)
    {
        var maxId = GuidMax;
        return new CompositeKey<TField, Guid>(field, maxId);
    }

    public static CompositeKey<Guid, Guid> RangeHiGuid(Guid field) =>
        new(field, GuidMax);

    private static readonly Guid GuidMax = new(
        int.MaxValue, short.MaxValue, short.MaxValue,
        byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue,
        byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue);
}
