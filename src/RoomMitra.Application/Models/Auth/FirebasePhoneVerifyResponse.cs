namespace RoomMitra.Application.Models.Auth;

/// <summary>
/// Response after verifying Firebase phone number.
/// </summary>
public sealed record FirebasePhoneVerifyResponse(
    /// <summary>
    /// Whether the phone number is associated with an existing user.
    /// </summary>
    bool UserExists,
    
    /// <summary>
    /// The phone number from Firebase token.
    /// </summary>
    string PhoneNumber,
    
    /// <summary>
    /// The username if user exists (for display purposes).
    /// </summary>
    string? Username
);
