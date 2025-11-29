using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;
using Bogus;

namespace Electra.DataStructures.Tests
{
    public class BinaryTreeTests
    {
        private readonly Faker _faker = new();

        [Fact]
        public void Insert_SingleValue_RootIsCorrect()
        {
            // Arrange
            var tree = new BinaryTree<int>();
            int value = _faker.Random.Int();

            // Act
            tree.Insert(value);

            // Assert
            tree.Root.Should().NotBeNull();
            tree.Root.Value.Should().Be(value);
        }
    }
}
