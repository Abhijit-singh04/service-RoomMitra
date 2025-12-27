namespace RoomMitra.Application.Models.Auth;

public sealed record AuthResponse(
    string AccessToken,
    AuthUserDto User
);
