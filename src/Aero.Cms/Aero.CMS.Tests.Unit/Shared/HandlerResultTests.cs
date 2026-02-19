using Aero.CMS.Core.Shared.Models;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class HandlerResultTests
{
    [Fact]
    public void Ok_HasSuccessTrueAndEmptyErrors()
    {
        var result = HandlerResult.Ok();
        
        result.Success.ShouldBeTrue();
        result.Errors.ShouldBeEmpty();
    }

    [Fact]
    public void Fail_WithString_HasSuccessFalseAndErrorsContainsMessage()
    {
        var result = HandlerResult.Fail("Something went wrong");
        
        result.Success.ShouldBeFalse();
        result.Errors.ShouldContain("Something went wrong");
    }

    [Fact]
    public void Fail_WithEnumerable_ContainsAllMessages()
    {
        var errors = new[] { "Error 1", "Error 2", "Error 3" };
        var result = HandlerResult.Fail(errors);
        
        result.Success.ShouldBeFalse();
        result.Errors.Count.ShouldBe(3);
        result.Errors.ShouldContain("Error 1");
        result.Errors.ShouldContain("Error 2");
        result.Errors.ShouldContain("Error 3");
    }

    [Fact]
    public void GenericOk_WithValue_HasSuccessTrueAndValueSet()
    {
        var result = HandlerResult<string>.Ok("test value");
        
        result.Success.ShouldBeTrue();
        result.Value.ShouldBe("test value");
    }

    [Fact]
    public void GenericFail_WithString_HasSuccessFalseAndValueDefault()
    {
        var result = HandlerResult<string>.Fail("error");
        
        result.Success.ShouldBeFalse();
        result.Value.ShouldBe(default);
    }

    [Fact]
    public void GenericFail_WithEnumerable_HasAllErrors()
    {
        var result = HandlerResult<int>.Fail(new[] { "err1", "err2" });
        
        result.Success.ShouldBeFalse();
        result.Errors.Count.ShouldBe(2);
        result.Value.ShouldBe(default);
    }

    [Fact]
    public void Generic_ValueTypePreservedForClass()
    {
        var obj = new { Name = "Test" };
        var result = HandlerResult<object>.Ok(obj);
        
        result.Value.ShouldBe(obj);
    }
}
