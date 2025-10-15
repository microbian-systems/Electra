// Simplified DefaultUserService tests focusing on essential functionality
using System.Text;
using System.Text.Json;
using Electra.Auth.Services.Implementation;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Electra.Auth.Tests.Services;

public class DefaultUserServiceTests
{
    private readonly IDataProtectionProvider _fakeDdp;
    private readonly IDataProtector _fakeProtector;
    private readonly DefaultUserService _sut;
    private readonly HttpContext _fakeHttpContext;

    public DefaultUserServiceTests()
    {
        _fakeDdp = A.Fake<IDataProtectionProvider>();
        _fakeProtector = A.Fake<IDataProtector>();
        _fakeHttpContext = A.Fake<HttpContext>();
        
        var request = A.Fake<HttpRequest>();
        var response = A.Fake<HttpResponse>();
        A.CallTo(() => _fakeHttpContext.Request).Returns(request);
        A.CallTo(() => _fakeHttpContext.Response).Returns(response);
        A.CallTo(() => _fakeDdp.CreateProtector(A<string>._)).Returns(_fakeProtector);
        
        _sut = new DefaultUserService(_fakeDdp);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDataProtectionProviderIsNull()
    {
        // Act & Assert
        var act = () => new DefaultUserService(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenValidParametersProvided()
    {
        // Act
        var service = new DefaultUserService(_fakeDdp);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var username = "testuser";
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await _sut.Invoking(x => x.CreateAsync(_fakeHttpContext, username, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task FindAsync_ByUsername_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var username = "testuser";
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await _sut.Invoking(x => x.FindAsync(_fakeHttpContext, username, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task CreateAsync_ShouldCallProtector_WhenValidUsernameProvided()
    {
        // Arrange
        var username = "testuser";
        var cancellationToken = CancellationToken.None;
        var protectedData = "protected-data"u8.ToArray();
        
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._)).Returns(protectedData);

        // Act
        var result = await _sut.CreateAsync(_fakeHttpContext, username, cancellationToken);

        // Assert
        result.Should().NotBeNull();
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._)).MustHaveHappened();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("testuser123")]
    public async Task CreateAsync_ShouldHandleDifferentUsernames(string username)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var protectedData = Encoding.UTF8.GetBytes("protected-data");
        
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._)).Returns(protectedData);

        // Act
        var result = await _sut.CreateAsync(_fakeHttpContext, username, cancellationToken);

        // Assert
        result.Should().NotBeNull();
    }

    // Note: For comprehensive cookie storage testing, use integration tests
    // that set up the full ASP.NET Core context with actual cookie management
}