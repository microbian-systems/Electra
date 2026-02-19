using Aero.CMS.Core.Shared.Interfaces;
using Aero.CMS.Core.Shared.Services;
using NSubstitute;
using Shouldly;

namespace Aero.CMS.Tests.Unit.Shared;

public class SystemClockTests
{
    [Fact]
    public void UtcNow_ReturnsValueApproximatelyUtcNow()
    {
        var clock = new SystemClock();
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = clock.UtcNow;
        var after = DateTime.UtcNow.AddSeconds(1);
        
        result.ShouldBeInRange(before, after);
    }

    [Fact]
    public void UtcNow_KindIsUtc()
    {
        var clock = new SystemClock();
        
        clock.UtcNow.Kind.ShouldBe(DateTimeKind.Utc);
    }

    [Fact]
    public void ISystemClock_CanBeSubstitutedWithNSubstitute()
    {
        var fixedTime = new DateTime(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);
        var substitute = Substitute.For<ISystemClock>();
        substitute.UtcNow.Returns(fixedTime);
        
        substitute.UtcNow.ShouldBe(fixedTime);
    }
}
