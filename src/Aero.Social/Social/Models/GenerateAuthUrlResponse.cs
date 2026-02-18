namespace Aero.Social.Models;

/// <summary>
/// Contains the response from generating an OAuth authorization URL.
/// </summary>
public class GenerateAuthUrlResponse
{
    /// <summary>
    /// Gets or sets the OAuth authorization URL for the user to visit.
    /// </summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the PKCE code verifier (for providers using PKCE).
    /// </summary>
    public string CodeVerifier { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the state parameter for CSRF protection.
    /// </summary>
    public string State { get; set; } = string.Empty;
}
