using Bunit;
using Electra.MerakiUI.Testimonials;

namespace Electra.MerakiUI.Tests.Testimonials;

public class TestimonialTests : BunitContext
{
    [Fact]
    public void TestimonialCard_ShouldRenderContent()
    {
        var cut = Render<TestimonialCard>(parameters => parameters
            .Add(p => p.Content, "Amazing service!")
        );

        Assert.Contains("Amazing service!", cut.Markup);
    }
}
