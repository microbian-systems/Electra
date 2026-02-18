# Aero.Social - Social Media Provider Library

A comprehensive C# library for posting content to 29+ social media platforms through a unified interface.

## Features

- **29 Social Media Providers**: Post to Discord, Slack, Telegram, LinkedIn, Facebook, X (Twitter), Instagram, TikTok, YouTube, and more
- **Unified Interface**: All providers implement `ISocialProvider` for consistent usage
- **OAuth Integration**: Built-in support for OAuth 1.0a, OAuth 2.0, and custom authentication flows
- **Media Support**: Upload images and videos via URLs
- **Error Handling**: Automatic retry logic, rate limiting detection, and token refresh
- **Analytics**: Platform-specific analytics support
- **Plugs System**: Post-processing hooks for automated workflows

## Installation

Add the project reference to your `.csproj`:

```xml
<ItemGroup>
  <ProjectReference Include="..\path\to\social-providers\src\Aero.Social.csproj" />
</ItemGroup>
```

_NuGet package coming soon_

## Quick Start

### 1. Register Services

```csharp
using Aero.Social;

// In Program.cs or Startup.cs
services.AddSocialProviders();
```

### 2. Configure Environment Variables

Each provider requires specific environment variables. Common patterns:

```bash
# OAuth 2.0 providers
LINKEDIN_CLIENT_ID=your_client_id
LINKEDIN_CLIENT_SECRET=your_client_secret

# Bot token providers
DISCORD_CLIENT_ID=your_client_id
DISCORD_CLIENT_SECRET=your_client_secret
TELEGRAM_TOKEN=your_bot_token

# API key providers
MEDIUM_TOKEN=your_integration_token
```

### 3. Post Content

```csharp
using Aero.Social;
using Aero.Social.Abstractions;
using Aero.Social.Models;

public class SocialPostingService
{
    private readonly IntegrationManager _integrationManager;

    public SocialPostingService(IntegrationManager integrationManager)
    {
        _integrationManager = integrationManager;
    }

    public async Task<PostResponse[]> PostToDiscordAsync(
        string accessToken,
        string message,
        List<MediaContent>? media = null)
    {
        var provider = _integrationManager.GetSocialIntegration("discord");

        var post = new PostDetails
        {
            Id = Guid.NewGuid().ToString(),
            Message = message,
            Media = media
        };

        var integration = new Integration
        {
            Id = "user_id",
            Token = accessToken
        };

        return await provider.PostAsync(
            integration.InternalId,
            accessToken,
            new List<PostDetails> { post },
            integration
        );
    }
}
```

### 4. OAuth Flow

```csharp
public async Task<string> StartOAuthFlowAsync()
{
    var provider = _integrationManager.GetSocialIntegration("linkedin");

    // Generate authorization URL
    var authResponse = await provider.GenerateAuthUrlAsync();

    // Store state and codeVerifier for later verification
    StoreInSession("oauth_state", authResponse.State);
    StoreInSession("code_verifier", authResponse.CodeVerifier);

    // Redirect user to authResponse.Url
    return authResponse.Url;
}

public async Task<AuthTokenDetails> CompleteOAuthFlowAsync(string code, string state)
{
    var provider = _integrationManager.GetSocialIntegration("linkedin");

    var storedState = GetFromSession("oauth_state");
    var codeVerifier = GetFromSession("code_verifier");

    // Verify state matches (CSRF protection)
    if (state != storedState)
        throw new InvalidOperationException("Invalid state");

    // Exchange code for tokens
    var tokenDetails = await provider.AuthenticateAsync(
        new AuthenticateParams(code, codeVerifier)
    );

    return tokenDetails;
}
```

### 5. Error Handling

```csharp
try
{
    var response = await provider.PostAsync(id, token, posts, integration);
}
catch (RefreshTokenException)
{
    // Token expired - need to refresh or re-authenticate
    var newTokens = await provider.RefreshTokenAsync(refreshToken);
}
catch (BadBodyException ex)
{
    // Invalid request - check ex.Response for details
    Console.WriteLine($"Error: {ex.Response}");
}
catch (NotEnoughScopesException)
{
    // OAuth scopes insufficient for this operation
    // User needs to re-authorize with additional scopes
}
```

## Supported Providers

| Provider             | Identifier             | Auth Type            | Features                   |
| -------------------- | ---------------------- | -------------------- | -------------------------- |
| Discord              | `discord`              | Bot Token            | Posts, Comments            |
| Slack                | `slack`                | OAuth 2.0            | Posts                      |
| Telegram             | `telegram`             | Bot Token            | Posts, Comments            |
| LinkedIn             | `linkedin`             | OAuth 2.0            | Posts, Analytics           |
| LinkedIn Page        | `linkedin-page`        | OAuth 2.0            | Posts, Analytics           |
| Facebook             | `facebook`             | OAuth 2.0            | Posts, Comments, Analytics |
| Instagram            | `instagram`            | OAuth 2.0 (Facebook) | Posts, Comments, Analytics |
| Instagram Standalone | `instagram-standalone` | OAuth 2.0            | Posts, Comments, Analytics |
| X (Twitter)          | `x`                    | OAuth 1.0a           | Posts, Comments            |
| Reddit               | `reddit`               | OAuth 2.0            | Posts                      |
| TikTok               | `tiktok`               | OAuth 2.0            | Posts                      |
| YouTube              | `youtube`              | OAuth 2.0            | Posts, Comments            |
| Threads              | `threads`              | OAuth 2.0            | Posts                      |
| Pinterest            | `pinterest`            | OAuth 2.0            | Posts, Analytics           |
| Twitch               | `twitch`               | OAuth 2.0            | Posts, Comments            |
| Dribbble             | `dribbble`             | OAuth 2.0            | Posts                      |
| Kick                 | `kick`                 | OAuth 2.0 + PKCE     | Posts                      |
| Google My Business   | `gmb`                  | OAuth 2.0            | Posts                      |
| Vk                   | `vk`                   | OAuth 2.0 + PKCE     | Posts                      |
| Mastodon             | `mastodon`             | OAuth 2.0            | Posts, Comments            |
| Bluesky              | `bluesky`              | AT Protocol          | Posts, Comments            |
| Farcaster            | `wrapcast`             | Neynar SDK           | Posts                      |
| Medium               | `medium`               | API Key              | Posts                      |
| Dev.to               | `devto`                | API Key              | Posts                      |
| Hashnode             | `hashnode`             | API Key (GraphQL)    | Posts                      |
| Lemmy                | `lemmy`                | JWT                  | Posts, Comments            |
| Nostr                | `nostr`                | Web3 (HEX keys)      | Posts, Comments            |
| WordPress            | `wordpress`            | Basic Auth           | Posts                      |
| Listmonk             | `listmonk`             | Basic Auth           | Posts                      |

## Configuration Reference

### Required Environment Variables

```bash
# Frontend URL (required for OAuth redirects)
FRONTEND_URL=https://yourdomain.com

# Provider-specific (example for Discord)
DISCORD_CLIENT_ID=your_client_id
DISCORD_CLIENT_SECRET=your_client_secret

# Provider-specific (example for LinkedIn)
LINKEDIN_CLIENT_ID=your_client_id
LINKEDIN_CLIENT_SECRET=your_client_secret

# Provider-specific (example for X/Twitter)
X_API_KEY=your_api_key
X_API_SECRET=your_api_secret
```

## Advanced Usage

### Custom Provider

Create a custom provider by inheriting from `SocialProviderBase`:

```csharp
using Aero.Social.Abstractions;
using Aero.Social.Models;

public class MyCustomProvider : SocialProviderBase
{
    public override string Identifier => "my-custom";
    public override string Name => "My Custom Platform";
    public override string[] Scopes => new[] { "read", "write" };

    public MyCustomProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MyCustomProvider> logger)
        : base(httpClient, logger)
    {
    }

    public override int MaxLength(object? additionalSettings = null) => 5000;

    public override async Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        // Implement OAuth URL generation
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        // Implement token exchange
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        // Implement posting logic
    }
}
```

### Using Plugs (Post-Processing Hooks)

```csharp
using Aero.Social.Plugs;

public class MyProvider : SocialProviderBase
{
    [PostPlug(
        Identifier = "auto-repost",
        Title = "Auto Repost",
        Description = "Automatically repost when post reaches X likes",
        Fields = new[] { "likes_threshold" }
    )]
    public async Task<PlugExecutionResult> AutoRepost(
        PlugExecutionContext context,
        Dictionary<string, object>? fieldValues,
        CancellationToken cancellationToken = default)
    {
        var threshold = (int)fieldValues?["likes_threshold"]!;

        // Check if post reached threshold
        if (context.CurrentLikes >= threshold)
        {
            // Repost logic here
        }

        return PlugExecutionResult.SuccessResult();
    }
}
```

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## License

MIT License - see LICENSE file for details.
