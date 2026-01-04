namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Request to verify Firebase phone and check if user exists.
/// </summary>
public sealed record FirebasePhoneVerifyRequest(
    /// <summary>
    /// Firebase ID Token obtained after OTP verification.
    /// </summary>
    string FirebaseIdToken
);
