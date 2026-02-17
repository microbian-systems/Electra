using Aero.MerakiUI.Skeletons;
using Bunit;
using Aero.MerakiUI.Skeletons;

namespace Aero.MerakiUI.Tests.Skeletons;

public class SkeletonTests : BunitContext
{
    [Fact]
    public void SkeletonCard_ShouldRenderPulse()
    {
        var cut = Render<SkeletonCard>();
        cut.Find(".animate-pulse");
    }
}
