using Aero.Models.Entities;
using Aero.RavenDB;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;

namespace Electra.Persistence.RavenDB.Tests;

public class RavenDbPersistenceTests : RavenDbTestBase
{
    private readonly RavenDbUnitOfWork _unitOfWork;
    private readonly ILogger<RavenDbUnitOfWork> _uowLogger;
    private readonly ILoggerFactory _loggerFactory;

    public RavenDbPersistenceTests()
    {
        _uowLogger = A.Fake<ILogger<RavenDbUnitOfWork>>();
        _loggerFactory = A.Fake<ILoggerFactory>();
        A.CallTo(() => _loggerFactory.CreateLogger(A<string>._)).Returns(A.Fake<ILogger>());

        _unitOfWork = new RavenDbUnitOfWork(DocumentStore.OpenAsyncSession(), _uowLogger, _loggerFactory);
    }

    [Fact]
    public async Task UnitOfWork_Should_Persist_User_Added_Through_Repository()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = "users/1",
            UserName = "uowuser",
            Email = "uow@example.com",
            FirstName = "UoW",
            LastName = "User",
            CreatedBy = "System"
        };

        // Act
        await _unitOfWork.Users.InsertAsync(user);
        var savedCount = await _unitOfWork.SaveChangesAsync();

        // Assert
        savedCount.Should().BeGreaterThan(0);

        using var session = DocumentStore.OpenAsyncSession();
        var persistedUser = await session.LoadAsync<AeroUser>(user.Id);
        persistedUser.Should().NotBeNull();
        persistedUser.UserName.Should().Be("uowuser");
    }

    [Fact]
    public async Task Repository_FindByIdAsync_Should_Return_User_In_Same_Session()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = "users/find-async",
            UserName = "finduser",
            Email = "find@example.com",
            FirstName = "Find",
            LastName = "User",
            CreatedBy = "System"
        };
        await _unitOfWork.Users.InsertAsync(user);
        // Note: Even without SaveChangesAsync, it should be findable in the same session if it's cached, 
        // but RavenDbRepositoryBase uses session.Query which might not see it until saved depending on RavenDB's behavior.
        // Actually session.LoadAsync sees it. RavenDbRepositoryBase.FindByIdAsync uses Query.
        await _unitOfWork.SaveChangesAsync();

        // Act
        var result = await _unitOfWork.Users.FindByIdAsync(user.Id);

        // Assert
        result.IsSome.Should().BeTrue();
        result.IfSome(u => u.Id.Should().Be(user.Id));
    }

    [Fact]
    public async Task UnitOfWork_Should_Track_Multiple_Changes()
    {
        // Arrange
        var user1 = new AeroUser { Id = "users/u1", UserName = "user1", Email = "u1@ex.com", FirstName = "f1", LastName = "l1", CreatedBy = "s" };
        var user2 = new AeroUser { Id = "users/u2", UserName = "user2", Email = "u2@ex.com", FirstName = "f2", LastName = "l2", CreatedBy = "s" };
        
        await _unitOfWork.Users.InsertAsync(user1);
        await _unitOfWork.Users.InsertAsync(user2);

        // Act
        var savedCount = await _unitOfWork.SaveChangesAsync();

        // Assert
        savedCount.Should().BeGreaterThanOrEqualTo(2);

        using var session = DocumentStore.OpenAsyncSession();
        var count = await session.Query<AeroUser>().CountAsync();
        count.Should().Be(2);
    }

    [Fact]
    public async Task Repository_DeleteAsync_Should_Work_With_UnitOfWork()
    {
        // Arrange
        var user = new AeroUser
        {
            Id = "users/delete-me",
            UserName = "deleteme",
            Email = "del@example.com",
            FirstName = "Del",
            LastName = "User",
            CreatedBy = "System"
        };
        await _unitOfWork.Users.InsertAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var deleted = await _unitOfWork.Users.DeleteAsync(user.Id);
        await _unitOfWork.SaveChangesAsync();

        // Assert
        deleted.Should().BeTrue();
        using var session = DocumentStore.OpenAsyncSession();
        var persistedUser = await session.LoadAsync<AeroUser>(user.Id);
        persistedUser.Should().BeNull();
    }

    [Fact]
    public async Task Repository_FindAsync_Should_Return_Matching_Users()
    {
        // Arrange
        var user1 = new AeroUser { Id = "users/f1", UserName = "find1", Email = "f1@ex.com", FirstName = "Match", LastName = "User", CreatedBy = "s" };
        var user2 = new AeroUser { Id = "users/f2", UserName = "find2", Email = "f2@ex.com", FirstName = "Other", LastName = "User", CreatedBy = "s" };
        await _unitOfWork.Users.InsertAsync(user1);
        await _unitOfWork.Users.InsertAsync(user2);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var results = await _unitOfWork.Users.FindAsync(u => u.FirstName == "Match");

        // Assert
        results.Should().HaveCount(1);
        results.First().UserName.Should().Be("find1");
    }

    [Fact]
    public async Task Repository_ExistsAsync_Should_Return_True_For_Existing_User()
    {
        // Arrange
        var user = new AeroUser { Id = "users/ex", UserName = "exists", Email = "ex@ex.com", FirstName = "Ex", LastName = "User", CreatedBy = "s" };
        await _unitOfWork.Users.InsertAsync(user);
        await _unitOfWork.SaveChangesAsync();

        // Act
        var exists = await _unitOfWork.Users.ExistsAsync(user.Id);

        // Assert
        exists.Should().BeTrue();
    }
}
