using Aero.MerakiUI.Testimonials;
using Bunit;
using Aero.MerakiUI.Testimonials;

namespace Aero.MerakiUI.Tests.Testimonials;

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
