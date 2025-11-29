using System;
using System.Collections.Generic;
using System.Linq;

namespace Electra.DataStructures.Trees
{
    /// <summary>
    /// Represents a B+ Tree.
    /// </summary>
    /// <typeparam name="T">The type of the keys in the B+ Tree, must be comparable.</typeparam>
    public class BPlusTree<T> where T : IComparable<T>
    {
        public BPlusTreeNode<T> Root { get; private set; }
        private readonly int _degree;

        public BPlusTree(int degree)
        {
            _degree = degree;
            Root = new BPlusTreeNode<T>(degree) { IsLeaf = true };
        }

        public T Find(T key)
        {
            var leaf = FindLeaf(key);
            foreach (var k in leaf.Keys)
            {
                if (k.CompareTo(key) == 0)
                {
                    return k;
                }
            }
            return default(T);
        }

        public IEnumerable<T> FindRange(T startKey, T endKey)
        {
            var leaf = FindLeaf(startKey);
            while (leaf != null)
            {
                foreach (var key in leaf.Keys)
                {
                    if (key.CompareTo(startKey) >= 0 && key.CompareTo(endKey) <= 0)
                    {
                        yield return key;
                    }
                }

                if (leaf.Keys.Any() && leaf.Keys.Last().CompareTo(endKey) > 0)
                {
                    break;
                }
                
                leaf = leaf.Next;
            }
        }


        public void Insert(T key)
        {
            var leaf = FindLeaf(key);
            InsertIntoLeaf(leaf, key);

            if (leaf.Keys.Count <= _degree) return;
            
            var newLeaf = SplitLeaf(leaf);
            InsertIntoParent(leaf, newLeaf.Keys[0], newLeaf);
        }
        
        private void InsertIntoParent(BPlusTreeNode<T> left, T key, BPlusTreeNode<T> right)
        {
            var parent = GetParent(Root, left);

            if (parent == null)
            {
                Root = new BPlusTreeNode<T>(_degree);
                Root.Keys.Add(key);
                Root.Children.Add(left);
                Root.Children.Add(right);
                return;
            }

            int index = parent.Children.IndexOf(left);
            parent.Keys.Insert(index, key);
            parent.Children.Insert(index + 1, right);

            if (parent.Keys.Count > _degree)
            {
                // Split parent
                var newParent = new BPlusTreeNode<T>(_degree);
                int mid = parent.Keys.Count / 2;
                
                newParent.Keys.AddRange(parent.Keys.GetRange(mid + 1, parent.Keys.Count - (mid + 1)));
                T newParentKey = parent.Keys[mid];
                parent.Keys.RemoveRange(mid, parent.Keys.Count - mid);

                newParent.Children.AddRange(parent.Children.GetRange(mid + 1, parent.Children.Count - (mid + 1)));
                parent.Children.RemoveRange(mid + 1, parent.Children.Count - (mid + 1));
                
                InsertIntoParent(parent, newParentKey, newParent);
            }
        }

        public void Delete(T key)
        {
            var leaf = FindLeaf(key);
            RemoveFromLeaf(leaf, key);
        }

        private void RemoveFromLeaf(BPlusTreeNode<T> leaf, T key)
        {
            int index = leaf.Keys.IndexOf(key);
            if (index == -1) return;

            leaf.Keys.RemoveAt(index);
            
            if (leaf.Keys.Count >= _degree / 2 || leaf == Root) return;

            var parent = GetParent(Root, leaf);
            if(parent == null) return;
            int leafIndex = parent.Children.IndexOf(leaf);

            CoalesceOrRedistribute(leaf, parent, leafIndex);
        }

        private void CoalesceOrRedistribute(BPlusTreeNode<T> node, BPlusTreeNode<T> parent, int index)
        {
            // Simplified coalesce/redistribute. A full implementation is much more complex.
            // This version will merge with a sibling if the node is underfull.
            if (index > 0)
            {
                // Merge with left sibling
                var leftSibling = parent.Children[index - 1];
                if (leftSibling.Keys.Count + node.Keys.Count <= _degree)
                {
                    leftSibling.Keys.AddRange(node.Keys);
                    leftSibling.Next = node.Next;
                    parent.Keys.RemoveAt(index - 1);
                    parent.Children.RemoveAt(index);
                }
            }
            else
            {
                // Merge with right sibling
                var rightSibling = parent.Children[index + 1];
                if (rightSibling.Keys.Count + node.Keys.Count <= _degree)
                {
                    node.Keys.AddRange(rightSibling.Keys);
                    node.Next = rightSibling.Next;
                    parent.Keys.RemoveAt(index);
                    parent.Children.RemoveAt(index + 1);
                }
            }
        }
        
        private BPlusTreeNode<T> GetParent(BPlusTreeNode<T> current, BPlusTreeNode<T> child)
        {
            if (current == null || current.IsLeaf)
            {
                return null;
            }

            if (current.Children.Contains(child))
            {
                return current;
            }

            foreach (var c in current.Children)
            {
                var p = GetParent(c, child);
                if (p != null) return p;
            }
            return null;
        }

        private BPlusTreeNode<T> FindLeaf(T key)
        {
            var current = Root;
            while (!current.IsLeaf)
            {
                var i = 0;
                while (i < current.Keys.Count && key.CompareTo(current.Keys[i]) >= 0)
                {
                    i++;
                }
                current = current.Children[i];
            }
            return current;
        }
        
        private void InsertIntoLeaf(BPlusTreeNode<T> leaf, T key)
        {
            var i = 0;
            while (i < leaf.Keys.Count && key.CompareTo(leaf.Keys[i]) > 0)
            {
                i++;
            }
            leaf.Keys.Insert(i, key);
        }
        
        private BPlusTreeNode<T> SplitLeaf(BPlusTreeNode<T> leaf)
        {
            var newLeaf = new BPlusTreeNode<T>(_degree) { IsLeaf = true };
            var mid = leaf.Keys.Count / 2;
                
            newLeaf.Keys.AddRange(leaf.Keys.GetRange(mid, leaf.Keys.Count - mid));
            leaf.Keys.RemoveRange(mid, leaf.Keys.Count - mid);

            newLeaf.Next = leaf.Next;
            leaf.Next = newLeaf;
                
            return newLeaf;
        }
    }
}
