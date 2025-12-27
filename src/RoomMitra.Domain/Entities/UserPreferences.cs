using RoomMitra.Domain.Common;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Domain.Entities;

public sealed class UserPreferences : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key to User
    public Guid UserId { get; set; }

    // Budget Preferences
    public decimal? BudgetMin { get; set; }
    public decimal? BudgetMax { get; set; }

    // Location Preferences
    public string? PreferredCity { get; set; }
    public List<string> PreferredAreas { get; set; } = new();

    // Roommate Preferences
    public Gender? PreferredGender { get; set; }
    public PreferredFood? PreferredFood { get; set; }

    // Property Preferences
    public PropertyType? PreferredPropertyType { get; set; }
    public Furnishing? PreferredFurnishing { get; set; }

    // Move-in Timeline
    public DateOnly? MoveInDate { get; set; }
}
