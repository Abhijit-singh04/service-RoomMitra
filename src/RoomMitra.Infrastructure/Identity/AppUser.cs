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
}
