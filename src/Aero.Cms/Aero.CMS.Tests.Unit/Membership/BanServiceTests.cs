using System;
using System.Threading.Tasks;
using Aero.CMS.Core.Membership.Models;
using Aero.CMS.Core.Membership.Services;
using Xunit;

namespace Aero.CMS.Tests.Unit.Membership;

public class BanServiceTests
{
    private readonly BanService _sut;

    public BanServiceTests()
    {
        _sut = new BanService();
    }

    [Fact]
    public async Task BanAsync_SetsIsBannedToTrue()
    {
        // Arrange
        var user = new CmsUser();

        // Act
        await _sut.BanAsync(user, "Test reason");

        // Assert
        Assert.True(user.IsBanned);
        Assert.Equal("Test reason", user.BanReason);
    }

    [Fact]
    public async Task BanAsync_WithNullUntil_IsPermanentBan()
    {
        // Arrange
        var user = new CmsUser();

        // Act
        await _sut.BanAsync(user, "Permanent", null);

        // Assert
        Assert.Null(user.BannedUntil);
        Assert.True(await _sut.IsBannedAsync(user));
    }

    [Fact]
    public async Task BanAsync_WithFutureDateTime_IsTemporaryBan()
    {
        // Arrange
        var user = new CmsUser();
        var until = DateTime.UtcNow.AddDays(1);

        // Act
        await _sut.BanAsync(user, "Temporary", until);

        // Assert
        Assert.Equal(until, user.BannedUntil);
        Assert.True(await _sut.IsBannedAsync(user));
    }

    [Fact]
    public async Task UnbanAsync_ClearsProperties()
    {
        // Arrange
        var user = new CmsUser
        {
            IsBanned = true,
            BanReason = "Old",
            BannedUntil = DateTime.UtcNow.AddDays(1)
        };

        // Act
        await _sut.UnbanAsync(user);

        // Assert
        Assert.False(user.IsBanned);
        Assert.Null(user.BanReason);
        Assert.Null(user.BannedUntil);
    }

    [Fact]
    public async Task IsBannedAsync_TrueForPermanentBan()
    {
        // Arrange
        var user = new CmsUser { IsBanned = true, BannedUntil = null };

        // Assert
        Assert.True(await _sut.IsBannedAsync(user));
    }

    [Fact]
    public async Task IsBannedAsync_TrueForActiveTemporaryBan()
    {
        // Arrange
        var user = new CmsUser { IsBanned = true, BannedUntil = DateTime.UtcNow.AddHours(1) };

        // Assert
        Assert.True(await _sut.IsBannedAsync(user));
    }

    [Fact]
    public async Task IsBannedAsync_FalseForExpiredTemporaryBan()
    {
        // Arrange
        var user = new CmsUser { IsBanned = true, BannedUntil = DateTime.UtcNow.AddHours(-1) };

        // Assert
        Assert.False(await _sut.IsBannedAsync(user));
    }
}
