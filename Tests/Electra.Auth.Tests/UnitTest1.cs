using FluentAssertions;

namespace Electra.Auth.Tests;

public class JwtExtensionsTests
{
    private readonly string validSecret;
    private readonly Faker faker = new();

    public JwtExtensionsTests()
    {
        validSecret = GenerateRandomSecretKey();
    }

    [Fact]
    public void DecodeHttpRequest_ValidToken_ReturnsJwtPayload()
    {
        // Arrange
        var userId = faker.Random.Guid().ToString();
        var user = new IdentityUser<string> { Email = $"{userId}@example.com", Id = userId };
        var token = CreateJwtTokenForUser(user, validSecret);

        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers["Authorization"]).Returns("Bearer " + token);

        // Act
        var payload = request.DecodeJwtPayload(validSecret)
            .ValueOrDefault;

        // Assert
        Assert.NotNull(payload);
        Assert.Equal(user.Email, payload.Sub);
        Assert.NotEmpty(payload.Claims);
    }

    [Fact]
    public void DecodeStringToken_ValidToken_ReturnsJwtPayload()
    {
        // Arrange
        var userId = faker.Random.Guid().ToString();
        var user = new IdentityUser<string> { Email = $"{userId}@example.com", Id = userId };
        var token = CreateJwtTokenForUser(user, validSecret);

        // Act
        var payload = token.DecodeJwtPayload(validSecret)
            .ValueOrDefault;

        // Assert
        Assert.NotNull(payload);
        Assert.Equal(user.Email, payload.Sub);
        Assert.NotEmpty(payload.Claims);
    }

    [Fact]
    public void DecodeHttpRequest_InvalidToken_ReturnsNull()
    {
        // Arrange
        var request = A.Fake<HttpRequest>();
        A.CallTo(() => request.Headers["Authorization"])
            .Returns("Bearer invalid-token");

        // Act
        var payload = request.DecodeJwtPayload(validSecret);

        // Assert
        payload.Should().NotBeNull();
        payload.ValueOrDefault.Should().BeNull();
        //payload.Value.GetType().Should().Be<Exception>();
    }

    [Fact]
    public void DecodeStringToken_InvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid-token";

        // Act
        var payload = invalidToken.DecodeJwtPayload(validSecret);

        // Assert
        payload.Should().NotBeNull();
        payload.ValueOrDefault.Should().BeNull();
        //payload.Value.GetType().Should().Be<Exception>();
    }

    private string CreateJwtTokenForUser(IdentityUser<string> user, string secret)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(
            issuer: "test_issuer",
            audience: "test_audience",
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRandomSecretKey()
    {
        // Generate a random string with at least 32 characters
        return faker.Random.AlphaNumeric(32);
    }
}