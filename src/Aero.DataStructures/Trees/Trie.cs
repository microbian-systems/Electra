using System.Collections.Generic;
using System.Linq;

namespace Aero.DataStructures.Trees;

/// <summary>
/// Represents a Trie node that wraps a complete word for ITreeNode interface.
/// </summary>
public class TrieWordNode : ITreeNode<string>
{
    public string Value { get; set; }
    public IEnumerable<ITreeNode<string>> Children => Enumerable.Empty<ITreeNode<string>>();

    public TrieWordNode(string value)
    {
        Value = value;
    }
}

/// <summary>
/// Represents a Trie (Prefix Tree) for efficient string storage and retrieval.
/// </summary>
public class Trie : ITree<string>
{
    private readonly TrieNode _root = new();

    /// <summary>
    /// Inserts a word into the trie.
    /// </summary>
    /// <param name="word">The word to insert.</param>
    public void Insert(string word)
    {
        var current = _root;
        foreach (var c in word)
        {
            if (!current.Children.ContainsKey(c))
            {
                current.Children[c] = new TrieNode();
            }
            current = current.Children[c];
        }
        current.IsEndOfWord = true;
    }

    /// <summary>
    /// Searches for a word in the trie.
    /// </summary>
    /// <param name="word">The word to search for.</param>
    /// <returns>True if the word is found, otherwise false.</returns>
    public bool Search(string word)
    {
        var node = FindNode(word);
        return node != null && node.IsEndOfWord;
    }

    /// <summary>
    /// Checks if there is any word in the trie that starts with the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to search for.</param>
    /// <returns>True if there is any word with the prefix, otherwise false.</returns>
    public bool StartsWith(string prefix)
    {
        return FindNode(prefix) != null;
    }

    private TrieNode FindNode(string prefix)
    {
        var current = _root;
        foreach (var c in prefix)
        {
            if (!current.Children.TryGetValue(c, out current))
            {
                return null;
            }
        }
        return current;
    }

    /// <inheritdoc />
    public ITreeNode<string> Find(string word)
    {
        var node = FindNode(word);
        return node != null && node.IsEndOfWord ? new TrieWordNode(word) : null;
    }
        
    /// <summary>
    /// Deletes a word from the trie.
    /// </summary>
    /// <param name="word">The word to delete.</param>
    public void Delete(string word)
    {
        Delete(_root, word, 0);
    }

    private bool Delete(TrieNode current, string word, int index)
    {
        if (index == word.Length)
        {
            if (!current.IsEndOfWord)
            {
                return false;
            }
            current.IsEndOfWord = false;
            return current.Children.Count == 0;
        }

        var ch = word[index];
        if (!current.Children.TryGetValue(ch, out var node))
        {
            return false;
        }

        var shouldDeleteChild = Delete(node, word, index + 1);

        if (shouldDeleteChild)
        {
            current.Children.Remove(ch);
            return current.Children.Count == 0 && !current.IsEndOfWord;
        }

        return false;
    }
}