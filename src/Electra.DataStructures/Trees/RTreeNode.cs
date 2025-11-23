using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents the minimum bounding rectangle (MBR) for an R-Tree node.
/// </summary>
public class Mbr
{
    public Point Min { get; }
    public Point Max { get; }

    public Mbr(Point min, Point max)
    {
        Min = min;
        Max = max;
    }

    public double Area()
    {
        return (Max.X - Min.X) * (Max.Y - Min.Y);
    }

    public bool Intersects(Mbr other)
    {
        return Min.X <= other.Max.X && Max.X >= other.Min.X &&
               Min.Y <= other.Max.Y && Max.Y >= other.Min.Y;
    }
}

/// <summary>
/// Represents a node in an R-Tree.
/// </summary>
public class RTreeNode
{
    public Mbr Mbr { get; set; }
    public RTreeNode Parent { get; set; }
    public List<RTreeNode> Children { get; } = new();
    public List<Point> Points { get; } = new();

    public bool IsLeaf => Children.Count == 0;
}