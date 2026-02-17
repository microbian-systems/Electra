namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a Segment Tree for efficient range queries (sum in this case).
/// </summary>
public class SegmentTree
{
    private readonly int[] _data;
    public SegmentTreeNode Root { get; private set; }

    public SegmentTree(int[] data)
    {
        _data = data;
        Root = Build(0, _data.Length - 1);
    }

    private SegmentTreeNode Build(int start, int end)
    {
        if (start > end)
        {
            return null;
        }

        var node = new SegmentTreeNode { Start = start, End = end };
        if (start == end)
        {
            node.Sum = _data[start];
        }
        else
        {
            var mid = start + (end - start) / 2;
            node.Left = Build(start, mid);
            node.Right = Build(mid + 1, end);
            node.Sum = (node.Left?.Sum ?? 0) + (node.Right?.Sum ?? 0);
        }
        return node;
    }

    public void Update(int index, int value)
    {
        _data[index] = value;
        Update(Root, index, value);
    }

    private void Update(SegmentTreeNode node, int index, int value)
    {
        if (node.Start == node.End)
        {
            node.Sum = value;
            return;
        }

        var mid = node.Start + (node.End - node.Start) / 2;
        if (index <= mid)
        {
            Update(node.Left, index, value);
        }
        else
        {
            Update(node.Right, index, value);
        }
        node.Sum = (node.Left?.Sum ?? 0) + (node.Right?.Sum ?? 0);
    }

    public int Query(int start, int end)
    {
        return Query(Root, start, end);
    }

    private int Query(SegmentTreeNode node, int start, int end)
    {
        if (node == null || start > node.End || end < node.Start)
        {
            return 0;
        }

        if (start <= node.Start && end >= node.End)
        {
            return node.Sum;
        }

        var mid = node.Start + (node.End - node.Start) / 2;
        var leftSum = Query(node.Left, start, end);
        var rightSum = Query(node.Right, start, end);

        return leftSum + rightSum;
    }
}