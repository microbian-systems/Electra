using Bunit;
using Electra.MerakiUI.Emails;
using Xunit;

namespace Electra.MerakiUI.Tests.Emails;

public class EmailTests : BunitContext
{
    [Fact]
    public void EmailVerification_ShouldRenderCode()
    {
        var cut = Render<EmailVerification>(parameters => parameters
            .Add(p => p.VerificationCode, "1234")
        );

        Assert.Contains("1", cut.Markup);
        Assert.Contains("2", cut.Markup);
        Assert.Contains("3", cut.Markup);
        Assert.Contains("4", cut.Markup);
    }
}
