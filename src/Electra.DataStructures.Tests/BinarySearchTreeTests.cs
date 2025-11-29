using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using Bogus;
using System;
using System.Linq;

namespace Electra.DataStructures.Tests
{
    public class BinarySearchTreeTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void Insert_ShouldAddItemsCorrectly()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            var values = _faker.Random.Digits(10).Distinct().ToList();

            // Act
            foreach (var value in values)
            {
                bst.Insert(value);
            }

            // Assert
            foreach (var value in values)
            {
                bst.Find(value).Should().NotBeNull();
                bst.Find(value).Value.Should().Be(value);
            }
        }

        [Fact]
        public void Find_ShouldReturnNull_WhenItemDoesNotExist()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(15);

            // Act
            var result = bst.Find(999);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void Delete_ShouldRemoveLeafNode()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5); // Leaf
            bst.Insert(15);

            // Act
            bst.Delete(5);

            // Assert
            bst.Find(5).Should().BeNull();
            bst.Root.Left.Should().BeNull();
            bst.Root.Value.Should().Be(10);
            bst.Root.Right.Value.Should().Be(15);
        }

        [Fact]
        public void Delete_ShouldRemoveNodeWithOneChild_Left()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(3); // 5 has left child 3

            // Act
            bst.Delete(5);

            // Assert
            bst.Find(5).Should().BeNull();
            bst.Find(3).Should().NotBeNull();
            bst.Root.Left.Value.Should().Be(3);
        }

        [Fact]
        public void Delete_ShouldRemoveNodeWithOneChild_Right()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(7); // 5 has right child 7

            // Act
            bst.Delete(5);

            // Assert
            bst.Find(5).Should().BeNull();
            bst.Find(7).Should().NotBeNull();
            bst.Root.Left.Value.Should().Be(7);
        }

        [Fact]
        public void Delete_ShouldRemoveNodeWithTwoChildren()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(3);
            bst.Insert(7); // 5 has children 3 and 7

            // Act
            bst.Delete(5);

            // Assert
            bst.Find(5).Should().BeNull();
            // The successor of 5 (two children) should be the min value of right subtree (7).
            // Or if standard implementation, replaces value.
            bst.Root.Left.Value.Should().Be(7); 
            bst.Find(3).Should().NotBeNull();
            bst.Find(7).Should().NotBeNull();
        }

        [Fact]
        public void Delete_ShouldRemoveRoot_WhenRootIsLeaf()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);

            // Act
            bst.Delete(10);

            // Assert
            bst.Find(10).Should().BeNull();
            bst.Root.Should().BeNull();
        }

        [Fact]
        public void Delete_ShouldRemoveRoot_WhenRootHasOneChild()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(15);

            // Act
            bst.Delete(10);

            // Assert
            bst.Find(10).Should().BeNull();
            bst.Root.Value.Should().Be(15);
        }

        [Fact]
        public void Delete_ShouldRemoveRoot_WhenRootHasTwoChildren()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(15);
            bst.Insert(12);
            bst.Insert(20);

            // Act
            bst.Delete(10);

            // Assert
            bst.Find(10).Should().BeNull();
            // Successor of 10 is min of right subtree (15's subtree), which is 12.
            bst.Root.Value.Should().Be(12);
            bst.Root.Right.Value.Should().Be(15);
            bst.Root.Left.Value.Should().Be(5);
        }

        [Fact]
        public void Delete_ShouldDoNothing_WhenNodeNotFound()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);

            // Act
            bst.Delete(99);

            // Assert
            bst.Find(10).Should().NotBeNull();
            bst.Find(5).Should().NotBeNull();
            bst.Root.Value.Should().Be(10);
        }

        [Fact]
        public void Find_ShouldWorkWithStrings()
        {
            // Arrange
            var bst = new BinarySearchTree<string>();
            bst.Insert("apple");
            bst.Insert("banana");
            bst.Insert("cherry");

            // Act
            var node = bst.Find("banana");

            // Assert
            node.Should().NotBeNull();
            node.Value.Should().Be("banana");
        }
    }
}
