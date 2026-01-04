namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Request to reset password after Firebase phone OTP verification.
/// </summary>
public sealed record FirebasePasswordResetRequest(
    /// <summary>
    /// Firebase ID Token obtained after OTP verification.
    /// </summary>
    string FirebaseIdToken,
    
    /// <summary>
    /// New password chosen by the user.
    /// </summary>
    string NewPassword
);
