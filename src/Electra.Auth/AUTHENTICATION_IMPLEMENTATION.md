# Unified Authentication System Implementation Guide

## Overview

This is a production-grade authentication system for ASP.NET Core 10 implementing the Spec Driven Development (SDD) requirements. It supports:

- **BFF-style cookie authentication** for web applications
- **JWT + refresh token authentication** for mobile (MAUI) and desktop apps
- **Social logins** (Google, Microsoft, Facebook, Twitter, Apple)
- **Passkeys** (ASP.NET Core 10 built-in support)
- **JWT signing key rotation** without downtime
- **Refresh token rotation** with one-time use enforcement
- **Session management** for both web and app clients

## Architecture

### Core Services

#### 1. **IJwtSigningKeyStore** - Key Rotation Management
- Handles signing key lifecycle
- Supports multiple valid keys for validation while only one signs new tokens
- Keys are persisted in the database for durability
- Includes caching for performance
- **Implementation**: `JwtSigningKeyStore`

#### 2. **IJwtTokenService** - Access Token Generation
- Generates short-lived access tokens (default: 5 minutes)
- Includes minimal claims (sub, email, jti)
- Uses the current signing key with `kid` header for validation
- Validates tokens against all valid keys (supports rotation)
- **Implementation**: `JwtTokenService`

#### 3. **IRefreshTokenService** - Session Token Management
- Generates cryptographically secure refresh tokens (64 bytes)
- Stores hashed tokens (SHA-256) - never plaintext
- Enforces one-time use through rotation
- Tracks client type, IP address, and user agent for security
- Revokes all user tokens on logout
- **Implementation**: `RefreshTokenService`

### Database Entities

#### RefreshToken
```csharp
public class RefreshToken
{
    public string Id { get; set; }
    public string UserId { get; set; }
    public string TokenHash { get; set; }  // SHA-256 hash
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenId { get; set; }
    public string ClientType { get; set; }  // "web", "mobile", "desktop"
    public string? IssuedFromIpAddress { get; set; }
    public string? UserAgent { get; set; }
}
```

#### JwtSigningKey
```csharp
public class JwtSigningKey
{
    public string Id { get; set; }
    public string KeyId { get; set; }  // kid header value
    public string KeyMaterial { get; set; }  // Base64 encoded
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public bool IsCurrentSigningKey { get; set; }
    public string Algorithm { get; set; }  // "HS256"
}
```

## Authentication Flows

### 1. Web (BFF) - Cookie Authentication

#### Login
```
POST /api/auth/login-web
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "rememberMe": false
}
```

**Response (Success)**:
```json
{
  "success": true,
  "message": "Login successful",
  "userId": "user-id",
  "email": "user@example.com"
}
```

The response includes an HttpOnly, Secure, SameSite=Strict cookie that the browser automatically sends with subsequent requests.

#### Logout
```
POST /api/auth/logout
Authorization: Bearer {cookie}
```

Response:
```json
{
  "success": true,
  "message": "Logout successful"
}
```

### 2. App (MAUI) - JWT + Refresh Token

#### Login
```
POST /api/auth/login-app
Content-Type: application/json

{
  "email": "user@example.com",
  "password": "SecurePassword123!",
  "clientType": "mobile",
  "deviceId": "optional-device-id"
}
```

**Response (Success)**:
```json
{
  "success": true,
  "message": "Login successful",
  "accessToken": "eyJhbGc...",
  "refreshToken": "base64-encoded-token",
  "accessTokenExpiresIn": 300,
  "tokenType": "Bearer",
  "userId": "user-id",
  "email": "user@example.com"
}
```

**Token Details**:
- Access Token: JWT with 5-minute lifetime
- Refresh Token: 64-byte random value, 30-day lifetime
- Both are one-time use (refresh rotates on each use)

#### Refresh Token
```
POST /api/auth/refresh
Content-Type: application/json

{
  "refreshToken": "base64-encoded-token"
}
```

**Response**:
```json
{
  "success": true,
  "message": "Token refreshed",
  "accessToken": "eyJhbGc...",
  "refreshToken": "new-base64-encoded-token",
  "accessTokenExpiresIn": 300
}
```

**Security**: 
- Old refresh token is automatically marked as replaced
- Issuing a duplicate refresh token is rejected
- IP address and User-Agent are logged for audit

#### Logout (Revoke All Sessions)
```
POST /api/auth/logout-app
Authorization: Bearer {accessToken}
```

Response:
```json
{
  "success": true,
  "message": "Logout successful"
}
```

### 3. Social Login - OAuth 2.0

#### Initiate (Works for both web and app)
```
GET /api/auth/external/challenge/{provider}?returnUrl=/&clientType=web
// Redirects to provider (Google, Microsoft, etc.)
```

#### Web Callback
```
GET /api/auth/external/callback?code=...&state=...
// Returns: Sets HttpOnly cookie, redirects to returnUrl
```

#### App Callback
```
GET /api/auth/external/app-callback?code=...&state=...
// Returns: JSON with JWT + refresh token
{
  "success": true,
  "accessToken": "eyJhbGc...",
  "refreshToken": "base64...",
  "accessTokenExpiresIn": 300,
  "userId": "user-id",
  "email": "user@example.com"
}
```

### 4. Session Management

#### Get Active Sessions
```
GET /api/auth/sessions
Authorization: Bearer {token or cookie}
```

**Response**:
```json
{
  "success": true,
  "sessions": [
    {
      "id": "session-id",
      "clientType": "mobile",
      "createdAt": "2026-01-31T10:51:12Z",
      "ipAddress": "192.168.1.1"
    }
  ]
}
```

#### Revoke Specific Session
```
POST /api/auth/sessions/{sessionId}/revoke
Authorization: Bearer {token or cookie}
```

## Configuration

Add to `appsettings.json`:

```json
{
  "Auth": {
    "AccessTokenLifetimeSeconds": 300,
    "RefreshTokenLifetimeDays": 30,
    "Jwt": {
      "Issuer": "Electra",
      "Audience": "ElectraClients"
    }
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    },
    "Facebook": {
      "AppId": "your-facebook-app-id",
      "AppSecret": "your-facebook-app-secret"
    },
    "Twitter": {
      "ConsumerKey": "your-twitter-consumer-key",
      "ConsumerSecret": "your-twitter-consumer-secret"
    }
  }
}
```

## Startup Configuration

In `Program.cs`:

```csharp
using Electra.Auth.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddElectraAuthentication(builder.Environment, builder.Configuration);

var app = builder.Build();

// Initialize JWT signing keys
await app.Services.InitializeJwtSigningKeysAsync();

// Configure middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
```

## Key Rotation Process

### Manual Key Rotation

```csharp
var signingKeyStore = serviceProvider.GetRequiredService<IJwtSigningKeyStore>();
var newKeyId = await signingKeyStore.RotateSigningKeyAsync();
```

### How It Works

1. Current signing key is marked as `IsCurrentSigningKey = false`
2. New key is generated and marked as `IsCurrentSigningKey = true`
3. Both keys remain valid for token validation (old key for existing tokens, new for new)
4. After old tokens expire naturally, old key can be revoked
5. No service restart required

### Token Validation

- JWT headers include `kid` (key ID)
- Validator fetches the key by `kid` from the store
- Falls back to all valid keys if `kid` is missing
- Supports gradual key rotation without downtime

## Database Migrations

After adding this authentication system, create migrations:

```bash
# From the Persistence project directory
dotnet ef migrations add AddAuthenticationTokens --context ElectraDbContext
dotnet ef database update --context ElectraDbContext
```

Or from the project root:

```bash
dotnet ef migrations add AddAuthenticationTokens -p src/Electra.Persistence -s src/Electra.Auth
```

## Security Features

### Token Security
- ✅ Refresh tokens stored as SHA-256 hashes (never plaintext)
- ✅ Access tokens are short-lived (5 minutes default)
- ✅ Refresh token rotation enforced (one-time use)
- ✅ All timestamps in UTC for consistency
- ✅ IP address and User-Agent logged for audit

### Cookie Security (Web)
- ✅ HttpOnly: Prevents JavaScript access
- ✅ Secure: HTTPS only in production
- ✅ SameSite=Strict: CSRF protection
- ✅ Automatic expiration

### Cryptography
- ✅ HMAC-SHA256 for JWT signing
- ✅ CSPRNG (RNGCryptoServiceProvider) for random values
- ✅ Base64 encoding for transport

## MAUI Mobile Client Integration

```csharp
// Store tokens securely
SecureStorage.Default.SetAsync("access_token", response.AccessToken);
SecureStorage.Default.SetAsync("refresh_token", response.RefreshToken);

// Add authorization header
httpClient.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", accessToken);

// Handle 401 responses - refresh token
if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
{
    var newTokens = await authService.RefreshTokenAsync(refreshToken);
    // Update stored tokens and retry request
}

// Logout
await authService.LogoutAppAsync(accessToken);
```

## Monitoring & Logging

The authentication system logs:
- User login/logout events
- Token generation and validation
- Key rotation events
- Failed login attempts
- Token refresh operations
- Session revocation

Enable structured logging in `appsettings.json`:

```json
{
  "Serilog": {
    "MinimumLevel": "Debug",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/auth-.txt", "rollingInterval": "Day" } }
    ]
  }
}
```

## Testing

### Manual Testing

```bash
# Login as web client
curl -X POST https://localhost:5001/api/auth/login-web \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123","rememberMe":false}' \
  -c cookies.txt

# Use web session
curl https://localhost:5001/api/auth/sessions -b cookies.txt

# Login as app
curl -X POST https://localhost:5001/api/auth/login-app \
  -H "Content-Type: application/json" \
  -d '{"email":"user@example.com","password":"password123","clientType":"mobile"}'

# Refresh token
curl -X POST https://localhost:5001/api/auth/refresh \
  -H "Content-Type: application/json" \
  -d '{"refreshToken":"token-value"}'
```

### Unit Testing

The services are designed for easy testing:

```csharp
[Test]
public async Task RefreshToken_ShouldRotateToken()
{
    var oldToken = await _service.GenerateRefreshTokenAsync("user-1", "mobile");
    var newToken = await _service.RotateRefreshTokenAsync(oldToken, "mobile");
    
    var validUserId = await _service.ValidateRefreshTokenAsync(newToken);
    Assert.That(validUserId, Is.EqualTo("user-1"));
    
    // Old token should be invalid
    var oldValid = await _service.ValidateRefreshTokenAsync(oldToken);
    Assert.That(oldValid, Is.Null);
}
```

## Future Enhancements

The design supports future evolution:

- **OpenIddict**: Can be added without rewriting Identity layer
- **Per-device refresh tokens**: Already track device info
- **Adaptive authentication**: Can add risk-based rules
- **FIDO2/WebAuthn**: Passkey support via Identity
- **Zero-trust**: Claims can be extended for microservices
- **Hardware security modules**: Key material can be offloaded to HSM
- **Rate limiting**: Can be added via middleware
- **Biometric authentication**: MAUI supports native biometrics

## Troubleshooting

### Issue: "No current signing key found"
**Solution**: Call `InitializeJwtSigningKeysAsync()` on startup

### Issue: Token validation fails after key rotation
**Solution**: Ensure old key remains valid during rotation period

### Issue: Refresh token always invalid
**Solution**: Check that RefreshToken table has been migrated and created

### Issue: CORS errors on external login callback
**Solution**: Ensure callback URL is registered with OAuth provider

## Related Documentation

- [ASP.NET Core Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
- [JWT Bearer Authentication](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/jwt-authn)
- [BFF Security Pattern](https://www.bff-architecture.org/)
- [OAuth 2.0 Security Best Practices](https://datatracker.ietf.org/doc/html/draft-ietf-oauth-security-topics)
