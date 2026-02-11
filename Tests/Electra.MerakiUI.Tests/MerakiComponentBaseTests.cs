using Bunit;
using Microsoft.AspNetCore.Components;

namespace Electra.MerakiUI.Tests;

public class MerakiComponentBaseTests : BunitContext
{
    // Stub to allow test to reference the base class we expect to exist
    private class TestComponent : MerakiComponentBase
    {
    }

    [Fact]
    public void BaseComponent_ShouldHaveStandardParameters()
    {
        var cut = Render<TestComponent>(parameters => parameters
            .Add(p => p.Class, "test-class")
            .Add(p => p.Id, "test-id")
            .Add(p => p.ChildContent, (RenderFragment)(builder => builder.AddContent(0, "content")))
            .AddUnmatched("data-test", "value")
        );

        var instance = cut.Instance;

        Assert.Equal("test-class", instance.Class);
        Assert.Equal("test-id", instance.Id);
        Assert.NotNull(instance.ChildContent);
        Assert.NotNull(instance.AdditionalAttributes);
        Assert.Contains("data-test", instance.AdditionalAttributes.Keys);
        Assert.Equal("value", instance.AdditionalAttributes["data-test"]);
    }
}
