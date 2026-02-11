using Bunit;
using Electra.MerakiUI.Avatars;

namespace Electra.MerakiUI.Tests.Avatars;

public class AvatarTests : BunitContext
{
    [Fact]
    public void Avatar_ShouldRenderCorrectSize()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.Size, AvatarSize.Lg)
        );

        cut.Find(".w-12.h-12");
    }

    [Fact]
    public void Avatar_ShouldRenderStatus()
    {
        var cut = Render<Avatar>(parameters => parameters
            .Add(p => p.ShowStatus, true)
            .Add(p => p.StatusColor, "bg-red-500")
        );

        cut.Find(".bg-red-500");
    }

    [Fact]
    public void ProfileAvatar_ShouldRenderNameAndEmail()
    {
        var cut = Render<ProfileAvatar>(parameters => parameters
            .Add(p => p.Name, "Jane Doe")
            .Add(p => p.Email, "jane@example.com")
        );

        Assert.Contains("Jane Doe", cut.Markup);
        Assert.Contains("jane@example.com", cut.Markup);
    }
}
