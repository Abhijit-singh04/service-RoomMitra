namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Response containing user info without the token.
/// Used for cookie-based auth where token is in HttpOnly cookie.
/// </summary>
public sealed record UserInfoResponse(
    Guid UserId,
    string Name,
    string Email,
    string? ProfileImageUrl,
    string? PhoneNumber,
    bool PhoneVerified,
    bool IsVerified,
    bool IsProfileComplete,
    string AuthProvider,
    bool IsNewUser = false,
    bool RequiresProfileCompletion = false
);
