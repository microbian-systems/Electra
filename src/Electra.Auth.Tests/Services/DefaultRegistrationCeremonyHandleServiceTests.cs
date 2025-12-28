using System.Text;
using Electra.Auth.Services.Implementation;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;

namespace Electra.Auth.Tests.Services;

public class DefaultRegistrationCeremonyHandleServiceTests
{
    private readonly IDataProtectionProvider _fakeDdp;
    private readonly IDataProtector _fakeProtector;
    private readonly DefaultRegistrationCeremonyHandleService _sut;
    private readonly HttpContext _fakeHttpContext;

    public DefaultRegistrationCeremonyHandleServiceTests()
    {
        _fakeDdp = A.Fake<IDataProtectionProvider>();
        _fakeProtector = A.Fake<IDataProtector>();
        _fakeHttpContext = A.Fake<HttpContext>();
        
        // Setup basic HTTP context structure
        var request = A.Fake<HttpRequest>();
        var response = A.Fake<HttpResponse>();
        A.CallTo(() => _fakeHttpContext.Request).Returns(request);
        A.CallTo(() => _fakeHttpContext.Response).Returns(response);
        
        A.CallTo(() => _fakeDdp.CreateProtector(A<string>._)).Returns(_fakeProtector);
        
        _sut = new DefaultRegistrationCeremonyHandleService(_fakeDdp);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenDataProtectionProviderIsNull()
    {
        // Act & Assert
        var act = () => new DefaultRegistrationCeremonyHandleService(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Constructor_ShouldCreateInstance_WhenValidParametersProvided()
    {
        // Act
        var service = new DefaultRegistrationCeremonyHandleService(_fakeDdp);

        // Assert
        service.Should().NotBeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var registrationCeremonyId = "test-registration-ceremony-id";
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await _sut.Invoking(x => x.SaveAsync(_fakeHttpContext, registrationCeremonyId, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ReadAsync_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await _sut.Invoking(x => x.ReadAsync(_fakeHttpContext, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowOperationCanceledException_WhenCancellationRequested()
    {
        // Arrange
        var cancellationToken = new CancellationToken(true);

        // Act & Assert
        await _sut.Invoking(x => x.DeleteAsync(_fakeHttpContext, cancellationToken))
            .Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task SaveAsync_ShouldCallProtector_WhenValidIdProvided()
    {
        // Arrange
        var registrationCeremonyId = "test-registration-ceremony-id";
        var cancellationToken = CancellationToken.None;
        var protectedData = "protected-data"u8.ToArray();
        
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._))
            .Returns(protectedData);

        // Act
        await _sut.SaveAsync(_fakeHttpContext, registrationCeremonyId, cancellationToken);

        // Assert
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>.That.Matches(bytes => 
            Encoding.UTF8.GetString(bytes) == registrationCeremonyId)))
            .MustHaveHappenedOnceExactly();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task SaveAsync_ShouldHandleEmptyOrWhitespaceIds(string registrationCeremonyId)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var protectedData = "protected-data"u8.ToArray();
        
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._))
            .Returns(protectedData);

        // Act
        await _sut.SaveAsync(_fakeHttpContext, registrationCeremonyId, cancellationToken);

        // Assert
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>.That.Matches(bytes => 
            Encoding.UTF8.GetString(bytes) == registrationCeremonyId)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnNull_WhenNoCookieExists()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        
        // Note: This test would require setting up the cookie infrastructure
        // For now, we test the method completes without throwing

        // Act
        var result = await _sut.ReadAsync(_fakeHttpContext, cancellationToken);

        // Assert - method completes (actual behavior depends on cookie setup)
        // In a real scenario, this would return null when no cookie exists
        result.Should().BeNull();
    }

    // Note: For comprehensive testing of cookie operations, use integration tests
    // that set up the full ASP.NET Core context with actual cookie management
}