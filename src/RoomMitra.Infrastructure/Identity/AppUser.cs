using Microsoft.AspNetCore.Identity;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Occupation { get; set; }
    public string? Bio { get; set; }
    public Gender? Gender { get; set; }
    public bool IsVerified { get; set; } = false;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? UpdatedAt { get; set; }
    
    /// <summary>
    /// The authentication provider used for sign-up: "google", "phone", or "email"
    /// </summary>
    public string AuthProvider { get; set; } = "email";
    
    /// <summary>
    /// External identity provider ID (e.g., Azure AD oid for Google users)
    /// </summary>
    public string? ExternalId { get; set; }
    
    /// <summary>
    /// Whether the user has completed their profile (name is set)
    /// </summary>
    public bool IsProfileComplete { get; set; } = false;
}
