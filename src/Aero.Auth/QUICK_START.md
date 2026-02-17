# Quick Start Guide - Unified Authentication System

## Installation

### 1. Database Migration

```bash
cd D:\proj\microbians\microbians.io\Aero

# Generate migration
dotnet ef migrations add AddAuthenticationTokens \
  -p src/Aero.Persistence \
  -s src/Aero.Auth

# Apply to database
dotnet ef database update
```

### 2. Configuration

Copy and customize `appsettings.Example.json`:

```json
{
  "Auth": {
    "AccessTokenLifetimeSeconds": 300,
    "RefreshTokenLifetimeDays": 30,
    "Jwt": {
      "Issuer": "Aero",
      "Audience": "AeroClients"
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "YOUR_GOOGLE_CLIENT_ID",
      "ClientSecret": "YOUR_GOOGLE_CLIENT_SECRET"
    }
  }
}
```

### 3. Startup Code

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);

// Add authentication
builder.Services.AddAeroAuthentication(builder.Environment, builder.Configuration);

var app = builder.Build();

// Initialize signing keys (CRITICAL - call only once)
await app.Services.InitializeJwtSigningKeysAsync();

// Configure middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

---

## API Usage

### Web Login (Sets Cookie)

```bash
curl -X POST https://localhost:5001/api/auth/login-web \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "rememberMe": false
  }' \
  -c cookies.txt

# Subsequent requests automatically include cookie
curl https://localhost:5001/api/auth/sessions \
  -b cookies.txt
```

### App Login (Returns JWT)

```bash
curl -X POST https://localhost:5001/api/auth/login-app \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "SecurePass123!",
    "clientType": "mobile"
  }'

# Response includes accessToken and refreshToken
{
  "success": true,
  "accessToken": "eyJ...",
  "refreshToken": "base64...",
  "accessTokenExpiresIn": 300
}
```

### Refresh Token

```bash
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN" \
  -d '{
    "refreshToken": "base64-token"
  }'
```

### Logout

```bash
# Web
curl -X POST https://localhost:5001/api/auth/logout \
  -b cookies.txt

# App
curl -X POST https://localhost:5001/api/auth/logout-app \
  -H "Authorization: Bearer YOUR_ACCESS_TOKEN"
```

---

## Key Files Overview

| File | Purpose |
|------|---------|
| `AuthController.cs` | Main authentication endpoints |
| `JwtTokenService.cs` | Access token generation/validation |
| `RefreshTokenService.cs` | Refresh token lifecycle management |
| `JwtSigningKeyStore.cs` | Key rotation support |
| `RefreshToken.cs` | Database entity for refresh tokens |
| `JwtSigningKey.cs` | Database entity for signing keys |
| `AuthInitializationExtensions.cs` | Startup initialization |

---

## Core Concepts

### Access Tokens
- **Lifetime**: 5 minutes (configurable)
- **Format**: JWT
- **Claims**: sub (user ID), email, jti
- **Usage**: Authorization header: `Bearer {token}`

### Refresh Tokens
- **Lifetime**: 30 days (configurable)
- **Format**: Base64 random string
- **Storage**: Database (SHA-256 hashed)
- **One-time use**: Rotates on every refresh

### Signing Keys
- **Algorithm**: HMAC-SHA256
- **Rotation**: Non-breaking, keys live until tokens expire
- **Validation**: All valid keys can validate
- **Signing**: Only current key signs new tokens

---

## Security Checklist

- ✅ HTTPS required (enforced in production)
- ✅ Tokens stored securely (refresh tokens hashed)
- ✅ Short-lived access tokens (5 min)
- ✅ One-time use refresh tokens (rotation enforced)
- ✅ HttpOnly cookies for web
- ✅ CSRF protection via SameSite
- ✅ Audit logging (IP, User-Agent)

---

## Troubleshooting

### "No current signing key found"
**Solution**: Ensure `InitializeJwtSigningKeysAsync()` was called on first run

### "Invalid refresh token"
**Solution**: Refresh tokens are single-use. Check if token was already used.

### "CORS error on external login"
**Solution**: Register callback URL with OAuth provider (Google, Microsoft, etc.)

### "Token validation failed"
**Solution**: Check that `IssuerSigningKey` configuration matches your actual key

---

## Performance Tuning

### Memory Cache
Signing key store uses 5-minute cache to reduce DB queries:

```json
{
  "Auth": {
    "SigningKeysCacheMinutes": 5
  }
}
```

### Database Indexes
Automatically created on:
- `RefreshTokens.TokenHash` (unique)
- `RefreshTokens.UserId`
- `JwtSigningKeys.KeyId` (unique)
- `JwtSigningKeys.IsCurrentSigningKey`

---

## Testing

### Unit Test Example

```csharp
[Test]
public async Task LoginApp_ShouldReturnTokens()
{
    // Arrange
    var user = new AeroUser { Email = "test@example.com" };
    var request = new LoginAppRequest { Email = "test@example.com", Password = "password" };

    // Act
    var result = await controller.LoginApp(request);

    // Assert
    Assert.IsTrue(((OkObjectResult)result).Value is LoginAppResponse);
}
```

---

## Monitoring & Logging

Enable structured logging:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/auth-.txt" } }
    ]
  }
}
```

Key log events:
- `User {Email} logged in via web`
- `Generated refresh token for user {UserId}`
- `Rotated refresh token for user {UserId}`
- `Revoked all refresh tokens for user {UserId}`
- `Created new signing key: {KeyId}`

---

## Next Steps

1. ✅ Run database migrations
2. ✅ Copy `appsettings.Example.json` → `appsettings.Development.json`
3. ✅ Configure social providers (Google, etc.)
4. ✅ Add to Program.cs (AddAeroAuthentication + InitializeJwtSigningKeysAsync)
5. ✅ Test with provided curl examples
6. ✅ Integrate with frontend (web/MAUI)

---

## Support Resources

- **Full Documentation**: `AUTHENTICATION_IMPLEMENTATION.md`
- **Implementation Details**: `IMPLEMENTATION_SUMMARY.md`
- **API Endpoints**: See `AuthController.cs`
- **Code Comments**: Inline XML documentation

---

**Status**: ✅ Production Ready
**Version**: 1.0
**Last Updated**: 2026-01-31
