using System.ComponentModel.DataAnnotations;

namespace RoomMitra.Application.Models.Auth;

public sealed class CompleteProfileRequest
{
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; init; } = string.Empty;
    
    [EmailAddress]
    [StringLength(256)]
    public string? Email { get; init; }
    
    [StringLength(100)]
    public string? Occupation { get; init; }
    
    [StringLength(1000)]
    public string? Bio { get; init; }
}
