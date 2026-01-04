namespace RoomMitra.Application.Models.Auth;

public sealed record AuthUserDto(
    Guid Id,
    string Name,
    string Email,
    string? ProfileImageUrl,
    string? PhoneNumber,
    bool PhoneVerified,
    bool IsVerified,
    bool IsProfileComplete,
    string AuthProvider
);
