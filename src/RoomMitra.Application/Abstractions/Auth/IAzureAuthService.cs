using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Application.Abstractions.Auth;

/// <summary>
/// Service for Azure AD B2C authentication operations.
/// Implements the Backend-for-Frontend (BFF) pattern for OAuth 2.0 flows.
/// </summary>
public interface IAzureAuthService
{
    /// <summary>
    /// Generates the Azure AD B2C authorization URL with PKCE parameters.
    /// </summary>
    /// <param name="redirectUri">The redirect URI for the callback.</param>
    /// <param name="codeChallenge">The PKCE code challenge.</param>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Login response containing the authorization URL and state.</returns>
    Task<AzureLoginResponse> GetAuthorizationUrlAsync(
        string redirectUri,
        string codeChallenge,
        string returnUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Exchanges an authorization code for tokens using the token endpoint.
    /// This is the server-side token exchange that can use a client secret.
    /// </summary>
    /// <param name="request">The callback request containing the authorization code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response containing tokens and user information.</returns>
    Task<AzureAuthResponse> ExchangeCodeForTokensAsync(
        AzureCallbackRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="refreshToken">The refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>New auth response with refreshed tokens.</returns>
    Task<AzureAuthResponse> RefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates an access token and returns the user information.
    /// </summary>
    /// <param name="accessToken">The access token to validate.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>User information if valid, null if invalid.</returns>
    Task<AzureUserDto?> ValidateTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the Azure AD B2C logout URL.
    /// </summary>
    /// <param name="postLogoutRedirectUri">The URL to redirect to after logout.</param>
    /// <returns>The logout URL.</returns>
    string GetLogoutUrl(string postLogoutRedirectUri);
}
