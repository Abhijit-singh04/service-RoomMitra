namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Request to initiate Azure AD B2C login flow.
/// </summary>
public sealed record AzureLoginRequest(
    /// <summary>
    /// The URL to redirect back to after successful authentication.
    /// </summary>
    string ReturnUrl = "/"
);

/// <summary>
/// Response containing the Azure AD B2C authorization URL.
/// </summary>
public sealed record AzureLoginResponse(
    /// <summary>
    /// The Azure AD B2C authorization URL to redirect the user to.
    /// </summary>
    string AuthorizationUrl,
    
    /// <summary>
    /// The state parameter for CSRF protection (to be stored by client).
    /// </summary>
    string State
);

/// <summary>
/// Request to exchange authorization code for tokens.
/// </summary>
public sealed record AzureCallbackRequest(
    /// <summary>
    /// The authorization code received from Azure AD B2C.
    /// </summary>
    string Code,
    
    /// <summary>
    /// The state parameter for CSRF validation.
    /// </summary>
    string State,
    
    /// <summary>
    /// The PKCE code verifier.
    /// </summary>
    string CodeVerifier,
    
    /// <summary>
    /// The redirect URI used in the authorization request.
    /// </summary>
    string RedirectUri
);

/// <summary>
/// Response after successful token exchange.
/// </summary>
public sealed record AzureAuthResponse(
    /// <summary>
    /// The access token for API calls.
    /// </summary>
    string AccessToken,
    
    /// <summary>
    /// The ID token containing user claims.
    /// </summary>
    string? IdToken,
    
    /// <summary>
    /// The refresh token for obtaining new access tokens.
    /// </summary>
    string? RefreshToken,
    
    /// <summary>
    /// Token expiration time in seconds.
    /// </summary>
    int ExpiresIn,
    
    /// <summary>
    /// The authenticated user information.
    /// </summary>
    AzureUserDto User
);

/// <summary>
/// User information extracted from Azure AD B2C tokens.
/// </summary>
public sealed record AzureUserDto(
    /// <summary>
    /// The user's unique identifier (subject claim).
    /// </summary>
    string Id,
    
    /// <summary>
    /// The Azure AD object ID.
    /// </summary>
    string? ObjectId,
    
    /// <summary>
    /// The user's display name.
    /// </summary>
    string Name,
    
    /// <summary>
    /// The user's email address.
    /// </summary>
    string Email,
    
    /// <summary>
    /// The identity provider (e.g., "google.com", "local").
    /// </summary>
    string? IdentityProvider
);

/// <summary>
/// Request to refresh tokens.
/// </summary>
public sealed record AzureRefreshRequest(
    /// <summary>
    /// The refresh token.
    /// </summary>
    string RefreshToken
);

/// <summary>
/// Token response from Azure AD B2C token endpoint.
/// </summary>
public sealed record AzureTokenResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string? IdToken { get; init; }
    public string? RefreshToken { get; init; }
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; }
    public string? Scope { get; init; }
}
