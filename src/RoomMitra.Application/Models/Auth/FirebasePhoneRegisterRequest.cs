namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Request to register a new user after Firebase phone OTP verification.
/// Password is not required - OTP verification is sufficient for authentication.
/// </summary>
public sealed record FirebasePhoneRegisterRequest(
    /// <summary>
    /// Firebase ID Token obtained after OTP verification.
    /// </summary>
    string FirebaseIdToken,
    
    /// <summary>
    /// Username chosen by the user.
    /// </summary>
    string Username
);
