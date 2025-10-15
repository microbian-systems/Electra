using System.Text;
using Electra.Auth.Services.Implementation;
using FakeItEasy;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace Electra.Auth.Tests.Services;

public class DefaultRegistrationCeremonyHandleServiceTests
{
    private readonly IDataProtectionProvider _fakeDdp;
    private readonly IDataProtector _fakeProtector;
    private readonly DefaultRegistrationCeremonyHandleService _sut;
    private readonly HttpContext _fakeHttpContext;
    private readonly IRequestCookieCollection _fakeRequestCookies;
    private readonly IResponseCookies _fakeResponseCookies;

    public DefaultRegistrationCeremonyHandleServiceTests()
    {
        _fakeDdp = A.Fake<IDataProtectionProvider>();
        _fakeProtector = A.Fake<IDataProtector>();
        _fakeHttpContext = A.Fake<HttpContext>();
        _fakeRequestCookies = A.Fake<IRequestCookieCollection>();
        _fakeResponseCookies = A.Fake<IResponseCookies>();
        
        A.CallTo(() => _fakeHttpContext.Request.Cookies).Returns(_fakeRequestCookies);
        A.CallTo(() => _fakeHttpContext.Response.Cookies).Returns(_fakeResponseCookies);
        A.CallTo(() => _fakeDdp.CreateProtector(A<string>._)).Returns(_fakeProtector);
        
        _sut = new DefaultRegistrationCeremonyHandleService(_fakeDdp);
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveRegistrationCeremonyId_WhenValidIdProvided()
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
        A.CallTo(() => _fakeResponseCookies.Append(A<string>._, A<string>._, A<CookieOptions>._))
            .MustHaveHappenedOnceExactly();
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
    public async Task ReadAsync_ShouldReturnRegistrationCeremonyId_WhenCookieExists()
    {
        // Arrange
        var expectedRegistrationCeremonyId = "test-registration-ceremony-id";
        var cancellationToken = CancellationToken.None;
        var protectedData = "protected-data";
        var unprotectedData = Encoding.UTF8.GetBytes(expectedRegistrationCeremonyId);
        
        A.CallTo(() => _fakeRequestCookies[A<string>._]).Returns(protectedData);
        A.CallTo(() => _fakeProtector.Unprotect(A<byte[]>._)).Returns(unprotectedData);

        // Act
        var result = await _sut.ReadAsync(_fakeHttpContext, cancellationToken);

        // Assert
        result.Should().Be(expectedRegistrationCeremonyId);
    }

    [Fact]
    public async Task ReadAsync_ShouldReturnNull_WhenCookieDoesNotExist()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        
        A.CallTo(() => _fakeRequestCookies[A<string>._]).Returns((string?)null);

        // Act
        var result = await _sut.ReadAsync(_fakeHttpContext, cancellationToken);

        // Assert
        result.Should().BeNull();
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
    public async Task DeleteAsync_ShouldDeleteCookie_WhenCalled()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;

        // Act
        await _sut.DeleteAsync(_fakeHttpContext, cancellationToken);

        // Assert
        A.CallTo(() => _fakeResponseCookies.Delete(A<string>._))
            .MustHaveHappenedOnceExactly();
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task SaveAsync_ShouldHandleEmptyOrWhitespaceIds(string registrationCeremonyId)
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var protectedData = Encoding.UTF8.GetBytes("protected-data");
        
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
    public async Task ReadAsync_ShouldHandleUnprotectException_Gracefully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var protectedData = "invalid-protected-data";
        
        A.CallTo(() => _fakeRequestCookies[A<string>._]).Returns(protectedData);
        A.CallTo(() => _fakeProtector.Unprotect(A<byte[]>._))
            .Throws<System.Security.Cryptography.CryptographicException>();

        // Act
        var result = await _sut.ReadAsync(_fakeHttpContext, cancellationToken);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task SaveAsync_ShouldHandleSpecialCharacters_InCeremonyId()
    {
        // Arrange
        var ceremonyIdWithSpecialChars = "test-ceremony-id@#$%^&*(){}[]|\\:;<>?,./";
        var cancellationToken = CancellationToken.None;
        var protectedData = Encoding.UTF8.GetBytes("protected-data");
        
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>._))
            .Returns(protectedData);

        // Act
        await _sut.SaveAsync(_fakeHttpContext, ceremonyIdWithSpecialChars, cancellationToken);

        // Assert
        A.CallTo(() => _fakeProtector.Protect(A<byte[]>.That.Matches(bytes => 
            Encoding.UTF8.GetString(bytes) == ceremonyIdWithSpecialChars)))
            .MustHaveHappenedOnceExactly();
    }

    [Fact]
    public async Task ReadAsync_ShouldHandleEmptyProtectedData_Gracefully()
    {
        // Arrange
        var cancellationToken = CancellationToken.None;
        var protectedData = "valid-data";
        var emptyUnprotectedData = Array.Empty<byte>();
        
        A.CallTo(() => _fakeRequestCookies[A<string>._]).Returns(protectedData);
        A.CallTo(() => _fakeProtector.Unprotect(A<byte[]>._)).Returns(emptyUnprotectedData);

        // Act
        var result = await _sut.ReadAsync(_fakeHttpContext, cancellationToken);

        // Assert
        result.Should().Be(string.Empty);
    }
}
