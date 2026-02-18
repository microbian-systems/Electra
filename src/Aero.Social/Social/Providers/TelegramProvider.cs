using System.Text.Json;
using System.Text.Json.Serialization;
using Aero.Social.Abstractions;
using Aero.Social.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Aero.Social.Providers;

public class TelegramProvider : SocialProviderBase
{
    private readonly IConfiguration _configuration;

    public override string Identifier => "telegram";
    public override string Name => "Telegram";
    public override string[] Scopes => Array.Empty<string>();
    public override EditorType Editor => EditorType.Html;
    public override bool IsWeb3 => true;
    public override int MaxConcurrentJobs => 3;

    public TelegramProvider(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<TelegramProvider> logger)
        : base(httpClient, logger)
    {
        _configuration = configuration;
    }

    public override int MaxLength(object? additionalSettings = null) => 4096;

    public override Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AuthTokenDetails
        {
            RefreshToken = string.Empty,
            ExpiresIn = 0,
            AccessToken = string.Empty,
            Id = string.Empty,
            Name = string.Empty,
            Picture = string.Empty,
            Username = string.Empty
        });
    }

    public override Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var state = MakeId(17);
        return Task.FromResult(new GenerateAuthUrlResponse
        {
            Url = state,
            CodeVerifier = MakeId(10),
            State = state
        });
    }

    public override async Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default)
    {
        var chatId = parameters.Code;

        var chat = await GetChatAsync(chatId, cancellationToken);

        if (string.IsNullOrEmpty(chat?.Id))
        {
            throw new InvalidOperationException("No chat found");
        }

        var photo = string.Empty;
        if (!string.IsNullOrEmpty(chat.Photo?.BigFileId))
        {
            photo = await GetFileLinkAsync(chat.Photo.BigFileId, cancellationToken);
        }

        var id = !string.IsNullOrEmpty(chat.Username) ? chat.Username : chat.Id.ToString();

        return new AuthTokenDetails
        {
            Id = id,
            Name = chat.Title ?? string.Empty,
            AccessToken = chat.Id.ToString(),
            RefreshToken = string.Empty,
            ExpiresIn = (int)TimeSpan.FromDays(200 * 365).TotalSeconds,
            Picture = photo,
            Username = chat.Username ?? string.Empty
        };
    }

    public override async Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var firstPost = posts.First();
        var messageId = await SendMessageAsync(accessToken, firstPost, null, cancellationToken);

        if (messageId.HasValue)
        {
            var releaseUrl = id != "undefined"
                ? $"https://t.me/{id}/{messageId}"
                : $"https://t.me/c/{accessToken.TrimStart('-').TrimStart('1', '0', '0')}/{messageId}";

            return new[]
            {
                new PostResponse
                {
                    Id = firstPost.Id,
                    PostId = messageId.Value.ToString(),
                    ReleaseUrl = releaseUrl,
                    Status = "completed"
                }
            };
        }

        return Array.Empty<PostResponse>();
    }

    public override async Task<PostResponse[]?> CommentAsync(
        string id,
        string postId,
        string? lastCommentId,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        var commentPost = posts.First();
        var replyToId = int.Parse(lastCommentId ?? postId);

        var messageId = await SendMessageAsync(accessToken, commentPost, replyToId, cancellationToken);

        if (messageId.HasValue)
        {
            var releaseUrl = id != "undefined"
                ? $"https://t.me/{id}/{messageId}"
                : $"https://t.me/c/{accessToken.TrimStart('-').TrimStart('1', '0', '0')}/{messageId}";

            return new[]
            {
                new PostResponse
                {
                    Id = commentPost.Id,
                    PostId = messageId.Value.ToString(),
                    ReleaseUrl = releaseUrl,
                    Status = "completed"
                }
            };
        }

        return Array.Empty<PostResponse>();
    }

    private async Task<int?> SendMessageAsync(
        string chatId,
        PostDetails message,
        int? replyToMessageId,
        CancellationToken cancellationToken)
    {
        var text = StripHtmlTags(message.Message);
        var mediaFiles = message.Media ?? new List<MediaContent>();

        if (mediaFiles.Count == 0)
        {
            return await SendTextMessageAsync(chatId, text, replyToMessageId, cancellationToken);
        }

        if (mediaFiles.Count == 1)
        {
            return await SendSingleMediaAsync(chatId, text, mediaFiles[0], replyToMessageId, cancellationToken);
        }

        return await SendMediaGroupAsync(chatId, text, mediaFiles, replyToMessageId, cancellationToken);
    }

    private async Task<int?> SendTextMessageAsync(
        string chatId,
        string text,
        int? replyToMessageId,
        CancellationToken cancellationToken)
    {
        var payload = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            ["text"] = text,
            ["parse_mode"] = "HTML"
        };

        if (replyToMessageId.HasValue)
        {
            payload["reply_to_message_id"] = replyToMessageId.Value;
        }

        var response = await PostToTelegramAsync("sendMessage", payload, cancellationToken);
        var result = await DeserializeAsync<TelegramMessageResponse>(response);
        return result?.Result?.MessageId;
    }

    private async Task<int?> SendSingleMediaAsync(
        string chatId,
        string text,
        MediaContent media,
        int? replyToMessageId,
        CancellationToken cancellationToken)
    {
        var (mediaType, endpoint) = GetMediaTypeAndEndpoint(media.Path);

        var payload = new Dictionary<string, object>
        {
            ["chat_id"] = chatId,
            [mediaType] = media.Path,
            ["caption"] = text,
            ["parse_mode"] = "HTML"
        };

        if (replyToMessageId.HasValue)
        {
            payload["reply_to_message_id"] = replyToMessageId.Value;
        }

        var response = await PostToTelegramAsync(endpoint, payload, cancellationToken);
        var result = await DeserializeAsync<TelegramMessageResponse>(response);
        return result?.Result?.MessageId;
    }

    private async Task<int?> SendMediaGroupAsync(
        string chatId,
        string text,
        List<MediaContent> mediaFiles,
        int? replyToMessageId,
        CancellationToken cancellationToken)
    {
        var chunks = ChunkList(mediaFiles, 10);
        int? firstMessageId = null;

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = chunks[i];
            var media = chunk.Select((m, index) => new Dictionary<string, object?>
            {
                ["type"] = GetMediaType(m.Path),
                ["media"] = m.Path,
                ["caption"] = i == 0 && index == 0 ? text : null,
                ["parse_mode"] = "HTML"
            }).ToList();

            var payload = new Dictionary<string, object?>
            {
                ["chat_id"] = chatId,
                ["media"] = media
            };

            if (replyToMessageId.HasValue && i == 0)
            {
                payload["reply_to_message_id"] = replyToMessageId.Value;
            }

            var response = await PostToTelegramAsync("sendMediaGroup", payload, cancellationToken);
            var result = await DeserializeAsync<TelegramMessagesResponse>(response);

            if (i == 0 && result?.Result?.FirstOrDefault() != null)
            {
                firstMessageId = result.Result.First().MessageId;
            }
        }

        return firstMessageId;
    }

    private async Task<HttpResponseMessage> PostToTelegramAsync(string endpoint, object payload, CancellationToken cancellationToken)
    {
        var botToken = GetBotToken();
        var url = $"https://api.telegram.org/bot{botToken}/{endpoint}";

        var request = CreateRequest(url, HttpMethod.Post, payload);
        var response = await HttpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return response;
    }

    private async Task<TelegramChat?> GetChatAsync(string chatId, CancellationToken cancellationToken)
    {
        var payload = new { chat_id = chatId };
        var response = await PostToTelegramAsync("getChat", payload, cancellationToken);
        var result = await DeserializeAsync<TelegramChatResponse>(response);
        return result?.Result;
    }

    private async Task<string> GetFileLinkAsync(string fileId, CancellationToken cancellationToken)
    {
        var payload = new { file_id = fileId };
        var response = await PostToTelegramAsync("getFile", payload, cancellationToken);
        var result = await DeserializeAsync<TelegramFileResponse>(response);

        if (result?.Result?.FilePath == null)
            return string.Empty;

        var botToken = GetBotToken();
        return $"https://api.telegram.org/file/bot{botToken}/{result.Result.FilePath}";
    }

    private static string StripHtmlTags(string html)
    {
        var text = html;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<strong>", "<b>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</strong>", "</b>");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<p>(.*?)</p>", "$1\n");
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", string.Empty);
        return text;
    }

    private static (string paramName, string endpoint) GetMediaTypeAndEndpoint(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
        {
            return ("photo", "sendPhoto");
        }
        if (new[] { ".mp4", ".mov", ".avi", ".mkv" }.Contains(extension))
        {
            return ("video", "sendVideo");
        }
        return ("document", "sendDocument");
    }

    private static string GetMediaType(string path)
    {
        var extension = Path.GetExtension(path).ToLowerInvariant();
        if (new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" }.Contains(extension))
        {
            return "photo";
        }
        if (new[] { ".mp4", ".mov", ".avi", ".mkv" }.Contains(extension))
        {
            return "video";
        }
        return "document";
    }

    private static List<List<T>> ChunkList<T>(List<T> list, int size)
    {
        var result = new List<List<T>>();
        for (var i = 0; i < list.Count; i += size)
        {
            result.Add(list.GetRange(i, Math.Min(size, list.Count - i)));
        }
        return result;
    }

    private string GetBotToken() => _configuration["TELEGRAM_TOKEN"] ?? throw new InvalidOperationException("TELEGRAM_TOKEN not configured");

    #region DTOs

    private class TelegramChatResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public TelegramChat? Result { get; set; }
    }

    private class TelegramChat
    {
        [JsonPropertyName("id")]
        public string Id { get; set; } = string.Empty;

        [JsonPropertyName("title")]
        public string? Title { get; set; }

        [JsonPropertyName("username")]
        public string? Username { get; set; }

        [JsonPropertyName("photo")]
        public TelegramChatPhoto? Photo { get; set; }
    }

    private class TelegramChatPhoto
    {
        [JsonPropertyName("big_file_id")]
        public string? BigFileId { get; set; }
    }

    private class TelegramFileResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public TelegramFile? Result { get; set; }
    }

    private class TelegramFile
    {
        [JsonPropertyName("file_path")]
        public string? FilePath { get; set; }
    }

    private class TelegramMessageResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public TelegramMessage? Result { get; set; }
    }

    private class TelegramMessagesResponse
    {
        [JsonPropertyName("ok")]
        public bool Ok { get; set; }

        [JsonPropertyName("result")]
        public List<TelegramMessage>? Result { get; set; }
    }

    private class TelegramMessage
    {
        [JsonPropertyName("message_id")]
        public int MessageId { get; set; }
    }

    #endregion
}
