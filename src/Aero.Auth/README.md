# Aero.Auth

Authentication and authorization services for the Aero framework with OpenIddict, JWT, and Passkey support.

## Overview

`Aero.Auth` provides comprehensive authentication and authorization capabilities including JWT token-based auth, OpenIddict OAuth2/OIDC implementation, WebAuthn/Passkey support, and social provider integration.

## Features

- **JWT Authentication** - Token-based authentication for APIs
- **OpenIddict** - Full OAuth2/OpenID Connect implementation
- **Passkey/WebAuthn** - FIDO2 passwordless authentication
- **Social Login** - Google, Microsoft, Facebook, Twitter, Apple, Coinbase OAuth
- **Cookie-based Auth** - Traditional session-based authentication
- **Embedded Views** - Default Razor views for login, register, etc.

## Key Components

### JWT Authentication

#### Token Service

```csharp
public class JwtTokenService
{
    public string GenerateToken(AeroUser user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: DateTime.Now.AddHours(_settings.ExpiryHours),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
```

### OpenIddict Integration

```csharp
public static class OpenIddictExtensions
{
    public static IServiceCollection AddAeroOpenIddict(
        this IServiceCollection services, 
        IConfiguration config)
    {
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                       .UseDbContext<AeroAuthDbContext>();
            })
            .AddServer(options =>
            {
                options.SetTokenEndpointUris("/connect/token");
                options.SetAuthorizationEndpointUris("/connect/authorize");

                options.AllowPasswordFlow();
                options.AllowRefreshTokenFlow();
                options.AllowAuthorizationCodeFlow();

                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                options.UseAspNetCore()
                       .EnableTokenEndpointPassthrough()
                       .EnableAuthorizationEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        return services;
    }
}
```

### Passkey/WebAuthn Support

Requires HTTPS and a valid Relying Party ID:

```json
{
  "Passkey": {
    "RelyingPartyId": "localhost",
    "RelyingPartyName": "Aero Application",
    "Origin": "https://localhost:5001"
  }
}
```

```csharp
public class PasskeyService
{
    public async Task<CredentialCreateOptions> BeginRegistrationAsync(string userId)
    {
        var user = await _userRepository.FindByIdAsync(userId);
        
        var options = _fido2.RequestNewCredential(
            new Fido2User
            {
                Id = Encoding.UTF8.GetBytes(user.Id),
                Name = user.Email,
                DisplayName = user.UserName
            },
            new List<PublicKeyCredentialDescriptor>(),
            AuthenticatorSelection.Default,
            AttestationConveyancePreference.None);

        return options;
    }

    public async Task<RegisteredPasskey> CompleteRegistrationAsync(
        string userId, 
        AuthenticatorAttestationRawResponse attestationResponse)
    {
        var result = await _fido2.MakeNewCredentialAsync(
            attestationResponse, 
            options, 
            async (_, __) => true);

        return new RegisteredPasskey
        {
            UserId = userId,
            CredentialId = result.Result.CredentialId,
            PublicKey = result.Result.PublicKey,
            SignCount = result.Result.Counter
        };
    }
}
```

### Social Authentication

```csharp
public static class SocialAuthExtensions
{
    public static AuthenticationBuilder AddAeroSocialAuth(
        this AuthenticationBuilder builder,
        IConfiguration config)
    {
        builder.AddGoogle(options =>
        {
            options.ClientId = config["Authentication:Google:ClientId"]!;
            options.ClientSecret = config["Authentication:Google:ClientSecret"]!;
        });

        builder.AddFacebook(options =>
        {
            options.AppId = config["Authentication:Facebook:AppId"]!;
            options.AppSecret = config["Authentication:Facebook:AppSecret"]!;
        });

        builder.AddMicrosoftAccount(options =>
        {
            options.ClientId = config["Authentication:Microsoft:ClientId"]!;
            options.ClientSecret = config["Authentication:Microsoft:ClientSecret"]!;
        });

        builder.AddTwitter(options =>
        {
            options.ConsumerKey = config["Authentication:Twitter:ConsumerKey"]!;
            options.ConsumerSecret = config["Authentication:Twitter:ConsumerSecret"]!;
        });

        builder.AddApple(options =>
        {
            options.ClientId = config["Authentication:Apple:ClientId"]!;
            options.KeyId = config["Authentication:Apple:KeyId"]!;
            options.TeamId = config["Authentication:Apple:TeamId"]!;
        });

        return builder;
    }
}
```

## Views

The library includes default Razor views embedded as resources:

- **Login**: `/login` - Username/password login
- **Register**: `/register` - User registration
- **Forgot Password**: `/forgot-password` - Password reset request
- **Reset Password**: `/reset-password` - Password reset confirmation
- **Passkey Login**: `/login-passkey` - WebAuthn/Passkey login
- **Passkey Registration**: `/register-passkey` - Passkey registration

### Customizing Views

Views can be overridden by placing view files in the consuming application:

```
Views/
  Auth/
    Login.cshtml
    Register.cshtml
    ForgotPassword.cshtml
    LoginPasskey.cshtml
    RegisterPasskey.cshtml
```

## Configuration

### Program.cs Setup

```csharp
builder.Services.AddAeroAuthentication(builder.Configuration);
builder.Services.AddAeroAuthorization();

// Extension methods
public static class AuthServiceExtensions
{
    public static IServiceCollection AddAeroAuthentication(
        this IServiceCollection services, 
        IConfiguration config)
    {
        services.Configure<JwtSettings>(config.GetSection("Jwt"));
        
        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = config["Jwt:Issuer"],
                ValidAudience = config["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(config["Jwt:Key"]!))
            };
        })
        .AddAeroSocialAuth(config);

        services.AddAeroOpenIddict(config);

        // FIDO2/Passkey
        services.AddFido2(options =>
        {
            options.ServerDomain = config["Passkey:RelyingPartyId"]!;
            options.ServerName = config["Passkey:RelyingPartyName"]!;
            options.Origin = config["Passkey:Origin"]!;
        });

        return services;
    }

    public static IServiceCollection AddAeroAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy =>
                policy.RequireRole("Admin"));

            options.AddPolicy("VerifiedUser", policy =>
                policy.RequireClaim("email_verified", "true"));
        });

        return services;
    }
}
```

### appsettings.json

```json
{
  "Jwt": {
    "Key": "your-secret-key-min-32-chars-long!",
    "Issuer": "AeroAuth",
    "Audience": "AeroApp",
    "ExpiryHours": 24
  },
  "Authentication": {
    "Google": {
      "ClientId": "your-google-client-id",
      "ClientSecret": "your-google-client-secret"
    },
    "Microsoft": {
      "ClientId": "your-microsoft-client-id",
      "ClientSecret": "your-microsoft-client-secret"
    }
  },
  "Passkey": {
    "RelyingPartyId": "localhost",
    "RelyingPartyName": "Aero Application",
    "Origin": "https://localhost:5001"
  }
}
```

## Controllers

### Auth Controller

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login([FromBody] LoginRequest request)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email);
        if (user == null || !await _userRepository.CheckPasswordAsync(user, request.Password))
            return Unauthorized(new { Error = "Invalid credentials" });

        var token = _tokenService.GenerateToken(user, user.Roles);
        var refreshToken = await _userRepository.GenerateRefreshTokenAsync(user);

        return Ok(new AuthResponse
        {
            Token = token,
            RefreshToken = refreshToken,
            ExpiresIn = 3600
        });
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponse>> Refresh([FromBody] RefreshRequest request)
    {
        var user = await _userRepository.FindByRefreshTokenAsync(request.RefreshToken);
        if (user == null || user.RefreshTokenExpiry < DateTime.UtcNow)
            return Unauthorized(new { Error = "Invalid refresh token" });

        var newToken = _tokenService.GenerateToken(user, user.Roles);
        return Ok(new AuthResponse { Token = newToken });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userId != null)
        {
            await _userRepository.RevokeRefreshTokensAsync(userId);
        }
        return Ok(new { Message = "Logged out successfully" });
    }
}
```

## Best Practices

1. **Secure Token Storage** - Never store tokens in localStorage; use httpOnly cookies
2. **Refresh Tokens** - Implement secure refresh token rotation
3. **Rate Limiting** - Apply rate limits to auth endpoints
4. **Account Lockout** - Implement account lockout after failed attempts
5. **Email Verification** - Require email verification before account activation
6. **Strong Passwords** - Enforce password complexity requirements
7. **HTTPS Only** - Always use HTTPS, especially for Passkeys

## Related Packages

- `Aero.Web` - Web framework integration
- `Aero.Web.Core` - Core web utilities
- `Aero.RavenDB` - User storage
- `Aero.Validators` - Input validation
