using System;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a point in 2D space.
/// </summary>
public class Point
{
    public double X { get; }
    public double Y { get; }

    public Point(double x, double y)
    {
        X = x;
        Y = y;
    }

    public double DistanceTo(Point other)
    {
        return Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2));
    }
}

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