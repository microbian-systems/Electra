using System.Collections.Generic;
using FluentAssertions;
using Xunit;

namespace Electra.Core.Tests;

public class SnowflakeTests
{
    [Fact]
    public void NewId_ShouldGenerateUniqueIds()
    {
        // Arrange
        var ids = new HashSet<long>();
        var numberOfIdsToGenerate = 10_000_000;

        // Act
        for (int i = 0; i < numberOfIdsToGenerate; i++)
        {
            var newId = Snowflake.NewId();
            var res = ids.Add(newId);
            res.Should() 
                .BeTrue($"because Duplicate IDs should not exist: {newId}");
        }

        // Assert
        numberOfIdsToGenerate.Should().Be(ids.Count);
    }
}