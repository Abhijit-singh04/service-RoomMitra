using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Security;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Controller for user profile and identity management.
/// This controller works with Azure AD B2C authenticated users.
/// </summary>
[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UsersController : ControllerBase
{
    private readonly IUserContext _userContext;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserContext userContext, ILogger<UsersController> logger)
    {
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Get the current authenticated user's profile information.
    /// This data comes from the Azure AD B2C JWT token claims.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult GetCurrentUser()
    {
        if (!_userContext.IsAuthenticated)
        {
            return Unauthorized();
        }

        var profile = new UserProfileResponse(
            Id: _userContext.UserId?.ToString() ?? _userContext.Subject ?? "",
            ObjectId: _userContext.ObjectId,
            Email: _userContext.Email,
            Name: _userContext.Name,
            IdentityProvider: _userContext.IdentityProvider,
            Roles: _userContext.Roles.ToList()
        );

        _logger.LogDebug("User profile requested: {UserId}, IDP: {IdentityProvider}", 
            profile.Id, profile.IdentityProvider);

        return Ok(profile);
    }

    /// <summary>
    /// Verify that the current user is authenticated.
    /// Useful for frontend session validation.
    /// </summary>
    [HttpGet("verify")]
    [ProducesResponseType(typeof(AuthVerifyResponse), StatusCodes.Status200OK)]
    public IActionResult VerifyAuthentication()
    {
        return Ok(new AuthVerifyResponse(
            IsAuthenticated: _userContext.IsAuthenticated,
            UserId: _userContext.UserId?.ToString(),
            Email: _userContext.Email
        ));
    }

    /// <summary>
    /// Admin-only endpoint example.
    /// </summary>
    [HttpGet("admin-only")]
    [Authorize(Policy = "RequireAdmin")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult AdminOnly()
    {
        return Ok(new { message = "You have admin access!", userId = _userContext.UserId });
    }
}

/// <summary>
/// User profile response DTO.
/// </summary>
public sealed record UserProfileResponse(
    string Id,
    string? ObjectId,
    string? Email,
    string? Name,
    string? IdentityProvider,
    List<string> Roles
);

/// <summary>
/// Authentication verification response.
/// </summary>
public sealed record AuthVerifyResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email
);
