namespace RoomMitra.Application.Models.Auth;

public sealed record LoginRequest(
    string Email,
    string Password
);
