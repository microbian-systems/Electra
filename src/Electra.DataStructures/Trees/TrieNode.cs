using System.Collections.Generic;

namespace Electra.DataStructures.Trees;

/// <summary>
/// Represents a node in a Trie.
/// </summary>
public class TrieNode
{
    public Dictionary<char, TrieNode> Children { get; } = new();
    public bool IsEndOfWord { get; set; }
}