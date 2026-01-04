namespace RoomMitra.Application.Models.Auth;

public sealed record AuthResponse(
    string AccessToken,
    AuthUserDto User,
    bool IsNewUser = false,
    bool RequiresProfileCompletion = false
);
