using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;

namespace Electra.DataStructures.Tests
{
    public class TrieTests
    {
        [Fact]
        public void Insert_And_Search_Success()
        {
            // Arrange
            var trie = new Trie();
            trie.Insert("apple");
            trie.Insert("app");
            
            // Assert
            trie.Search("app").Should().BeTrue();
            trie.Search("apple").Should().BeTrue();
            trie.Search("appl").Should().BeFalse();
        }

        [Fact]
        public void StartsWith_Success()
        {
            // Arrange
            var trie = new Trie();
            trie.Insert("apple");
            trie.Insert("app");
            trie.Insert("banana");
            
            // Assert
            trie.StartsWith("ap").Should().BeTrue();
            trie.StartsWith("ban").Should().BeTrue();
            trie.StartsWith("can").Should().BeFalse();
        }

        [Fact]
        public void Delete_And_Search_Fails()
        {
            // Arrange
            var trie = new Trie();
            trie.Insert("apple");
            trie.Insert("app");

            // Act
            trie.Delete("apple");

            // Assert
            trie.Search("apple").Should().BeFalse();
            trie.Search("app").Should().BeTrue();
        }
    }
}
