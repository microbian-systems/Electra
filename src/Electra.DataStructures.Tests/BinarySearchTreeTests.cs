using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using Bogus;
using System;

namespace Electra.DataStructures.Tests
{
    public class BinarySearchTreeTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void Insert_And_Find_Success()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            var values = _faker.Random.Digits(5);
            foreach (var value in values)
            {
                bst.Insert(value);
            }
            var valueToFind = values[2];

            // Act
            var foundNode = bst.Find(valueToFind);

            // Assert
            foundNode.Should().NotBeNull();
            foundNode.Value.Should().Be(valueToFind);
        }

        [Fact]
        public void Delete_Node_NotFound()
        {
            // Arrange
            var bst = new BinarySearchTree<int>();
            bst.Insert(10);
            bst.Insert(5);
            bst.Insert(15);
            
            // Act
            bst.Delete(5);
            var foundNode = bst.Find(5);

            // Assert
            foundNode.Should().BeNull();
        }
    }
}
