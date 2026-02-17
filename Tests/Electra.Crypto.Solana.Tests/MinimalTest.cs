using Xunit;

namespace Electra.Crypto.Solana.Tests;

public class MinimalTest
{
    [Fact]
    public void SimpleTest_ShouldPass()
    {
        // Arrange
        var result = 2 + 2;
        
        // Assert
        Assert.Equal(4, result);
    }
}