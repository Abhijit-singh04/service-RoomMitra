using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Controller for Azure AD B2C authentication using the BFF (Backend-for-Frontend) pattern.
/// All OAuth flows are handled server-side for enhanced security.
/// </summary>
[ApiController]
[Route("api/auth/azure")]
public sealed class AzureAuthController : ControllerBase
{
    private readonly IAzureAuthService _azureAuthService;
    private readonly ILogger<AzureAuthController> _logger;

    public AzureAuthController(
        IAzureAuthService azureAuthService,
        ILogger<AzureAuthController> logger)
    {
        _azureAuthService = azureAuthService;
        _logger = logger;
    }

    /// <summary>
    /// Initiates the Azure AD B2C login flow.
    /// Returns the authorization URL that the client should redirect to.
    /// </summary>
    /// <param name="redirectUri">The redirect URI for the OAuth callback.</param>
    /// <param name="codeChallenge">The PKCE code challenge (generated client-side).</param>
    /// <param name="returnUrl">The URL to return to after authentication.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("login")]
    [ProducesResponseType(typeof(AzureLoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Login(
        [FromQuery] string redirectUri,
        [FromQuery] string codeChallenge,
        [FromQuery] string returnUrl = "/",
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(redirectUri))
            {
                return BadRequest(new { error = "redirect_uri is required" });
            }

            if (string.IsNullOrEmpty(codeChallenge))
            {
                return BadRequest(new { error = "code_challenge is required" });
            }

            var response = await _azureAuthService.GetAuthorizationUrlAsync(
                redirectUri,
                codeChallenge,
                returnUrl,
                cancellationToken);

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate login");
            return Problem(
                title: "Login initiation failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Exchanges an authorization code for tokens.
    /// This endpoint receives the code from the OAuth callback and exchanges it for tokens.
    /// </summary>
    /// <param name="request">The callback request containing the authorization code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("callback")]
    [ProducesResponseType(typeof(AzureAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Callback(
        [FromBody] AzureCallbackRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.Code))
            {
                return BadRequest(new { error = "code is required" });
            }

            if (string.IsNullOrEmpty(request.CodeVerifier))
            {
                return BadRequest(new { error = "code_verifier is required" });
            }

            var response = await _azureAuthService.ExchangeCodeForTokensAsync(request, cancellationToken);
            
            _logger.LogInformation("User {UserId} authenticated successfully", response.User.Id);
            
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Token exchange failed");
            return Unauthorized(new { error = "token_exchange_failed", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback processing failed");
            return Problem(
                title: "Authentication failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Refreshes an access token using a refresh token.
    /// </summary>
    /// <param name="request">The refresh request containing the refresh token.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(AzureAuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] AzureRefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(request.RefreshToken))
            {
                return BadRequest(new { error = "refresh_token is required" });
            }

            var response = await _azureAuthService.RefreshTokenAsync(request.RefreshToken, cancellationToken);
            
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Token refresh failed");
            return Unauthorized(new { error = "token_refresh_failed", message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token refresh failed");
            return Problem(
                title: "Token refresh failed",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Gets the current user information from the access token.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(AzureUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken = default)
    {
        try
        {
            // Extract access token from Authorization header
            var authHeader = Request.Headers.Authorization.ToString();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            {
                return Unauthorized(new { error = "No access token provided" });
            }

            var accessToken = authHeader["Bearer ".Length..];
            var user = await _azureAuthService.ValidateTokenAsync(accessToken, cancellationToken);

            if (user is null)
            {
                return Unauthorized(new { error = "Invalid or expired token" });
            }

            return Ok(new { authenticated = true, user });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get current user");
            return Problem(
                title: "Failed to get user",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Initiates logout and returns the Azure AD B2C logout URL.
    /// </summary>
    /// <param name="postLogoutRedirectUri">The URL to redirect to after logout.</param>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout([FromQuery] string postLogoutRedirectUri = "/")
    {
        var logoutUrl = _azureAuthService.GetLogoutUrl(postLogoutRedirectUri);
        return Ok(new { logoutUrl });
    }
}
