using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Auth;
using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IConfiguration _configuration;
    
    // Cookie settings
    private const string AccessTokenCookie = "roommitra_access_token";
    private const int CookieExpirationDays = 7;

    public AuthController(IAuthService authService, IConfiguration configuration)
    {
        _authService = authService;
        _configuration = configuration;
    }
    
    /// <summary>
    /// Check current session. Returns user info if authenticated via cookie.
    /// This is called on app startup to restore session without re-login.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser(CancellationToken cancellationToken)
    {
        // Try to get token from HttpOnly cookie
        var token = Request.Cookies[AccessTokenCookie];
        
        if (string.IsNullOrEmpty(token))
        {
            return Unauthorized(new { message = "No session found" });
        }
        
        // Validate the token and get user info
        var userInfo = await _authService.ValidateTokenAndGetUserAsync(token, cancellationToken);
        
        if (userInfo == null)
        {
            // Cookie exists but token is invalid/expired - clear it
            ClearAuthCookie();
            return Unauthorized(new { message = "Session expired" });
        }
        
        return Ok(userInfo);
    }
    
    /// <summary>
    /// Logout - clears the HttpOnly auth cookie.
    /// </summary>
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Logout()
    {
        ClearAuthCookie();
        return Ok(new { message = "Logged out successfully" });
    }
    
    private void SetAuthCookie(string token)
    {
        var allowInsecureCookies = _configuration.GetValue<bool>("AllowInsecureCookies");
        var allowCrossSiteCookies = _configuration.GetValue<bool>("AllowCrossSiteCookies");

        // If we're behind a proxy and forwarding headers is enabled, Request.IsHttps should be accurate.
        // As a safety net: in production, prefer Secure cookies.
        var secureCookie = Request.IsHttps || !allowInsecureCookies;

        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,                    // JavaScript cannot read this
            Secure = secureCookie,
            // If UI and API are on different origins, SameSite must be None (and Secure=true) for fetch/XHR.
            SameSite = allowCrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax,
            Expires = DateTimeOffset.UtcNow.AddDays(CookieExpirationDays),
            Path = "/",                         // Available for all paths
        };
        
        Response.Cookies.Append(AccessTokenCookie, token, cookieOptions);
    }
    
    private void ClearAuthCookie()
    {
        var allowInsecureCookies = _configuration.GetValue<bool>("AllowInsecureCookies");
        var allowCrossSiteCookies = _configuration.GetValue<bool>("AllowCrossSiteCookies");
        var secureCookie = Request.IsHttps || !allowInsecureCookies;

        Response.Cookies.Delete(AccessTokenCookie, new CookieOptions
        {
            HttpOnly = true,
            Secure = secureCookie,
            SameSite = allowCrossSiteCookies ? SameSiteMode.None : SameSiteMode.Lax,
            Path = "/",
        });
    }

    [HttpPost("register")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RegisterAsync(request, cancellationToken);
            
            // Set token in HttpOnly cookie (not in response body)
            SetAuthCookie(response.AccessToken);
            
            // Return user info without token
            return Ok(ToUserInfoResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Registration failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.LoginAsync(request, cancellationToken);
            
            // Set token in HttpOnly cookie (not in response body)
            SetAuthCookie(response.AccessToken);
            
            // Return user info without token
            return Ok(ToUserInfoResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Login failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("request-otp")]
    [ProducesResponseType(typeof(RequestOtpResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.RequestOtpAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "OTP request failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    [HttpPost("verify-otp")]
    [ProducesResponseType(typeof(UserInfoResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _authService.VerifyOtpAsync(request, cancellationToken);
            
            // Set token in HttpOnly cookie (not in response body)
            SetAuthCookie(response.AccessToken);
            
            // Return user info without token
            return Ok(ToUserInfoResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "OTP verification failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Complete profile for users who signed up via Phone OTP.
    /// Requires authentication.
    /// </summary>
    [Authorize]
    [HttpPost("complete-profile")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CompleteProfile([FromBody] CompleteProfileRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized();
            }

            var response = await _authService.CompleteProfileAsync(userId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "Profile completion failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }

    /// <summary>
    /// Sync user from external identity provider (Azure AD B2C / Google).
    /// Called after OAuth callback to create or update local user.
    /// </summary>
    /// <remarks>
    /// This endpoint must only trust identity data from a validated Azure AD B2C access token.
    /// </remarks>
    [Authorize(AuthenticationSchemes = "AzureB2C")]
    [HttpPost("external/sync")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> SyncExternalUser([FromBody] ExternalSyncRequest? request, CancellationToken cancellationToken)
    {
        try
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Unauthorized();
            }

            // Debug: Log all claims
            Console.WriteLine("[ExternalSync] Claims in token:");
            foreach (var claim in User.Claims)
            {
                Console.WriteLine($"  {claim.Type}: {claim.Value}");
            }

            static string? FirstClaimValue(ClaimsPrincipal user, string type)
            {
                return user.Claims.FirstOrDefault(c => string.Equals(c.Type, type, StringComparison.OrdinalIgnoreCase))?.Value;
            }

            // Azure CIAM uses full URI claim types, not short names
            // oid -> http://schemas.microsoft.com/identity/claims/objectidentifier
            // sub -> http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier
            var objectId = FirstClaimValue(User, "oid")
                ?? FirstClaimValue(User, "http://schemas.microsoft.com/identity/claims/objectidentifier")
                ?? FirstClaimValue(User, "sub")
                ?? FirstClaimValue(User, ClaimTypes.NameIdentifier);

            Console.WriteLine($"[ExternalSync] objectId: {objectId}");

            if (string.IsNullOrWhiteSpace(objectId))
            {
                return Problem(title: "External user sync failed", detail: "Missing required claim 'oid' (or 'sub') in token.", statusCode: StatusCodes.Status400BadRequest);
            }

            // Email can come from 'emails' (B2C), 'email', 'preferred_username' (CIAM), or standard claim.
            var email = FirstClaimValue(User, "emails")
                ?? FirstClaimValue(User, "email")
                ?? FirstClaimValue(User, "preferred_username")
                ?? User.FindFirstValue(ClaimTypes.Email);

            Console.WriteLine($"[ExternalSync] email: {email}");

            // Name can come from 'name' or given/family.
            var name = FirstClaimValue(User, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                var given = FirstClaimValue(User, "given_name");
                var family = FirstClaimValue(User, "family_name");
                var combined = $"{given} {family}".Trim();
                name = string.IsNullOrWhiteSpace(combined) ? null : combined;
            }

            var idpRaw = FirstClaimValue(User, "idp");
            var identityProvider = idpRaw?.ToLowerInvariant().Contains("google") == true
                ? "google"
                : (idpRaw ?? "azure");

            var profileImageUrl = request?.ProfileImageUrl;
            if (string.IsNullOrWhiteSpace(profileImageUrl))
            {
                profileImageUrl = FirstClaimValue(User, "picture");
            }

            var external = new ExternalUserInfo(
                ObjectId: objectId,
                Email: email,
                Name: name,
                ProfileImageUrl: profileImageUrl,
                IdentityProvider: identityProvider
            );

            var response = await _authService.SyncExternalUserAsync(external, cancellationToken);
            
            // Set token in HttpOnly cookie (not in response body)
            SetAuthCookie(response.AccessToken);
            
            // Return user info without token
            return Ok(ToUserInfoResponse(response));
        }
        catch (InvalidOperationException ex)
        {
            return Problem(title: "External user sync failed", detail: ex.Message, statusCode: StatusCodes.Status400BadRequest);
        }
    }
    
    /// <summary>
    /// Convert AuthResponse to UserInfoResponse (strips token for secure response)
    /// </summary>
    private static UserInfoResponse ToUserInfoResponse(AuthResponse auth) => new(
        UserId: auth.User.Id,
        Name: auth.User.Name,
        Email: auth.User.Email,
        ProfileImageUrl: auth.User.ProfileImageUrl,
        PhoneNumber: auth.User.PhoneNumber,
        PhoneVerified: auth.User.PhoneVerified,
        IsVerified: auth.User.IsVerified,
        IsProfileComplete: auth.User.IsProfileComplete,
        AuthProvider: auth.User.AuthProvider,
        IsNewUser: auth.IsNewUser,
        RequiresProfileCompletion: auth.RequiresProfileCompletion
    );

    public sealed record ExternalSyncRequest(string? ProfileImageUrl);
}
