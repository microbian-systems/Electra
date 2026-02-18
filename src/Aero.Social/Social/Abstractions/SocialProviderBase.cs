using System.Reflection;
using System.Text.Json;
using Aero.Core.Http;
using Aero.Social.Models;
using Aero.Social.Plugs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Aero.Social.Abstractions;

/// <summary>
/// Base class for all social media provider implementations.
/// Provides common functionality for HTTP requests, error handling, retry logic, and plug support.
/// </summary>
public abstract class SocialProviderBase : HttpClientBase, ISocialProvider
{
    /// <inheritdoc/>
    public abstract string Identifier { get; }

    /// <inheritdoc/>
    public abstract string Name { get; }

    /// <inheritdoc/>
    public abstract string[] Scopes { get; }

    /// <inheritdoc/>
    public virtual EditorType Editor => EditorType.Normal;

    /// <inheritdoc/>
    public virtual bool IsBetweenSteps => false;

    /// <inheritdoc/>
    public virtual bool IsWeb3 => false;

    /// <inheritdoc/>
    public virtual int MaxConcurrentJobs => 1;

    /// <inheritdoc/>
    public virtual string? Tooltip => null;

    /// <inheritdoc/>
    public virtual bool OneTimeToken => false;

    /// <inheritdoc/>
    public virtual bool RefreshWait => false;

    /// <inheritdoc/>
    public virtual bool ConvertToJpeg => false;

    /// <summary>
    /// Initializes a new instance of the <see cref="SocialProviderBase"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client for making API requests.</param>
    /// <param name="logger">The logger for this provider.</param>
    /// <param name="resiliencePipeline">Optional resilience pipeline for retry logic.</param>
    protected SocialProviderBase(
        HttpClient httpClient,
        ILogger logger,
        ResiliencePipeline<HttpResponseMessage>? resiliencePipeline = null)
        : base(httpClient, logger, resiliencePipeline)
    {
    }

    /// <inheritdoc/>
    public abstract int MaxLength(object? additionalSettings = null);

    /// <inheritdoc/>
    public abstract Task<PostResponse[]> PostAsync(
        string id,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual Task<PostResponse[]?> CommentAsync(
        string id,
        string postId,
        string? lastCommentId,
        string accessToken,
        List<PostDetails> posts,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<PostResponse[]?>(null);
    }

    /// <inheritdoc/>
    public abstract Task<GenerateAuthUrlResponse> GenerateAuthUrlAsync(
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<AuthTokenDetails> AuthenticateAsync(
        AuthenticateParams parameters,
        ClientInformation? clientInformation = null,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public abstract Task<AuthTokenDetails> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <inheritdoc/>
    public virtual Task<AuthTokenDetails?> ReConnectAsync(
        string id,
        string requiredId,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AuthTokenDetails?>(null);
    }

    /// <inheritdoc/>
    public virtual Task<AnalyticsData[]?> AnalyticsAsync(
        string id,
        string accessToken,
        int days,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AnalyticsData[]?>(null);
    }

    /// <inheritdoc/>
    public virtual Task<AnalyticsData[]?> PostAnalyticsAsync(
        string integrationId,
        string accessToken,
        string postId,
        int days,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<AnalyticsData[]?>(null);
    }

    /// <inheritdoc/>
    public virtual Task<object?> MentionAsync(
        string token,
        MentionQuery query,
        string id,
        Integration integration,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<object?>(new NoMentionResult());
    }

    /// <inheritdoc/>
    public virtual string? MentionFormat(string idOrHandle, string name) => null;

    /// <inheritdoc/>
    public virtual Task<FetchPageInformationResult?> FetchPageInformationAsync(
        string accessToken,
        object data,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<FetchPageInformationResult?>(null);
    }

    /// <summary>
    /// Handles provider-specific errors from API responses.
    /// Override this method to provide custom error handling logic.
    /// </summary>
    /// <param name="responseBody">The response body to analyze.</param>
    /// <returns>An error handling result, or null if no specific handling is needed.</returns>
    protected virtual ErrorHandlingResult? HandleErrors(string responseBody)
    {
        return null;
    }

    /// <summary>
    /// Represents the result of error handling analysis.
    /// </summary>
    /// <param name="Type">The type of error handling required.</param>
    /// <param name="Value">Additional context about the error.</param>
    public record ErrorHandlingResult(ErrorHandlingType Type, string Value);

    /// <summary>
    /// Defines the types of error handling actions.
    /// </summary>
    public enum ErrorHandlingType
    {
        /// <summary>
        /// The access token needs to be refreshed.
        /// </summary>
        RefreshToken,
        
        /// <summary>
        /// The request body was invalid or malformed.
        /// </summary>
        BadBody,
        
        /// <summary>
        /// The request should be retried after a delay.
        /// </summary>
        Retry
    }

    /// <summary>
    /// Checks if all required OAuth scopes have been granted.
    /// </summary>
    /// <param name="required">The required scopes.</param>
    /// <param name="granted">The granted scopes.</param>
    /// <exception cref="NotEnoughScopesException">Thrown when not all required scopes are granted.</exception>
    protected void CheckScopes(string[] required, string[] granted)
    {
        if (!required.All(scope => granted.Contains(scope, StringComparer.OrdinalIgnoreCase)))
        {
            throw new NotEnoughScopesException();
        }
    }

    /// <summary>
    /// Checks if all required OAuth scopes have been granted from a delimited string.
    /// </summary>
    /// <param name="required">The required scopes.</param>
    /// <param name="grantedScopes">The granted scopes as a comma or space-delimited string.</param>
    /// <exception cref="NotEnoughScopesException">Thrown when not all required scopes are granted.</exception>
    protected void CheckScopes(string[] required, string grantedScopes)
    {
        var delimiter = grantedScopes.Contains(',') ? ',' : ' ';
        var scopes = grantedScopes.Split(delimiter, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .ToArray();
        CheckScopes(required, scopes);
    }

    /// <summary>
    /// Fetches a URL with automatic retry logic for rate limiting and transient errors.
    /// </summary>
    /// <param name="url">The URL to fetch.</param>
    /// <param name="request">The HTTP request message.</param>
    /// <param name="identifier">The provider identifier for error messages.</param>
    /// <param name="maxRetries">Maximum number of retry attempts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP response message.</returns>
    /// <exception cref="BadBodyException">Thrown when the response indicates an unrecoverable error.</exception>
    /// <exception cref="RefreshTokenException">Thrown when the access token needs to be refreshed.</exception>
    protected async Task<HttpResponseMessage> FetchWithRetryAsync(
        string url,
        HttpRequestMessage request,
        string identifier = "",
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        var response = await HttpClient.SendAsync(request, cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return response;
        }

        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (maxRetries <= 0)
        {
            throw new BadBodyException(identifier, responseBody);
        }

        var handleError = HandleErrors(responseBody);

        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests ||
            response.StatusCode == System.Net.HttpStatusCode.InternalServerError ||
            responseBody.Contains("rate_limit_exceeded", StringComparison.OrdinalIgnoreCase) ||
            responseBody.Contains("Rate limit", StringComparison.OrdinalIgnoreCase))
        {
            await Task.Delay(5000, cancellationToken);
            var newRequest = await CloneRequestAsync(request);
            return await FetchWithRetryAsync(url, newRequest, identifier, maxRetries - 1, cancellationToken);
        }

        if (handleError?.Type == ErrorHandlingType.Retry)
        {
            await Task.Delay(5000, cancellationToken);
            var newRequest = await CloneRequestAsync(request);
            return await FetchWithRetryAsync(url, newRequest, identifier, maxRetries - 1, cancellationToken);
        }

        if ((response.StatusCode == System.Net.HttpStatusCode.Unauthorized &&
             (handleError?.Type == ErrorHandlingType.RefreshToken || handleError == null)) ||
            handleError?.Type == ErrorHandlingType.RefreshToken)
        {
            throw new RefreshTokenException(identifier, responseBody, request.Content, handleError?.Value);
        }

        throw new BadBodyException(identifier, responseBody, request.Content, handleError?.Value);
    }

    private static async Task<HttpRequestMessage> CloneRequestAsync(HttpRequestMessage request)
    {
        var cloned = new HttpRequestMessage(request.Method, request.RequestUri);

        foreach (var header in request.Headers)
        {
            cloned.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var content = await request.Content.ReadAsByteArrayAsync();
            cloned.Content = new ByteArrayContent(content);

            foreach (var header in request.Content.Headers)
            {
                cloned.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return cloned;
    }

    /// <summary>
    /// Reads a file from disk or downloads it from a URL.
    /// </summary>
    /// <param name="path">The local file path or URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The file contents as a byte array.</returns>
    protected async Task<byte[]> ReadOrFetchAsync(string path, CancellationToken cancellationToken = default)
    {
        if (path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            return await DownloadBytesAsync(path) ?? throw new InvalidOperationException($"Failed to download media from {path}");
        }

        return await File.ReadAllBytesAsync(path, cancellationToken);
    }

    /// <summary>
    /// Generates a random alphanumeric string of the specified length.
    /// </summary>
    /// <param name="length">The length of the string to generate.</param>
    /// <returns>A random alphanumeric string.</returns>
    protected static string MakeId(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        var random = Random.Shared;
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }

    #region Plug Support

    /// <summary>
    /// Discovers all plug methods defined in this provider using reflection.
    /// </summary>
    /// <returns>An enumerable of plug information for each discovered plug.</returns>
    public virtual IEnumerable<PlugInfo> DiscoverPlugs()
    {
        var methods = GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);

        foreach (var method in methods)
        {
            var plugAttr = method.GetCustomAttribute<PlugAttribute>();
            if (plugAttr != null)
            {
                yield return new PlugInfo
                {
                    Method = method,
                    Attribute = plugAttr,
                    IsPostPlug = false
                };
            }

            var postPlugAttr = method.GetCustomAttribute<PostPlugAttribute>();
            if (postPlugAttr != null)
            {
                yield return new PlugInfo
                {
                    Method = method,
                    PostPlugAttribute = postPlugAttr,
                    IsPostPlug = true
                };
            }
        }
    }

    /// <summary>
    /// Executes a plug method with the given context and executor.
    /// </summary>
    /// <param name="plug">The plug to execute.</param>
    /// <param name="executor">The plug executor.</param>
    /// <param name="context">The execution context.</param>
    /// <param name="fieldValues">Optional field values for the plug.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the plug execution.</returns>
    public virtual async Task<PlugExecutionResult> ExecutePlugAsync(
        PlugInfo plug,
        IPlugExecutor executor,
        PlugExecutionContext context,
        Dictionary<string, object>? fieldValues = null,
        CancellationToken cancellationToken = default)
    {
        if (plug.IsPostPlug && plug.PostPlugAttribute != null)
        {
            var validationResult = executor.ValidateFields(plug.PostPlugAttribute, fieldValues);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.SelectMany(e => e.Value));
                return PlugExecutionResult.FailedResult($"Validation failed: {errorMessage}");
            }
        }
        else if (!plug.IsPostPlug && plug.Attribute != null)
        {
            var validationResult = executor.ValidateFields(plug.Attribute, fieldValues);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join("; ", validationResult.Errors.SelectMany(e => e.Value));
                return PlugExecutionResult.FailedResult($"Validation failed: {errorMessage}");
            }
        }

        try
        {
            return await executor.ExecuteAsync(plug.Method, this, context, fieldValues, cancellationToken);
        }
        catch (Exception ex)
        {
            return PlugExecutionResult.FailedResult($"Plug execution failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets a plug by its identifier.
    /// </summary>
    /// <param name="identifier">The plug identifier to find.</param>
    /// <returns>The plug information, or null if not found.</returns>
    public virtual PlugInfo? GetPlug(string identifier)
    {
        return DiscoverPlugs().FirstOrDefault(p =>
            (p.IsPostPlug && p.PostPlugAttribute?.Identifier == identifier) ||
            (!p.IsPostPlug && p.Attribute?.Identifier == identifier));
    }

    /// <summary>
    /// Information about a discovered plug.
    /// </summary>
    public class PlugInfo
    {
        /// <summary>
        /// Gets or sets the method info for the plug.
        /// </summary>
        public MethodInfo Method { get; set; } = null!;
        
        /// <summary>
        /// Gets or sets the plug attribute (for regular plugs).
        /// </summary>
        public PlugAttribute? Attribute { get; set; }
        
        /// <summary>
        /// Gets or sets the post plug attribute (for post-processing plugs).
        /// </summary>
        public PostPlugAttribute? PostPlugAttribute { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether this is a post-processing plug.
        /// </summary>
        public bool IsPostPlug { get; set; }

        /// <summary>
        /// Gets the identifier for this plug.
        /// </summary>
        public string Identifier => IsPostPlug
            ? PostPlugAttribute?.Identifier ?? string.Empty
            : Attribute?.Identifier ?? string.Empty;

        /// <summary>
        /// Gets the title for this plug.
        /// </summary>
        public string Title => IsPostPlug
            ? PostPlugAttribute?.Title ?? string.Empty
            : Attribute?.Title ?? string.Empty;

        /// <summary>
        /// Gets the description for this plug.
        /// </summary>
        public string Description => IsPostPlug
            ? PostPlugAttribute?.Description ?? string.Empty
            : Attribute?.Description ?? string.Empty;
    }

    #endregion
}
