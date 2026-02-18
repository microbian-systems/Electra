namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a node in a KD-Tree.
/// </summary>
public class KdTreeNode
{
    public Point Point { get; }
    public KdTreeNode Left { get; set; }
    public KdTreeNode Right { get; set; }

    public KdTreeNode(Point point)
    {
        Point = point;
    }
}