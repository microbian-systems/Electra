namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in an Interval Tree.
/// </summary>
public class IntervalTreeNode
{
    public Interval Interval { get; }
    public int Max { get; set; }
    public IntervalTreeNode Left { get; set; }
    public IntervalTreeNode Right { get; set; }
    public int Height { get; set; }

    public IntervalTreeNode(Interval interval)
    {
        Interval = interval;
        Max = interval.End;
        Height = 1;
    }
}