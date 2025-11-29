using Xunit;
using FluentAssertions;
using Electra.DataStructures.Trees;

namespace Electra.DataStructures.Tests
{
    public class ExpressionTreeTests
    {
        [Theory]
        [InlineData("3 4 + 2 *", 14)]
        [InlineData("5 1 2 + 4 * + 3 -", 14)]
        public void Evaluate_Postfix_Expression_Correctly(string postfix, double expected)
        {
            // Arrange
            var expTree = new ExpressionTree();
            
            // Act
            expTree.Build(postfix);
            var result = expTree.Evaluate();

            // Assert
            result.Should().Be(expected);
        }
    }
}
