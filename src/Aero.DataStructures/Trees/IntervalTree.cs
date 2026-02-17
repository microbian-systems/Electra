using System;
using System.Collections.Generic;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents an Interval Tree for storing intervals and finding overlaps.
/// </summary>
public class IntervalTree
{
    public IntervalTreeNode Root { get; private set; }

    public void Insert(Interval interval)
    {
        Root = Insert(Root, interval);
    }

    private IntervalTreeNode Insert(IntervalTreeNode node, Interval interval)
    {
        if (node == null)
        {
            return new IntervalTreeNode(interval);
        }

        if (interval.Start < node.Interval.Start)
        {
            node.Left = Insert(node.Left, interval);
        }
        else
        {
            node.Right = Insert(node.Right, interval);
        }

        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
        node.Max = Math.Max(node.Max, interval.End);

        return Balance(node);
    }
        
    public IEnumerable<Interval> SearchOverlapping(Interval interval)
    {
        var result = new List<Interval>();
        SearchOverlapping(Root, interval, result);
        return result;
    }

    private void SearchOverlapping(IntervalTreeNode node, Interval interval, List<Interval> result)
    {
        if (node == null)
        {
            return;
        }

        if (node.Interval.Overlaps(interval))
        {
            result.Add(node.Interval);
        }

        if (node.Left != null && node.Left.Max >= interval.Start)
        {
            SearchOverlapping(node.Left, interval, result);
        }

        SearchOverlapping(node.Right, interval, result);
    }

    // AVL Tree balancing logic
    private int Height(IntervalTreeNode node) => node?.Height ?? 0;
    private int BalanceFactor(IntervalTreeNode node) => node == null ? 0 : Height(node.Left) - Height(node.Right);

    private IntervalTreeNode Balance(IntervalTreeNode node)
    {
        var balance = BalanceFactor(node);

        if (balance > 1) // Left heavy
        {
            if (BalanceFactor(node.Left) < 0)
            {
                node.Left = LeftRotate(node.Left);
            }
            return RightRotate(node);
        }
        if (balance < -1) // Right heavy
        {
            if (BalanceFactor(node.Right) > 0)
            {
                node.Right = RightRotate(node.Right);
            }
            return LeftRotate(node);
        }
        return node;
    }
        
    private IntervalTreeNode RightRotate(IntervalTreeNode y)
    {
        var x = y.Left;
        var T2 = x.Right;
        x.Right = y;
        y.Left = T2;
        y.Height = 1 + Math.Max(Height(y.Left), Height(y.Right));
        x.Height = 1 + Math.Max(Height(x.Left), Height(x.Right));
        y.Max = Math.Max(y.Interval.End, Math.Max(y.Left?.Max ?? int.MinValue, y.Right?.Max ?? int.MinValue));
        x.Max = Math.Max(x.Interval.End, Math.Max(x.Left?.Max ?? int.MinValue, x.Right?.Max ?? int.MinValue));
        return x;
    }

    private IntervalTreeNode LeftRotate(IntervalTreeNode x)
    {
        var y = x.Right;
        var T2 = y.Left;
        y.Left = x;
        x.Right = T2;
        x.Height = 1 + Math.Max(Height(x.Left), Height(x.Right));
        y.Height = 1 + Math.Max(Height(y.Left), Height(y.Right));
        x.Max = Math.Max(x.Interval.End, Math.Max(x.Left?.Max ?? int.MinValue, x.Right?.Max ?? int.MinValue));
        y.Max = Math.Max(y.Interval.End, Math.Max(y.Left?.Max ?? int.MinValue, y.Right?.Max ?? int.MinValue));
        return y;
    }
}