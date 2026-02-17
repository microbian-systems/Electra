using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a KD-Tree for organizing points in a 2D space.
/// </summary>
public class KdTree
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