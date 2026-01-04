using RoomMitra.Application.Models.Auth;

namespace RoomMitra.Application.Abstractions.Auth;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<RequestOtpResponse> RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken);

    Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// Complete profile for users who signed up via Phone OTP.
    /// </summary>
    Task<AuthResponse> CompleteProfileAsync(Guid userId, CompleteProfileRequest request, CancellationToken cancellationToken);
    
    /// <summary>
    /// Sync user from external identity provider (Azure AD B2C / Google).
    /// Creates user if not exists, updates if exists.
    /// </summary>
    Task<AuthResponse> SyncExternalUserAsync(ExternalUserInfo externalUser, CancellationToken cancellationToken);
    
    /// <summary>
    /// Validate a JWT token and return user info if valid.
    /// Used for cookie-based session restoration.
    /// </summary>
    Task<UserInfoResponse?> ValidateTokenAndGetUserAsync(string token, CancellationToken cancellationToken);
}

/// <summary>
/// User information from external identity provider (Azure AD B2C / Google)
/// </summary>
public sealed record ExternalUserInfo(
    string ObjectId,
    string? Email,
    string? Name,
    string? ProfileImageUrl,
    string IdentityProvider
);
