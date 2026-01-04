namespace RoomMitra.Application.Abstractions.Auth;

/// <summary>
/// Represents the result of verifying a Firebase ID token.
/// </summary>
public sealed record FirebaseTokenResult(
    string Uid,
    string? PhoneNumber,
    string? Email,
    bool IsValid
);

/// <summary>
/// Service for verifying Firebase ID tokens.
/// </summary>
public interface IFirebaseAuthService
{
    /// <summary>
    /// Verifies a Firebase ID token and extracts user claims.
    /// </summary>
    /// <param name="idToken">The Firebase ID token from the client.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The token verification result.</returns>
    Task<FirebaseTokenResult> VerifyIdTokenAsync(string idToken, CancellationToken cancellationToken = default);
}
