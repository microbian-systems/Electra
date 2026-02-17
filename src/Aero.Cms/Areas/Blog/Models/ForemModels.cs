using System.Text.Json.Serialization;

namespace Aero.Cms.Areas.Blog.Models;

public record ArticleFlareTag
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("bg_color_hex")]
    public string? BackgroundColorHex { get; init; }

    [JsonPropertyName("text_color_hex")]
    public string? TextColorHex { get; init; }
}

public record SharedUser
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("twitter_username")]
    public string? TwitterUsername { get; init; }

    [JsonPropertyName("github_username")]
    public string? GithubUsername { get; init; }

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    [JsonPropertyName("profile_image")]
    public string ProfileImage { get; init; } = string.Empty;

    [JsonPropertyName("profile_image_90")]
    public string ProfileImage90 { get; init; } = string.Empty;
}

public record SharedOrganization
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("profile_image")]
    public string ProfileImage { get; init; } = string.Empty;

    [JsonPropertyName("profile_image_90")]
    public string ProfileImage90 { get; init; } = string.Empty;
}

public record ArticleIndex
{
    [JsonPropertyName("type_of")]
    public string TypeOf { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public int Id { get; init; }

    [JsonPropertyName("title")]
    public string Title { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("cover_image")]
    public string? CoverImage { get; init; }

    [JsonPropertyName("readable_publish_date")]
    public string ReadablePublishDate { get; init; } = string.Empty;

    [JsonPropertyName("social_image")]
    public string SocialImage { get; init; } = string.Empty;

    [JsonPropertyName("tag_list")]
    public List<string> TagList { get; init; } = new();

    [JsonPropertyName("tags")]
    public string Tags { get; init; } = string.Empty;

    [JsonPropertyName("slug")]
    public string Slug { get; init; } = string.Empty;

    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("canonical_url")]
    public string CanonicalUrl { get; init; } = string.Empty;

    [JsonPropertyName("positive_reactions_count")]
    public int PositiveReactionsCount { get; init; }

    [JsonPropertyName("public_reactions_count")]
    public int PublicReactionsCount { get; init; }

    [JsonPropertyName("comments_count")]
    public int CommentsCount { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("edited_at")]
    public DateTime? EditedAt { get; init; }

    [JsonPropertyName("crossposted_at")]
    public DateTime? CrosspostedAt { get; init; }

    [JsonPropertyName("published_at")]
    public DateTime PublishedAt { get; init; }

    [JsonPropertyName("last_comment_at")]
    public DateTime LastCommentAt { get; init; }

    [JsonPropertyName("published_timestamp")]
    public DateTime PublishedTimestamp { get; init; }

    [JsonPropertyName("reading_time_minutes")]
    public int ReadingTimeMinutes { get; init; }

    [JsonPropertyName("user")]
    public SharedUser User { get; init; } = new();

    [JsonPropertyName("flare_tag")]
    public ArticleFlareTag? FlareTag { get; init; }

    [JsonPropertyName("organization")]
    public SharedOrganization? Organization { get; init; }
}

public record ArticleCreate
{
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    [JsonPropertyName("body_markdown")]
    public string? BodyMarkdown { get; init; }

    [JsonPropertyName("published")]
    public bool Published { get; init; } = false;

    [JsonPropertyName("series")]
    public string? Series { get; init; }

    [JsonPropertyName("main_image")]
    public string? MainImage { get; init; }

    [JsonPropertyName("canonical_url")]
    public string? CanonicalUrl { get; init; }

    [JsonPropertyName("description")]
    public string? Description { get; init; }

    [JsonPropertyName("tags")]
    public string? Tags { get; init; }

    [JsonPropertyName("organization_id")]
    public int? OrganizationId { get; init; }
}

public record ArticleCreateRequest
{
    [JsonPropertyName("article")]
    public ArticleCreate Article { get; init; } = new();
}

public record User
{
    [JsonPropertyName("type_of")]
    public string TypeOf { get; init; } = string.Empty;

    [JsonPropertyName("id")]
    public long Id { get; init; }

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string? Summary { get; init; }

    [JsonPropertyName("twitter_username")]
    public string TwitterUsername { get; init; } = string.Empty;

    [JsonPropertyName("github_username")]
    public string GithubUsername { get; init; } = string.Empty;

    [JsonPropertyName("website_url")]
    public string? WebsiteUrl { get; init; }

    [JsonPropertyName("location")]
    public string? Location { get; init; }

    [JsonPropertyName("joined_at")]
    public string JoinedAt { get; init; } = string.Empty;

    [JsonPropertyName("profile_image")]
    public string ProfileImage { get; init; } = string.Empty;
}

public record Organization
{
    [JsonPropertyName("type_of")]
    public string TypeOf { get; init; } = string.Empty;

    [JsonPropertyName("username")]
    public string Username { get; init; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; init; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; init; } = string.Empty;

    [JsonPropertyName("twitter_username")]
    public string TwitterUsername { get; init; } = string.Empty;

    [JsonPropertyName("github_username")]
    public string GithubUsername { get; init; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; init; } = string.Empty;

    [JsonPropertyName("location")]
    public string Location { get; init; } = string.Empty;
}

public enum ArticleState
{
    Fresh,
    Rising,
    All
}