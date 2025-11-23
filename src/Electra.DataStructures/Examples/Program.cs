using System;
using System.Collections.Generic;
using System.Linq;
using Electra.DataStructures.Trees;

namespace Electra.DataStructures.Examples;

public static class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("--- Tree Data Structures Examples ---");

        BinaryTreeExample();
        BinarySearchTreeExample();
        AvlTreeExample();
        RedBlackTreeExample();
        SplayTreeExample();
        TreapExample();
        BinaryHeapExample();
        BTreeExample();
        BPlusTreeExample();
        TrieExample();
        SegmentTreeExample();
        IntervalTreeExample();
        KdTreeExample();
        RTreeExample();
        ExpressionTreeExample();
    }

    private static void BinaryTreeExample()
    {
        Console.WriteLine("\n--- Binary Tree ---");
        var tree = new BinaryTree<int>();
        tree.Insert(1);
        tree.Insert(2);
        tree.Insert(3);
        tree.Insert(4);
        tree.Insert(5);

        Console.WriteLine("BFS Traversal:");
        var bfs = new BfsSearch<int>(tree);
        foreach (var node in bfs.Search())
        {
            Console.Write(node.Value + " ");
        }
        Console.WriteLine();

        Console.WriteLine("DFS Inorder Traversal:");
        var dfs = new DfsSearch<int>(tree, DfsTraversalOrder.Inorder);
        foreach (var node in dfs.Search())
        {
            Console.Write(node.Value + " ");
        }
        Console.WriteLine();
    }

    private static void BinarySearchTreeExample()
    {
        Console.WriteLine("\n--- Binary Search Tree ---");
        var bst = new BinarySearchTree<int>();
        bst.Insert(10);
        bst.Insert(5);
        bst.Insert(15);
        bst.Insert(3);
        bst.Insert(7);

        Console.WriteLine("Found 7: " + (bst.Find(7) != null));
        Console.WriteLine("Found 12: " + (bst.Find(12) != null));
    }

    private static void AvlTreeExample()
    {
        Console.WriteLine("\n--- AVL Tree ---");
        var avl = new AvlTree<int>();
        avl.Insert(10);
        avl.Insert(20);
        avl.Insert(30);
        avl.Insert(40);
        avl.Insert(50);
        avl.Insert(25);
            
        Console.WriteLine("Inorder traversal of AVL tree:");
        // Using a simple inorder traversal for demonstration
        PrintInorder(avl.Root);
        Console.WriteLine();
    }

    private static void RedBlackTreeExample()
    {
        Console.WriteLine("\n--- Red-Black Tree ---");
        var rbt = new RedBlackTree<int>();
        rbt.Insert(10);
        rbt.Insert(20);
        rbt.Insert(30);
        rbt.Insert(15);
            
        Console.WriteLine("Inorder traversal of Red-Black tree:");
        PrintInorder(rbt.Root);
        Console.WriteLine();
    }

    private static void SplayTreeExample()
    {
        Console.WriteLine("\n--- Splay Tree ---");
        var splay = new SplayTree<int>();
        splay.Insert(100);
        splay.Insert(50);
        splay.Insert(200);
        splay.Insert(40);
        splay.Insert(30);
        splay.Insert(20);
            
        splay.Find(20); // Splay 20 to the root
        Console.WriteLine("Root after splaying 20: " + splay.Root.Value);
    }
        
    private static void TreapExample()
    {
        Console.WriteLine("\n--- Treap ---");
        var treap = new Treap<int>();
        treap.Insert(50);
        treap.Insert(30);
        treap.Insert(70);
        treap.Insert(20);
        treap.Insert(40);
            
        Console.WriteLine("Inorder traversal of Treap:");
        PrintInorder(treap.Root);
        Console.WriteLine();
    }

    private static void BinaryHeapExample()
    {
        Console.WriteLine("\n--- Binary Heap (Min-Heap) ---");
        var heap = new BinaryHeap<int>(HeapType.MinHeap);
        heap.Insert(5);
        heap.Insert(3);
        heap.Insert(8);
        heap.Insert(1);

        Console.WriteLine("Extracted Min: " + heap.Extract());
        Console.WriteLine("Extracted Min: " + heap.Extract());
    }

    private static void BTreeExample()
    {
        Console.WriteLine("\n--- B-Tree ---");
        var btree = new BTree<int>(2);
        btree.Insert(10);
        btree.Insert(20);
        btree.Insert(5);
        btree.Insert(6);
        btree.Insert(12);

        Console.WriteLine("Found 6: " + btree.Find(6));
        Console.WriteLine("Found 15: " + btree.Find(15));
    }

    private static void BPlusTreeExample()
    {
        Console.WriteLine("\n--- B+ Tree ---");
        var bptree = new BPlusTree<int>(3);
        bptree.Insert(10);
        bptree.Insert(20);
        bptree.Insert(30);
        bptree.Insert(40);
        bptree.Insert(50);

        Console.WriteLine("Range 20-40:");
        foreach (var key in bptree.FindRange(20, 40))
        {
            Console.Write(key + " ");
        }
        Console.WriteLine();
    }

    private static void TrieExample()
    {
        Console.WriteLine("\n--- Trie ---");
        var trie = new Trie();
        trie.Insert("apple");
        trie.Insert("app");
        trie.Insert("banana");

        Console.WriteLine("Search 'app': " + trie.Search("app"));
        Console.WriteLine("Search 'apple': " + trie.Search("apple"));
        Console.WriteLine("Starts with 'ap': " + trie.StartsWith("ap"));
        Console.WriteLine("Search 'appl': " + trie.Search("appl"));
    }

    private static void SegmentTreeExample()
    {
        Console.WriteLine("\n--- Segment Tree ---");
        var data = new[] { 1, 3, 5, 7, 9, 11 };
        var segTree = new SegmentTree(data);

        Console.WriteLine("Sum of range [1, 3]: " + segTree.Query(1, 3));
        segTree.Update(2, 6);
        Console.WriteLine("Sum of range [1, 3] after update: " + segTree.Query(1, 3));
    }

    private static void IntervalTreeExample()
    {
        Console.WriteLine("\n--- Interval Tree ---");
        var intervalTree = new IntervalTree();
        intervalTree.Insert(new Interval(15, 20));
        intervalTree.Insert(new Interval(10, 30));
        intervalTree.Insert(new Interval(17, 19));
        intervalTree.Insert(new Interval(5, 20));

        var overlapping = intervalTree.SearchOverlapping(new Interval(12, 16));
        Console.WriteLine("Intervals overlapping with [12, 16]:");
        foreach (var interval in overlapping)
        {
            Console.WriteLine($"[{interval.Start}, {interval.End}]");
        }
    }

    private static void KdTreeExample()
    {
        Console.WriteLine("\n--- KD-Tree ---");
        var kdTree = new KdTree();
        kdTree.Insert(new Point(3, 6));
        kdTree.Insert(new Point(17, 15));
        kdTree.Insert(new Point(13, 15));
        kdTree.Insert(new Point(6, 12));

        var range = new Rect(5, 10, 15, 16);
        var inRange = kdTree.RangeSearch(range);

        Console.WriteLine("Points in range [5,10] to [15,16]:");
        foreach (var point in inRange)
        {
            Console.WriteLine($"({point.X}, {point.Y})");
        }

        var nearest = kdTree.NearestNeighbor(new Point(5, 11));
        Console.WriteLine($"Nearest neighbor to (5,11): ({nearest.X}, {nearest.Y})");
    }

    private static void RTreeExample()
    {
        Console.WriteLine("\n--- R-Tree ---");
        Console.WriteLine("R-Tree implementation is structural. See code for details.");
        var rtree = new RTree();
        try
        {
            rtree.Insert(new Point(1,1));
        }
        catch(NotImplementedException e)
        {
            Console.WriteLine("Caught expected exception: " + e.Message);
        }
    }

    private static void ExpressionTreeExample()
    {
        Console.WriteLine("\n--- Expression Tree ---");
        var expTree = new ExpressionTree();
        expTree.Build("3 4 + 2 *"); // (3 + 4) * 2
        Console.WriteLine("Result of '3 4 + 2 *' is: " + expTree.Evaluate());
    }
        
    // Helper to print inorder for BST-like trees
    private static void PrintInorder(BinaryTreeNode<int> node)
    {
        if (node == null) return;
        PrintInorder((BinaryTreeNode<int>)node.Left);
        Console.Write(node.Value + " ");
        PrintInorder((BinaryTreeNode<int>)node.Right);
    }
}