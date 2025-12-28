using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Abstract base class for tree search algorithms.
/// </summary>
/// <typeparam name="T">The type of the values in the tree.</typeparam>
public abstract class TreeSearch<T> : ISearchAlgorithm<T>
{
    protected readonly ITree<T> Tree;

    /// <summary>
    /// Initializes a new instance of the <see cref="TreeSearch{T}"/> class.
    /// </summary>
    /// <param name="tree">The tree to perform the search on.</param>
    protected TreeSearch(ITree<T> tree)
    {
        Tree = tree;
    }

    /// <inheritdoc />
    public abstract IEnumerable<ITreeNode<T>> Search();
}