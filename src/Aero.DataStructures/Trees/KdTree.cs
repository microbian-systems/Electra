using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a KD-Tree node wrapper for ITreeNode interface.
/// </summary>
public class KdTreeNodeWrapper : ITreeNode<Point>
{
    private readonly KdTreeNode _node;

    public KdTreeNodeWrapper(KdTreeNode node)
    {
        _node = node;
    }

    public Point Value
    {
        get => _node.Point;
        set => throw new NotSupportedException("Cannot modify KD-Tree node value directly");
    }

    public IEnumerable<ITreeNode<Point>> Children
    {
        get
        {
            if (_node.Left != null)
                yield return new KdTreeNodeWrapper(_node.Left);
            if (_node.Right != null)
                yield return new KdTreeNodeWrapper(_node.Right);
        }
    }
}

/// <summary>
/// Represents a KD-Tree for organizing points in a 2D space.
/// </summary>
public class KdTree : ITree<Point>
{
    public KdTreeNode Root { get; private set; }

    public void Insert(Point point)
    {
        Root = Insert(Root, point, 0);
    }

    private KdTreeNode Insert(KdTreeNode node, Point point, int depth)
    {
        if (node == null)
        {
            return new KdTreeNode(point);
        }

        var axis = depth % 2;
        if ((axis == 0 ? point.X : point.Y) < (axis == 0 ? node.Point.X : node.Point.Y))
        {
            node.Left = Insert(node.Left, point, depth + 1);
        }
        else
        {
            node.Right = Insert(node.Right, point, depth + 1);
        }

        return node;
    }

    public IEnumerable<Point> RangeSearch(Rect range)
    {
        var result = new List<Point>();
        RangeSearch(Root, range, 0, result);
        return result;
    }

    private void RangeSearch(KdTreeNode node, Rect range, int depth, List<Point> result)
    {
        if (node == null) return;

        if (range.Contains(node.Point))
        {
            result.Add(node.Point);
        }

        var axis = depth % 2;
        var pointCoord = axis == 0 ? node.Point.X : node.Point.Y;
        var rangeMin = axis == 0 ? range.XMin : range.YMin;
        var rangeMax = axis == 0 ? range.XMax : range.YMax;

        if (pointCoord >= rangeMin)
        {
            RangeSearch(node.Left, range, depth + 1, result);
        }
        if (pointCoord <= rangeMax)
        {
            RangeSearch(node.Right, range, depth + 1, result);
        }
    }
        
    public Point NearestNeighbor(Point target)
    {
        // Simplified nearest neighbor search, not fully optimized
        return NearestNeighbor(Root, target, 0, Root)?.Point;
    }

    private KdTreeNode NearestNeighbor(KdTreeNode node, Point target, int depth, KdTreeNode best)
    {
        if (node == null) return best;

        if (target.DistanceTo(node.Point) < target.DistanceTo(best.Point))
        {
            best = node;
        }

        var axis = depth % 2;
        var dx = (axis == 0 ? target.X : target.Y) - (axis == 0 ? node.Point.X : node.Point.Y);
        var nearer = dx < 0 ? node.Left : node.Right;
        var further = dx < 0 ? node.Right : node.Left;

        best = NearestNeighbor(nearer, target, depth + 1, best);

        if (dx * dx < target.DistanceTo(best.Point))
        {
            best = NearestNeighbor(further, target, depth + 1, best);
        }

        return best;
    }

    /// <inheritdoc />
    public void Delete(Point point)
    {
        Root = Delete(Root, point, 0);
    }

    private KdTreeNode Delete(KdTreeNode node, Point point, int depth)
    {
        if (node == null) return null;

        var axis = depth % 2;
        var pointCoord = axis == 0 ? point.X : point.Y;
        var nodeCoord = axis == 0 ? node.Point.X : node.Point.Y;

        if (pointCoord == nodeCoord && node.Point.X == point.X && node.Point.Y == point.Y)
        {
            // Found the node to delete
            if (node.Right != null)
            {
                // Find minimum in right subtree
                var minNode = FindMin(node.Right, axis, depth + 1);
                node = new KdTreeNode(minNode.Point) { Left = node.Left, Right = Delete(node.Right, minNode.Point, depth + 1) };
            }
            else if (node.Left != null)
            {
                // Find minimum in left subtree and move to right
                var minNode = FindMin(node.Left, axis, depth + 1);
                node = new KdTreeNode(minNode.Point) { Left = null, Right = Delete(node.Left, minNode.Point, depth + 1) };
            }
            else
            {
                return null; // Leaf node
            }
        }
        else if (pointCoord < nodeCoord)
        {
            node.Left = Delete(node.Left, point, depth + 1);
        }
        else
        {
            node.Right = Delete(node.Right, point, depth + 1);
        }

        return node;
    }

    private KdTreeNode FindMin(KdTreeNode node, int targetAxis, int depth)
    {
        if (node == null) return null;

        var currentAxis = depth % 2;
        if (currentAxis == targetAxis)
        {
            if (node.Left == null) return node;
            return FindMin(node.Left, targetAxis, depth + 1);
        }

        // Need to check both subtrees
        var leftMin = FindMin(node.Left, targetAxis, depth + 1);
        var rightMin = FindMin(node.Right, targetAxis, depth + 1);

        var minNode = node;
        var minCoord = targetAxis == 0 ? node.Point.X : node.Point.Y;

        if (leftMin != null)
        {
            var leftCoord = targetAxis == 0 ? leftMin.Point.X : leftMin.Point.Y;
            if (leftCoord < minCoord)
            {
                minNode = leftMin;
                minCoord = leftCoord;
            }
        }

        if (rightMin != null)
        {
            var rightCoord = targetAxis == 0 ? rightMin.Point.X : rightMin.Point.Y;
            if (rightCoord < minCoord)
            {
                minNode = rightMin;
            }
        }

        return minNode;
    }

    /// <inheritdoc />
    public ITreeNode<Point> Find(Point point)
    {
        var node = FindNode(Root, point, 0);
        return node != null ? new KdTreeNodeWrapper(node) : null;
    }

    private KdTreeNode FindNode(KdTreeNode node, Point point, int depth)
    {
        if (node == null) return null;

        if (node.Point.X == point.X && node.Point.Y == point.Y)
            return node;

        var axis = depth % 2;
        if ((axis == 0 ? point.X : point.Y) < (axis == 0 ? node.Point.X : node.Point.Y))
        {
            return FindNode(node.Left, point, depth + 1);
        }
        else
        {
            return FindNode(node.Right, point, depth + 1);
        }
    }
}

/// <summary>
/// Represents a rectangle for range queries.
/// </summary>
public class Rect
{
    public double XMin { get; }
    public double YMin { get; }
    public double XMax { get; }
    public double YMax { get; }

    public Rect(double xmin, double ymin, double xmax, double ymax)
    {
        XMin = xmin;
        YMin = ymin;
        XMax = xmax;
        YMax = ymax;
    }

    public bool Contains(Point p)
    {
        return p.X >= XMin && p.X <= XMax && p.Y >= YMin && p.Y <= YMax;
    }
}