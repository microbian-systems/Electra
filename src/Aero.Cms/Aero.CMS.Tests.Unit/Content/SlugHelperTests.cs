using Aero.CMS.Core.Extensions;
using Shouldly;
using Xunit;

namespace Aero.CMS.Tests.Unit.Content;

public class SlugHelperTests
{
    [Fact]
    public void Generate_LowercasesInput()
    {
        var result = SlugHelper.Generate("Hello World");
        result.ShouldBe("hello-world");
    }

    [Fact]
    public void Generate_SpacesBecomeHyphens()
    {
        var result = SlugHelper.Generate("Hello World");
        result.ShouldBe("hello-world");
    }

    [Fact]
    public void Generate_SpecialCharsRemoved()
    {
        var result = SlugHelper.Generate("Hello@World!");
        result.ShouldBe("helloworld");
    }

    [Fact]
    public void Generate_MultipleHyphensCollapsed()
    {
        var result = SlugHelper.Generate("Hello   World");
        result.ShouldBe("hello-world");
    }

    [Fact]
    public void Generate_LeadingTrailingHyphensTrimmed()
    {
        var result = SlugHelper.Generate("  Hello World  ");
        result.ShouldBe("hello-world");
    }

    [Fact]
    public void Generate_HelloWorldExclamation_ReturnsHelloWorld()
    {
        var result = SlugHelper.Generate("Hello World!");
        result.ShouldBe("hello-world");
    }

    [Fact]
    public void Generate_EmptyInput_ReturnsEmptyString()
    {
        var result = SlugHelper.Generate("");
        result.ShouldBe("");
    }
}