using System.Collections.Generic;

namespace Aero.DataStructures.Trees;



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