using System;
using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents an R-Tree for storing spatial data.
/// </summary>
public class RTree
{
    private readonly int _maxChildren;
    private readonly int _minChildren;
    public RTreeNode Root { get; private set; }
    private readonly List<RTreeNode> _deletedNodes = new();

    public RTree(int maxChildren = 4)
    {
        if (maxChildren < 2)
            throw new ArgumentException("maxChildren must be at least 2", nameof(maxChildren));
        _maxChildren = maxChildren;
        _minChildren = maxChildren / 2;
        Root = new RTreeNode(); // IsLeaf is computed as Children.Count == 0
    }

    public void Insert(Point point)
    {
        var leaf = ChooseLeaf(Root, point);
        leaf.Points.Add(point);

        if (leaf.Points.Count > _maxChildren)
        {
            var newLeaf = QuadraticSplit(leaf);
            AdjustTree(leaf, newLeaf);
        }
        else
        {
            AdjustTree(leaf, null);
        }
    }

    public void Delete(Point point)
    {
        var leaf = FindLeaf(Root, point);
        if (leaf == null) return;

        leaf.Points.Remove(point);
        CondenseTree(leaf);
    }

    private RTreeNode ChooseLeaf(RTreeNode node, Point point)
    {
        if (node.IsLeaf)
        {
            return node;
        }

        double minEnlargement = double.MaxValue;
        RTreeNode bestChild = null;

        foreach (var child in node.Children)
        {
            double enlargement = child.Mbr.Enlargement(point);
            if (enlargement < minEnlargement)
            {
                minEnlargement = enlargement;
                bestChild = child;
            }
            else if (Math.Abs(enlargement - minEnlargement) < double.Epsilon)
            {
                if (bestChild == null || child.Mbr.Area() < bestChild.Mbr.Area())
                {
                    bestChild = child;
                }
            }
        }
        return ChooseLeaf(bestChild, point);
    }

    private RTreeNode QuadraticSplit(RTreeNode node)
    {
        var newNode = new RTreeNode { Parent = node.Parent };
        
        // Quadratic split algorithm
        // Pick two seeds that would waste the most area if grouped together
        var (seed1, seed2) = PickSeeds(node);
        
        var group1 = new List<Point>(node.Points);
        var group2 = new List<Point>();
        
        if (node.IsLeaf)
        {
            group1.Remove(seed2);
            group2.Add(seed2);
            node.Points.Remove(seed2);
            
            // Distribute remaining entries
            while (group1.Count + group2.Count < _maxChildren + 1 && group1.Count > 0)
            {
                // Ensure minimum fill requirement
                if (group1.Count <= _minChildren)
                    break;
                if (group2.Count >= _maxChildren)
                    break;
                    
                // Pick next entry to assign
                var next = group1[0];
                group1.RemoveAt(0);
                node.Points.Remove(next);
                group2.Add(next);
            }
            
            newNode.Points.AddRange(group2);
        }
        else
        {
            // For non-leaf nodes, redistribute children
            var children = new List<RTreeNode>(node.Children);
            node.Children.Clear();
            
            int half = children.Count / 2;
            node.Children.AddRange(children.Take(half));
            newNode.Children.AddRange(children.Skip(half));
            
            foreach (var child in newNode.Children)
                child.Parent = newNode;
        }
        
        // Update MBRs
        node.Mbr = CalculateMbr(node);
        newNode.Mbr = CalculateMbr(newNode);
        
        return newNode;
    }

    private (Point, Point) PickSeeds(RTreeNode node)
    {
        // Simple linear pick: first and last points
        if (node.IsLeaf)
        {
            return (node.Points[0], node.Points[node.Points.Count - 1]);
        }
        else
        {
            return (node.Children[0].Mbr.Min, node.Children[node.Children.Count - 1].Mbr.Max);
        }
    }

    private void AdjustTree(RTreeNode node, RTreeNode newNode)
    {
        if (node == Root)
        {
            if (newNode != null)
            {
                var newRoot = new RTreeNode();
                newRoot.Children.Add(node);
                newRoot.Children.Add(newNode);
                node.Parent = newRoot;
                newNode.Parent = newRoot;
                Root = newRoot;
                Root.Mbr = CalculateMbr(Root);
            }
            else
            {
                Root.Mbr = CalculateMbr(Root);
            }
            return;
        }

        node.Parent.Mbr = CalculateMbr(node.Parent);
        if (newNode != null)
        {
            node.Parent.Children.Add(newNode);
            newNode.Parent = node.Parent;
            if (node.Parent.Children.Count > _maxChildren)
            {
                var newParent = QuadraticSplit(node.Parent);
                AdjustTree(node.Parent, newParent);
            }
            else
            {
                AdjustTree(node.Parent, null);
            }
        }
        else
        {
            AdjustTree(node.Parent, null);
        }
    }

    private RTreeNode FindLeaf(RTreeNode node, Point point)
    {
        if (node.IsLeaf)
        {
            return node.Points.Any(p => p.X == point.X && p.Y == point.Y) ? node : null;
        }

        foreach (var child in node.Children)
        {
            if (child.Mbr.Contains(point))
            {
                var result = FindLeaf(child, point);
                if (result != null) return result;
            }
        }
        return null;
    }
    
    private void CondenseTree(RTreeNode node)
    {
        if (node == Root)
        {
            if (node.Children.Count == 1 && !node.IsLeaf)
            {
                Root = node.Children[0];
                Root.Parent = null;
            }
            else if (node.Children.Count == 0 && !node.IsLeaf)
            {
                // Root is empty, reset
                Root = new RTreeNode();
            }
            Root.Mbr = CalculateMbr(Root);
            return;
        }

        var parent = node.Parent;
        int index = parent.Children.IndexOf(node);

        // If node has too few children, remove it and reinsert its entries
        if ((node.IsLeaf && node.Points.Count < _minChildren) ||
            (!node.IsLeaf && node.Children.Count < _minChildren))
        {
            parent.Children.RemoveAt(index);
            _deletedNodes.Add(node);
            
            // Reinsert orphaned entries
            if (node.IsLeaf)
            {
                foreach (var point in node.Points)
                {
                    Insert(point);
                }
            }
            else
            {
                foreach (var child in node.Children)
                {
                    // Reinsert all points from this subtree
                    ReinsertSubtree(child);
                }
            }
        }
        else
        {
            node.Mbr = CalculateMbr(node);
        }

        CondenseTree(parent);
    }

    private void ReinsertSubtree(RTreeNode node)
    {
        if (node.IsLeaf)
        {
            foreach (var point in node.Points)
            {
                Insert(point);
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                ReinsertSubtree(child);
            }
        }
    }
    
    private Mbr CalculateMbr(RTreeNode node)
    {
        if (node.IsLeaf)
        {
            if (node.Points.Count == 0)
                return new Mbr(new Point(0, 0), new Point(0, 0));
            
            double minX = node.Points.Min(p => p.X);
            double minY = node.Points.Min(p => p.Y);
            double maxX = node.Points.Max(p => p.X);
            double maxY = node.Points.Max(p => p.Y);
            
            return new Mbr(new Point(minX, minY), new Point(maxX, maxY));
        }
        else
        {
            if (node.Children.Count == 0)
                return new Mbr(new Point(0, 0), new Point(0, 0));
            
            double minX = node.Children.Min(c => c.Mbr.Min.X);
            double minY = node.Children.Min(c => c.Mbr.Min.Y);
            double maxX = node.Children.Max(c => c.Mbr.Max.X);
            double maxY = node.Children.Max(c => c.Mbr.Max.Y);
            
            return new Mbr(new Point(minX, minY), new Point(maxX, maxY));
        }
    }

    /// <summary>
    /// Searches for all points within a given rectangular area.
    /// </summary>
    /// <param name="searchArea">The area to search within.</param>
    /// <returns>A list of points found in the area.</returns>
    public IEnumerable<Point> Search(Mbr searchArea)
    {
        var result = new List<Point>();
        Search(Root, searchArea, result);
        return result;
    }

    private void Search(RTreeNode node, Mbr searchArea, List<Point> result)
    {
        if (node.IsLeaf)
        {
            foreach (var point in node.Points)
            {
                if (searchArea.Min.X <= point.X && point.X <= searchArea.Max.X &&
                    searchArea.Min.Y <= point.Y && point.Y <= searchArea.Max.Y)
                {
                    result.Add(point);
                }
            }
        }
        else
        {
            foreach (var child in node.Children)
            {
                if (child.Mbr.Intersects(searchArea))
                {
                    Search(child, searchArea, result);
                }
            }
        }
    }
}