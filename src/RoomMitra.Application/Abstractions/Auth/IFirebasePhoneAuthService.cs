using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Application.Abstractions.Auth;

/// <summary>
/// Service for handling Firebase phone-based authentication flows.
/// This is separate from the existing auth flows (Google, Gmail, Azure AD).
/// </summary>
public interface IFirebasePhoneAuthService
{
    /// <summary>
    /// Verifies a Firebase ID token and checks if the phone number is associated with an existing user.
    /// </summary>
    /// <param name="request">The Firebase token verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Verification response indicating if user exists.</returns>
    Task<FirebasePhoneVerifyResponse> VerifyPhoneAsync(FirebasePhoneVerifyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Registers a new user after Firebase phone OTP verification.
    /// No password required - OTP verification is sufficient for authentication.
    /// </summary>
    /// <param name="request">The registration request with Firebase token and username.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with access token and user info.</returns>
    Task<AuthResponse> RegisterWithPhoneAsync(FirebasePhoneRegisterRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates an existing user using Firebase ID token (OTP verification).
    /// No password required - OTP verification is sufficient.
    /// </summary>
    /// <param name="request">The Firebase token verification request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with access token and user info.</returns>
    Task<AuthResponse> LoginWithPhoneAsync(FirebasePhoneVerifyRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Resets user password after Firebase phone OTP verification.
    /// </summary>
    /// <param name="request">The password reset request with Firebase token and new password.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Auth response with new access token and user info.</returns>
    Task<AuthResponse> ResetPasswordAsync(FirebasePasswordResetRequest request, CancellationToken cancellationToken);
}
