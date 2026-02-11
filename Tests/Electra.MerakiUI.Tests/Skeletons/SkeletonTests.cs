using Bunit;
using Electra.MerakiUI.Skeletons;
using Xunit;

namespace Electra.MerakiUI.Tests.Skeletons;

public class SkeletonTests : BunitContext
{
    [Fact]
    public void SkeletonCard_ShouldRenderPulse()
    {
        var cut = Render<SkeletonCard>();
        cut.Find(".animate-pulse");
    }
}
