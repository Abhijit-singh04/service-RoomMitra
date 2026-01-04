using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Security;
using RoomMitra.Infrastructure.Identity;

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
    private readonly UserManager<AppUser> _userManager;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IUserContext userContext, 
        UserManager<AppUser> userManager,
        ILogger<UsersController> logger)
    {
        _userContext = userContext;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get the current authenticated user's profile information.
    /// This data comes from the Azure AD B2C JWT token claims.
    /// </summary>
    [HttpGet("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetCurrentUser()
    {
        if (!_userContext.IsAuthenticated)
        {
            return Unauthorized();
        }

        // Try to get extended profile from database
        var userId = _userContext.UserId;
        AppUser? dbUser = null;
        if (userId.HasValue)
        {
            dbUser = await _userManager.FindByIdAsync(userId.Value.ToString());
        }

        var profile = new UserProfileResponse(
            Id: _userContext.UserId?.ToString() ?? _userContext.Subject ?? "",
            ObjectId: _userContext.ObjectId,
            Email: _userContext.Email,
            Name: dbUser?.Name ?? _userContext.Name,
            Phone: dbUser?.PhoneNumber ?? _userContext.PhoneNumber,
            IdentityProvider: _userContext.IdentityProvider,
            Roles: _userContext.Roles.ToList(),
            ProfileImage: dbUser?.ProfileImageUrl,
            Bio: dbUser?.Bio,
            Occupation: dbUser?.Occupation,
            CreatedAt: dbUser?.CreatedAt ?? DateTimeOffset.UtcNow
        );

        _logger.LogDebug("User profile requested: {UserId}, IDP: {IdentityProvider}", 
            profile.Id, profile.IdentityProvider);

        return Ok(profile);
    }

    /// <summary>
    /// Update the current authenticated user's profile information.
    /// </summary>
    [HttpPatch("me")]
    [ProducesResponseType(typeof(UserProfileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateProfileRequest request)
    {
        if (!_userContext.IsAuthenticated || !_userContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var userId = _userContext.UserId.Value;
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return NotFound(new { message = "User not found" });
        }

        // Update fields if provided
        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            user.Name = request.Name;
        }
        if (request.Bio != null) // Allow setting to empty string
        {
            user.Bio = request.Bio;
        }
        if (request.Occupation != null)
        {
            user.Occupation = request.Occupation;
        }
        if (!string.IsNullOrWhiteSpace(request.ProfileImage))
        {
            user.ProfileImageUrl = request.ProfileImage;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User profile update failed: {Errors}", errors);
            return Problem(
                title: "Profile update failed",
                detail: errors,
                statusCode: StatusCodes.Status400BadRequest);
        }

        _logger.LogInformation("User profile updated. UserId: {UserId}", userId);

        var profile = new UserProfileResponse(
            Id: userId.ToString(),
            ObjectId: _userContext.ObjectId,
            Email: _userContext.Email,
            Name: user.Name,
            Phone: user.PhoneNumber ?? _userContext.PhoneNumber,
            IdentityProvider: _userContext.IdentityProvider,
            Roles: _userContext.Roles.ToList(),
            ProfileImage: user.ProfileImageUrl,
            Bio: user.Bio,
            Occupation: user.Occupation,
            CreatedAt: user.CreatedAt
        );

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

    /// <summary>
    /// Delete the current user's account.
    /// This permanently removes the user and all their data.
    /// </summary>
    [HttpDelete("me")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteAccount()
    {
        if (!_userContext.IsAuthenticated || !_userContext.UserId.HasValue)
        {
            return Unauthorized();
        }

        var userId = _userContext.UserId.Value;
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user is null)
        {
            return NotFound(new { message = "User not found" });
        }

        _logger.LogInformation("User deletion requested. UserId: {UserId}", userId);

        var result = await _userManager.DeleteAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(e => e.Description));
            _logger.LogWarning("User deletion failed: {Errors}", errors);
            return Problem(
                title: "Account deletion failed",
                detail: errors,
                statusCode: StatusCodes.Status400BadRequest);
        }

        _logger.LogInformation("User deleted successfully. UserId: {UserId}", userId);

        return Ok(new { message = "Account deleted successfully" });
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
    string? Phone,
    string? IdentityProvider,
    List<string> Roles,
    string? ProfileImage,
    string? Bio,
    string? Occupation,
    DateTimeOffset CreatedAt
);

/// <summary>
/// Authentication verification response.
/// </summary>
public sealed record AuthVerifyResponse(
    bool IsAuthenticated,
    string? UserId,
    string? Email
);

/// <summary>
/// Request to update user profile.
/// </summary>
public sealed record UpdateProfileRequest(
    string? Name,
    string? Bio,
    string? Occupation,
    string? ProfileImage
);
