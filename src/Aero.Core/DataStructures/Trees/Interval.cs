using System;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents an interval with a start and an end.
/// </summary>
public class Interval : IComparable<Interval>
{
    public int Start { get; }
    public int End { get; }

    public Interval(int start, int end)
    {
        Start = start;
        End = end;
    }

    public int CompareTo(Interval other)
    {
        return Start.CompareTo(other.Start);
    }

    public bool Overlaps(Interval other)
    {
        return Start < other.End && End > other.Start;
    }
}