using System;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

namespace Electra.Crypto.Solana.Tests;

public class BasicCompilationTest
{
    [Fact]
    public void BasicArraySyntax_ShouldWork()
    {
        // Test C# 12 collection initializer syntax
        string[] emptyArray = [];
        string[] arrayWithItems = ["item1", "item2"];
        
        emptyArray.Should().NotBeNull();
        emptyArray.Length.Should().Be(0);
        
        arrayWithItems.Should().NotBeNull();
        arrayWithItems.Length.Should().Be(2);
        arrayWithItems[0].Should().Be("item1");
        arrayWithItems[1].Should().Be("item2");
    }
    
    [Fact]
    public async Task BasicAsync_ShouldWork()
    {
        // Test basic async functionality
        await Task.Delay(1);
        
        var result = await GetTestValueAsync();
        result.Should().Be("test");
    }
    
    private async Task<string> GetTestValueAsync()
    {
        await Task.Delay(1);
        return "test";
    }
    
    [Fact]
    public void BasicRecord_ShouldWork()
    {
        // Test record syntax
        var record = new TestRecord("test", 42, []);
        
        record.Should().NotBeNull();
        record.Name.Should().Be("test");
        record.Value.Should().Be(42);
        record.Tags.Should().NotBeNull();
        record.Tags.Length.Should().Be(0);
    }
}

public record TestRecord(string Name, int Value, string[] Tags);