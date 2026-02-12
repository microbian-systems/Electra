using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using Electra.Cms.Areas.Blog.Models;
using Electra.Core.Http;
using Microsoft.Extensions.Logging;

namespace Electra.Cms.Areas.Blog.Services;

public interface IForemApiClientBase
{
    Task<List<ArticleIndex>> GetArticlesAsync(
        int? page = null,
        int? perPage = null,
        string? tag = null,
        string? tags = null,
        string? tagsExclude = null,
        string? username = null,
        ArticleState? state = null,
        int? top = null,
        CancellationToken cancellationToken = default);

    Task<ArticleIndex?> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<List<ArticleIndex>> GetUserPublishedArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    Task<List<ArticleIndex>> GetUserUnpublishedArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    Task<List<ArticleIndex>> GetUserAllArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default);

    Task<ArticleIndex?> CreateArticleAsync(
        ArticleCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<ArticleIndex?> UpdateArticleAsync(
        int id,
        ArticleCreateRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> UnpublishArticleAsync(
        int id,
        string? note = null,
        CancellationToken cancellationToken = default);

    Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default);
    Task<Organization?> GetOrganizationByUsernameAsync(string username, CancellationToken cancellationToken = default);
}

public abstract class ForemApiClientBase(HttpClient httpClient, ILogger<ForemApiClientBase> log) : HttpClientBase(httpClient, log), IForemApiClientBase
{
    public async Task<List<ArticleIndex>> GetArticlesAsync(
        int? page = null,
        int? perPage = null,
        string? tag = null,
        string? tags = null,
        string? tagsExclude = null,
        string? username = null,
        ArticleState? state = null,
        int? top = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (perPage.HasValue) queryParams.Add($"per_page={perPage.Value}");
        if (!string.IsNullOrEmpty(tag)) queryParams.Add($"tag={Uri.EscapeDataString(tag)}");
        if (!string.IsNullOrEmpty(tags)) queryParams.Add($"tags={Uri.EscapeDataString(tags)}");
        if (!string.IsNullOrEmpty(tagsExclude)) queryParams.Add($"tags_exclude={Uri.EscapeDataString(tagsExclude)}");
        if (!string.IsNullOrEmpty(username)) queryParams.Add($"username={Uri.EscapeDataString(username)}");
        if (state.HasValue) queryParams.Add($"state={state.Value.ToString().ToLowerInvariant()}");
        if (top.HasValue) queryParams.Add($"top={top.Value}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"/articles{query}";

        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get articles. Status: {StatusCode}, Content: {Content}", 
                response.StatusCode, await response.Content.ReadAsStringAsync(cancellationToken));
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var articles = await DeserializeAsync<List<ArticleIndex>>(json);
        return articles ?? [];
    }

    public virtual async Task<ArticleIndex?> GetArticleByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"/articles/{id}";
        
        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get article {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<ArticleIndex>(json);
    }

    public virtual async Task<List<ArticleIndex>> GetUserPublishedArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (perPage.HasValue) queryParams.Add($"per_page={perPage.Value}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"/articles/me/published{query}";

        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get user published articles. Status: {StatusCode}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var articles = await DeserializeAsync<List<ArticleIndex>>(json);
        return articles ?? [];
    }

    public virtual async Task<List<ArticleIndex>> GetUserUnpublishedArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (perPage.HasValue) queryParams.Add($"per_page={perPage.Value}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"/articles/me/unpublished{query}";

        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get user unpublished articles. Status: {StatusCode}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var articles = await DeserializeAsync<List<ArticleIndex>>(json);
        return articles ?? [];
    }

    public virtual async Task<List<ArticleIndex>> GetUserAllArticlesAsync(
        int? page = null,
        int? perPage = null,
        CancellationToken cancellationToken = default)
    {
        var queryParams = new List<string>();
        
        if (page.HasValue) queryParams.Add($"page={page.Value}");
        if (perPage.HasValue) queryParams.Add($"per_page={perPage.Value}");

        var query = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : string.Empty;
        var url = $"/articles/me/all{query}";

        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get user all articles. Status: {StatusCode}", response.StatusCode);
            return [];
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var articles = await DeserializeAsync<List<ArticleIndex>>(json);
        return articles ?? [];
    }

    public virtual async Task<ArticleIndex?> CreateArticleAsync(
        ArticleCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await PostAsync("/articles", request);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to create article. Status: {StatusCode}", response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<ArticleIndex>(json);
    }

    public virtual async Task<ArticleIndex?> UpdateArticleAsync(
        int id,
        ArticleCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = await PutAsync($"/articles/{id}", request);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to update article {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<ArticleIndex>(json);
    }

    public virtual async Task<bool> UnpublishArticleAsync(
        int id,
        string? note = null,
        CancellationToken cancellationToken = default)
    {
        var query = !string.IsNullOrEmpty(note) ? $"?note={Uri.EscapeDataString(note)}" : string.Empty;
        var url = $"/articles/{id}/unpublish{query}";

        var response = await PutAsync<object>(url, new { });
        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        log.LogWarning("Failed to unpublish article {Id}. Status: {StatusCode}", id, response.StatusCode);
        return false;
    }

    public virtual async Task<User?> GetUserByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var url = $"/users/{id}";
        
        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get user {Id}. Status: {StatusCode}", id, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<User>(json);
    }

    public virtual async Task<User?> GetUserByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var url = $"/users/by_username";
        var queryParams = new List<string> { $"url={Uri.EscapeDataString(username)}" };
        var query = "?" + string.Join("&", queryParams);

        var response = await GetAsync($"{url}{query}");
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get user {Username}. Status: {StatusCode}", username, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<User>(json);
    }

    public virtual async Task<Organization?> GetOrganizationByUsernameAsync(string username, CancellationToken cancellationToken = default)
    {
        var url = $"/organizations/{Uri.EscapeDataString(username)}";
        
        var response = await GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            log.LogWarning("Failed to get organization {Username}. Status: {StatusCode}", username, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        return await DeserializeAsync<Organization>(json);
    }
}