using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aero.Social.Tests.Infrastructure;

public static class MockResponses
{
    public static class OAuth2
    {
        public static string TokenResponse(string accessToken, string refreshToken = "mock_refresh_token", int expiresIn = 3600)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                expires_in = expiresIn,
                token_type = "Bearer"
            });
        }

        public static string ErrorResponse(string error, string description)
        {
            return JsonSerializer.Serialize(new
            {
                error,
                error_description = description
            });
        }
    }

    public static class Facebook
    {
        public static string TokenResponse(string accessToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                token_type = "bearer",
                expires_in = 5184000
            });
        }

        public static string UserInfoResponse(string id, string name, string picture = "https://example.com/pic.jpg")
        {
            return JsonSerializer.Serialize(new
            {
                id,
                name,
                picture = new
                {
                    data = new { url = picture }
                }
            });
        }

        public static string PagesResponse(params (string Id, string Name, string AccessToken)[] pages)
        {
            return JsonSerializer.Serialize(new
            {
                data = pages.Select(p => new
                {
                    id = p.Id,
                    name = p.Name,
                    access_token = p.AccessToken
                })
            });
        }

        public static string PermissionsResponse(params string[] granted)
        {
            return JsonSerializer.Serialize(new
            {
                data = granted.Select(g => new { permission = g, status = "granted" })
            });
        }

        public static string PostResponse(string postId)
        {
            return JsonSerializer.Serialize(new { id = postId });
        }

        public static string ErrorResponse(int code, string message, string type = "OAuthException")
        {
            return JsonSerializer.Serialize(new
            {
                error = new
                {
                    code,
                    message,
                    type
                }
            });
        }
    }

    public static class LinkedIn
    {
        public static string TokenResponse(string accessToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                expires_in = 5184000,
                refresh_token = "mock_refresh",
                refresh_token_expires_in = 5184000
            });
        }

        public static string UserProfileResponse(string id, string name, string picture = "")
        {
            return JsonSerializer.Serialize(new
            {
                id,
                name = new
                {
                    localized = new { en_US = name }
                },
                profilePicture = new
                {
                    displayImage = picture
                }
            });
        }

        public static string PostResponse(string postId, string urn)
        {
            return JsonSerializer.Serialize(new
            {
                id = postId,
                activityUrn = urn
            });
        }
    }

    public static class Instagram
    {
        public static string TokenResponse(string accessToken, string userId)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                user_id = userId
            });
        }

        public static string UserInfoResponse(string id, string username)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                username,
                account_type = "BUSINESS"
            });
        }

        public static string MediaResponse(string containerId)
        {
            return JsonSerializer.Serialize(new { id = containerId });
        }

        public static string PublishResponse(string mediaId)
        {
            return JsonSerializer.Serialize(new { id = mediaId });
        }
    }

    public static class X
    {
        public static string TokenResponse(string token, string tokenSecret)
        {
            return $"oauth_token={token}&oauth_token_secret={tokenSecret}";
        }

        public static string UserInfoResponse(long id, string name, string screenName)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                name,
                screen_name = screenName
            });
        }

        public static string TweetResponse(long id, string text)
        {
            return JsonSerializer.Serialize(new
            {
                data = new
                {
                    id = id.ToString(),
                    text
                }
            });
        }
    }

    public static class Reddit
    {
        public static string TokenResponse(string accessToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                token_type = "bearer",
                expires_in = 3600,
                scope = "*"
            });
        }

        public static string UserInfoResponse(string name, string id)
        {
            return JsonSerializer.Serialize(new
            {
                name,
                id,
                icon_img = "https://example.com/avatar.png"
            });
        }

        public static string SubmitResponse(string name, string url)
        {
            return JsonSerializer.Serialize(new
            {
                json = new
                {
                    data = new
                    {
                        name,
                        url
                    }
                }
            });
        }
    }

    public static class TikTok
    {
        public static string TokenResponse(string accessToken, string refreshToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                expires_in = 86400,
                token_type = "Bearer"
            });
        }

        public static string UserInfoResponse(string openId, string unionId, string displayName)
        {
            return JsonSerializer.Serialize(new
            {
                data = new
                {
                    user = new
                    {
                        open_id = openId,
                        union_id = unionId,
                        display_name = displayName
                    }
                }
            });
        }
    }

    public static class YouTube
    {
        public static string ChannelResponse(string id, string title)
        {
            return JsonSerializer.Serialize(new
            {
                items = new[]
                {
                    new
                    {
                        id,
                        snippet = new { title },
                        contentDetails = new
                        {
                            relatedPlaylists = new { uploads = $"UU{id}" }
                        }
                    }
                }
            });
        }

        public static string VideoResponse(string id, string title)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                snippet = new { title }
            });
        }
    }

    public static class Discord
    {
        public static string UserInfoResponse(string id, string username, string discriminator = "0001")
        {
            return JsonSerializer.Serialize(new
            {
                id,
                username,
                discriminator,
                avatar = "abc123"
            });
        }

        public static string GuildResponse(string id, string name)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                name,
                icon = "guild_icon"
            });
        }

        public static string ChannelResponse(string id, string name)
        {
            return JsonSerializer.Serialize(new { id, name });
        }

        public static string MessageResponse(string id, string content)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                content,
                channel_id = "123456789"
            });
        }
    }

    public static class Telegram
    {
        public static string MeResponse(string botUsername)
        {
            return JsonSerializer.Serialize(new
            {
                ok = true,
                result = new
                {
                    id = 123456789,
                    is_bot = true,
                    first_name = "Test Bot",
                    username = botUsername
                }
            });
        }

        public static string SendMessageResponse(int messageId, int chatId)
        {
            return JsonSerializer.Serialize(new
            {
                ok = true,
                result = new
                {
                    message_id = messageId,
                    chat = new { id = chatId },
                    date = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                }
            });
        }

        public static string ErrorResponse(int errorCode, string description)
        {
            return JsonSerializer.Serialize(new
            {
                ok = false,
                error_code = errorCode,
                description
            });
        }
    }

    public static class Bluesky
    {
        public static string SessionResponse(string did, string handle, string accessJwt)
        {
            return JsonSerializer.Serialize(new
            {
                did,
                handle,
                accessJwt
            });
        }

        public static string ResolveHandleResponse(string did)
        {
            return JsonSerializer.Serialize(new
            {
                did
            });
        }

        public static string CreateRecordResponse(string uri, string cid)
        {
            return JsonSerializer.Serialize(new
            {
                uri,
                cid
            });
        }
    }

    public static class Mastodon
    {
        public static string InstanceResponse(string domain)
        {
            return JsonSerializer.Serialize(new
            {
                uri = $"https://{domain}",
                version = "4.0.0"
            });
        }

        public static string TokenResponse(string accessToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                token_type = "Bearer",
                scope = "read write push",
                created_at = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
            });
        }

        public static string AccountResponse(string id, string username, string displayName)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                username,
                display_name = displayName,
                avatar = "https://example.com/avatar.png"
            });
        }

        public static string StatusResponse(string id, string content)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                content,
                created_at = DateTime.UtcNow.ToString("O")
            });
        }
    }

    public static class Threads
    {
        public static string TokenResponse(string accessToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                token_type = "bearer"
            });
        }

        public static string UserInfoResponse(string id, string username)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                username,
                threads_profile_picture_url = "https://example.com/pic.jpg"
            });
        }

        public static string ContainerResponse(string id)
        {
            return JsonSerializer.Serialize(new { id });
        }

        public static string PublishResponse(string id)
        {
            return JsonSerializer.Serialize(new { id });
        }
    }

    public static class Pinterest
    {
        public static string TokenResponse(string accessToken, string refreshToken)
        {
            return JsonSerializer.Serialize(new
            {
                access_token = accessToken,
                refresh_token = refreshToken,
                token_type = "bearer",
                expires_in = 3600
            });
        }

        public static string UserResponse(string id, string username)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                username,
                profile_image = "https://example.com/pic.jpg"
            });
        }

        public static string BoardResponse(string id, string name)
        {
            return JsonSerializer.Serialize(new
            {
                id,
                name
            });
        }

        public static string PinResponse(string id)
        {
            return JsonSerializer.Serialize(new { id });
        }
    }
}
