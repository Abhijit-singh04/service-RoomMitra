namespace RoomMitra.Domain.Entities;

public sealed class Amenity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty; // Wifi, AC, Washing Machine, etc.
    public string? Description { get; set; }
    public string? Icon { get; set; } // Optional: for UI icons

    // Navigation Property
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
}
