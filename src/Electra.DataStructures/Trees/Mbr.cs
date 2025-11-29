using System;

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

    public bool Contains(Point point)
    {
        return point.X >= Min.X && point.X <= Max.X &&
               point.Y >= Min.Y && point.Y <= Max.Y;
    }

    public double Enlargement(Point point)
    {
        double enlargedArea = (Math.Max(Max.X, point.X) - Math.Min(Min.X, point.X)) *
                              (Math.Max(Max.Y, point.Y) - Math.Min(Min.Y, point.Y));
        return enlargedArea - Area();
    }
}