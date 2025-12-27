using Microsoft.AspNetCore.Identity;

namespace RoomMitra.Infrastructure.Identity;

public sealed class AppUser : IdentityUser<Guid>
{
    public string Name { get; set; } = string.Empty;
    public string? ProfileImageUrl { get; set; }
    public string? Occupation { get; set; }
    public string? Bio { get; set; }
}
