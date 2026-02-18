using Aero.MerakiUI.Teams;
using Bunit;
using Aero.MerakiUI.Teams;

namespace Aero.MerakiUI.Tests.Teams;

public class TeamTests : BunitContext
{
    [Fact]
    public void TeamCard_ShouldRenderNameAndRole()
    {
        var cut = Render<TeamCard>(parameters => parameters
            .Add(p => p.Name, "Alice")
            .Add(p => p.Role, "Dev")
        );

        Assert.Contains("Alice", cut.Markup);
        Assert.Contains("Dev", cut.Markup);
    }
}
