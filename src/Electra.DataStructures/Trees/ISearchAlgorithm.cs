namespace Electra.DataStructures.Trees
{
    /// <summary>
    /// Defines a contract for search algorithms on trees.
    /// </summary>
    /// <typeparam name="T">The type of the values in the tree.</typeparam>
    public interface ISearchAlgorithm<T>
    {
        /// <summary>
        /// Performs a search on the tree and returns the traversal order.
        /// </summary>
        /// <returns>An enumerable of the tree nodes in traversal order.</returns>
        IEnumerable<ITreeNode<T>> Search();
    }
}
