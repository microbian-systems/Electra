using Bunit;
using Electra.MerakiUI.Faq;
using Xunit;

namespace Electra.MerakiUI.Tests.Faq;

public class FaqTests : BunitContext
{
    [Fact]
    public void FaqAccordion_ShouldRenderTitle()
    {
        var cut = Render<FaqAccordion>(parameters => parameters
            .Add(p => p.Title, "Help Center")
        );

        Assert.Contains("Help Center", cut.Markup);
    }

    [Fact]
    public void FaqItem_ShouldRenderQuestion()
    {
        var cut = Render<FaqItem>(parameters => parameters
            .Add(p => p.Question, "Is it free?")
            .Add(p => p.Answer, "Yes, absolutely.")
        );

        Assert.Contains("Is it free?", cut.Markup);
        Assert.Contains("Yes, absolutely.", cut.Markup);
    }
}
