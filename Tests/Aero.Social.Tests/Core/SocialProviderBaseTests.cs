using Aero.Social.Abstractions;
using Aero.Social.Models;
using Aero.Social.Tests.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Tests.Core;

public class SocialProviderBaseTests : ProviderTestBase
{
    [Fact]
    public void CheckScopes_WhenAllScopesGranted_ShouldNotThrow()
    {
        var required = new[] { "read", "write", "email" };
        var granted = new[] { "read", "write", "email", "profile" };

        var exception = Record.Exception(() => CreateTestProvider().TestCheckScopes(required, granted));
        
        exception.ShouldBeNull();
    }

    [Fact]
    public void CheckScopes_WhenScopeMissing_ShouldThrowNotEnoughScopesException()
    {
        var required = new[] { "read", "write", "admin" };
        var granted = new[] { "read", "write" };

        Should.Throw<NotEnoughScopesException>(() => CreateTestProvider().TestCheckScopes(required, granted));
    }

    [Fact]
    public void CheckScopes_WhenScopesGrantedAsString_ShouldParseCorrectly()
    {
        var required = new[] { "read", "write" };
        var grantedScopes = "read write email";

        var exception = Record.Exception(() => CreateTestProvider().TestCheckScopes(required, grantedScopes));
        
        exception.ShouldBeNull();
    }

    [Fact]
    public void CheckScopes_WhenScopesGrantedAsCommaDelimited_ShouldParseCorrectly()
    {
        var required = new[] { "read", "write" };
        var grantedScopes = "read,write,email";

        var exception = Record.Exception(() => CreateTestProvider().TestCheckScopes(required, grantedScopes));
        
        exception.ShouldBeNull();
    }

    [Fact]
    public void CheckScopes_ShouldBeCaseInsensitive()
    {
        var required = new[] { "READ", "Write" };
        var granted = new[] { "read", "WRITE" };

        var exception = Record.Exception(() => CreateTestProvider().TestCheckScopes(required, granted));
        
        exception.ShouldBeNull();
    }

    [Fact]
    public void MakeId_ShouldGenerateStringOfCorrectLength()
    {
        var result = CreateTestProvider().TestMakeId(10);
        
        result.Length.ShouldBe(10);
    }

    [Fact]
    public void MakeId_ShouldGenerateAlphanumericString()
    {
        var result = CreateTestProvider().TestMakeId(20);
        
        result.ShouldAllBe(c => char.IsLetterOrDigit(c));
    }

    [Fact]
    public void MakeId_ShouldGenerateDifferentValues()
    {
        var results = new HashSet<string>();
        for (int i = 0; i < 100; i++)
        {
            results.Add(CreateTestProvider().TestMakeId(10));
        }
        
        results.Count.ShouldBeGreaterThan(90);
    }

    private TestSocialProvider CreateTestProvider()
    {
        return new TestSocialProvider(HttpClient, LoggerMock.Object);
    }
}

public class TestSocialProvider : SocialProviderBase
{
    public TestSocialProvider(HttpClient httpClient, ILogger logger) 
        : base(httpClient, logger)
    {
    }

    public override string Identifier => "test";
    public override string Name => "Test Provider";
    public override string[] Scopes => new[] { "read", "write" };

    public override int MaxLength(object? additionalSettings = null) => 1000;

    public override Task<PostResponse[]> PostAsync(
        string id, string accessToken, List<PostDetails> posts, 
        Integration integration, CancellationToken cancellationToken = default)
        => Task.FromResult(Array.Empty<PostResponse>());

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new GenerateAuthUrlResponse());

    public override Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthTokenDetails());

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
        => Task.FromResult(new AuthTokenDetails());

    public void TestCheckScopes(string[] required, string[] granted)
        => CheckScopes(required, granted);

    public void TestCheckScopes(string[] required, string grantedScopes)
        => CheckScopes(required, grantedScopes);

    public string TestMakeId(int length) => MakeId(length);
}
