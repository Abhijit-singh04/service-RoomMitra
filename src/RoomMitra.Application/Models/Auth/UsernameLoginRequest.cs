namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Request to login using username and password.
/// </summary>
public sealed record UsernameLoginRequest(
    /// <summary>
    /// Username of the user.
    /// </summary>
    string Username,
    
    /// <summary>
    /// Password of the user.
    /// </summary>
    string Password
);
