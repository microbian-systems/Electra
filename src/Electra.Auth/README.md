# Electra.Auth - Authentication Microservice

A complete .NET 9 authentication microservice implementing OpenIddict, ASP.NET Core Identity, WebAuthn.Net (Passkeys), and social login providers.

## 🚀 Features

- **Multi-Factor Authentication**
  - Username/Password authentication
  - Passkey (WebAuthn) support
  - Social login (Google, Facebook, Microsoft, Twitter)

- **Token Management**
  - JWT access tokens (15-minute expiry)
  - Refresh tokens (2-minute expiry with rotation)
  - OpenID Connect compliant

- **Security Features**
  - Account lockout protection
  - Refresh token rotation and invalidation
  - CORS support for SPA applications
  - Secure token storage

- **Infrastructure**
  - PostgreSQL database with Entity Framework Core
  - YARP reverse proxy for API routing
  - Comprehensive logging with Serilog
  - Swagger documentation

## 🏗️ Architecture

### Controllers

- **AuthController** (`/api/auth`)
  - User registration and login
  - OpenIddict token endpoint (`/connect/token`)
  - User info endpoint (`/connect/userinfo`)
  - Password-based authentication

- **PasskeyController** (`/api/passkey`)
  - Passkey registration and authentication
  - WebAuthn credential management
  - Biometric authentication support

- **ExternalLoginController** (`/api/externallogin`)
  - Social login provider integration
  - External account linking/unlinking
  - Provider discovery

### Authentication Flows

1. **Password Flow**
   ```
   POST /api/auth/login
   POST /connect/token (grant_type=password)
   ```

2. **Passkey Flow**
   ```
   POST /api/passkey/authenticate/begin
   POST /api/passkey/authenticate/complete
   ```

3. **Social Login Flow**
   ```
   GET /api/externallogin/challenge/{provider}
   GET /api/externallogin/callback
   ```

4. **Refresh Token Flow**
   ```
   POST /connect/token (grant_type=refresh_token)
   ```

## 🔧 Configuration

### Using the New Service Registration

```csharp
using Electra.Auth.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add comprehensive authentication services with automatic environment-based configuration
builder.Services.AddElectraAuthentication(builder.Environment, builder.Configuration);

var app = builder.Build();
```

### Database Connection
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=electra_auth;Username=postgres;Password=password;"
  }
}
```

**Note**: In development, the system automatically uses an in-memory database. In production, it uses PostgreSQL.

### Social Login Providers
```json
{
  "Auth": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "Facebook": {
      "AppId": "your-facebook-app-id", 
      "AppSecret": "your-facebook-app-secret"
    }
  }
}
```

### WebAuthn Configuration
```json
{
  "WebAuthn": {
    "RelyingPartyId": "localhost",
    "RelyingPartyName": "Electra Auth Service",
    "Origins": ["https://localhost:7001"]
  }
}
```

## 🚀 Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 15+
- (Optional) Docker for development dependencies

### Development Setup

1. **Start Dependencies**
   ```bash
   docker-compose up -d  # Starts PostgreSQL, Redis, etc.
   ```

2. **Update Configuration**
   - Copy `appsettings.Development.json.example` to `appsettings.Development.json`
   - Configure database connection string
   - Add social login provider credentials

3. **Run Database Migrations**
   ```bash
   dotnet ef database update
   ```

4. **Start the Service**
   ```bash
   dotnet run
   ```

5. **Access Swagger UI**
   - Navigate to `https://localhost:7001/swagger`

### Production Deployment

1. **Configure Production Settings**
   - Use proper SSL certificates for OpenIddict
   - Configure production database connection
   - Set appropriate CORS origins
   - Configure external provider credentials

2. **Database Setup**
   ```bash
   dotnet ef database update --environment Production
   ```

## 📡 API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Password login
- `POST /connect/token` - OpenIddict token endpoint
- `GET /connect/userinfo` - User information
- `POST /connect/revoke` - Revoke tokens

### Passkeys
- `POST /api/passkey/register/begin` - Start passkey registration
- `POST /api/passkey/register/complete` - Complete passkey registration
- `POST /api/passkey/authenticate/begin` - Start passkey authentication
- `POST /api/passkey/authenticate/complete` - Complete passkey authentication

### External Login
- `GET /api/externallogin/providers` - Available providers
- `GET /api/externallogin/challenge/{provider}` - Initiate external login
- `GET /api/externallogin/callback` - Handle external login callback

## 🔐 Token Management

### Access Tokens
- **Lifetime**: 15 minutes
- **Type**: JWT with user claims and roles
- **Usage**: Authorize API requests

### Refresh Tokens
- **Lifetime**: 2 minutes (configurable)
- **Rotation**: New refresh token issued on each use
- **Security**: Previous tokens are invalidated

### Example Token Request
```bash
curl -X POST https://localhost:7001/connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "grant_type=password&username=user@example.com&password=password123"
```

## 🛡️ Security Features

- **Password Requirements**: Configurable complexity rules
- **Account Lockout**: 5 failed attempts = 5-minute lockout
- **Token Security**: Reference refresh tokens, short-lived access tokens
- **CORS**: Configured for specific origins
- **HTTPS**: Required in production
- **Audit Logging**: All authentication events logged

## 🔗 Integration Examples

### Frontend Integration (JavaScript)
```javascript
// Login with password
const response = await fetch('https://localhost:7001/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ email: 'user@example.com', password: 'password123' })
});

// Use refresh token
const tokenResponse = await fetch('https://localhost:7001/connect/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  body: 'grant_type=refresh_token&refresh_token=' + refreshToken
});
```

### API Client Configuration
```csharp
services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.Authority = "https://localhost:7001";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidTypes = new[] { "at+jwt" }
        };
    });
```

## 🧪 Testing

### Unit Tests
```bash
dotnet test
```

### Manual Testing with Swagger
1. Navigate to `https://localhost:7001/swagger`
2. Register a new user via `/api/auth/register`
3. Login via `/api/auth/login`
4. Test passkey registration and authentication
5. Test social login flows

## 📊 Monitoring & Logging

- **Serilog**: Structured logging to console and files
- **OpenTelemetry**: Distributed tracing support
- **Health Checks**: Built-in health monitoring
- **Metrics**: Performance and security metrics

## 🔄 Extending Scopes and Claims

### Adding Custom Scopes
```csharp
// In Program.cs
options.RegisterScopes("custom-scope", "admin-panel");

// In token generation
principal.SetScopes(new[] { "openid", "email", "custom-scope" });
```

### Custom Claims
```csharp
// Add custom claims in AuthController
identity.AddClaim(new Claim("tenant_id", user.TenantId));
identity.AddClaim(new Claim("subscription_level", user.SubscriptionLevel));
```

### Role-Based Authorization
```csharp
[Authorize(Roles = "Admin")]
[Authorize(Policy = "RequireSubscription")]
```

## 🤝 Contributing

1. Follow existing code conventions
2. Add comprehensive tests for new features
3. Update documentation for API changes
4. Ensure security best practices

## 📄 License

This project is part of the Electra platform ecosystem.