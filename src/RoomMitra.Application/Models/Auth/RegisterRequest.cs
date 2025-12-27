namespace RoomMitra.Application.Models.Auth;

public sealed record RegisterRequest(
    string Name,
    string Email,
    string Password
);
