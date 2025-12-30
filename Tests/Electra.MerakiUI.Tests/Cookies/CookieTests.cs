using Bunit;
using Electra.MerakiUI.Cookies;
using Xunit;

namespace Electra.MerakiUI.Tests.Cookies;

public class CookieTests : BunitContext
{
    [Fact]
    public void CookieBanner_ShouldRenderMessage()
    {
        var cut = Render<CookieBanner>(parameters => parameters
            .Add(p => p.Message, "Test Cookie Message")
        );

        Assert.Contains("Test Cookie Message", cut.Markup);
    }
}
