namespace RoomMitra.Application.Abstractions.Security;

/// <summary>
/// Represents the current authenticated user context.
/// Provides access to user identity information from the JWT token.
/// </summary>
public interface IUserContext
{
    /// <summary>
    /// The unique identifier of the user (Azure AD B2C Object ID or Subject).
    /// </summary>
    Guid? UserId { get; }

    /// <summary>
    /// The Azure AD B2C Object ID (oid claim).
    /// </summary>
    string? ObjectId { get; }

    /// <summary>
    /// The subject identifier from the token (sub claim).
    /// </summary>
    string? Subject { get; }

    /// <summary>
    /// The user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// The user's display name.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// The identity provider used for authentication (e.g., "google.com", "local").
    /// </summary>
    string? IdentityProvider { get; }

    /// <summary>
    /// The roles assigned to the user.
    /// </summary>
    IReadOnlyList<string> Roles { get; }

    /// <summary>
    /// Whether the current request is authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Checks if the user has a specific role.
    /// </summary>
    bool HasRole(string role);
}
