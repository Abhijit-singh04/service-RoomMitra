namespace RoomMitra.Domain.Entities;

/// <summary>
/// Join table for many-to-many relationship between Properties and Amenities
/// </summary>
public sealed class PropertyAmenity
{
    public Guid PropertyId { get; set; }
    public Property Property { get; set; } = null!;

    public Guid AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
