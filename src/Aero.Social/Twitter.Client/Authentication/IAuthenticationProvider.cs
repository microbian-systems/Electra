namespace Aero.Social.Twitter.Client.Authentication;

/// <summary>
/// Interface for authentication providers that can authenticate HTTP requests.
/// </summary>
public interface IAuthenticationProvider
{
    /// <summary>
    /// Authenticates an HTTP request by adding the necessary authentication headers.
    /// </summary>
    /// <param name="request">The HTTP request to authenticate.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task AuthenticateRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken);
}