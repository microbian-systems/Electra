using Bunit;
using Electra.MerakiUI.Auth;
using Xunit;

namespace Electra.MerakiUI.Tests.Auth;

public class AuthTests : BunitContext
{
    [Fact]
    public void SignInCard_ShouldRenderTitle()
    {
        var cut = Render<SignInCard>(parameters => parameters
            .Add(p => p.Title, "Login Now")
        );

        Assert.Contains("Login Now", cut.Markup);
    }

    [Fact]
    public void SignUpCard_ShouldRenderSignInUrl()
    {
        var cut = Render<SignUpCard>(parameters => parameters
            .Add(p => p.SignInUrl, "/login")
        );

        Assert.Contains("href=\"/login\"", cut.Markup);
    }
}
