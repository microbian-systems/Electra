namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a Segment Tree.
/// </summary>
public class SegmentTreeNode
{
    public int Start { get; set; }
    public int End { get; set; }
    public int Sum { get; set; }
    public SegmentTreeNode Left { get; set; }
    public SegmentTreeNode Right { get; set; }
}