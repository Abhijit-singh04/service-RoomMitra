using RoomMitra.Domain.Common;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Domain.Entities;

public sealed class Property : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Foreign Key to User
    public Guid UserId { get; set; }

    // Basic Information
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public PropertyType PropertyType { get; set; }

    // Financial Details
    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }
    public DateOnly? AvailableFrom { get; set; }

    // Location
    public string City { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }

    // Preferences
    public Gender PreferredGender { get; set; } = Gender.Any;
    public PreferredFood PreferredFood { get; set; } = PreferredFood.Any;
    public Furnishing Furnishing { get; set; }

    // Status
    public ListingStatus Status { get; set; } = ListingStatus.Active;

    // Navigation Properties
    public ICollection<PropertyImage> Images { get; set; } = new List<PropertyImage>();
    public ICollection<PropertyAmenity> PropertyAmenities { get; set; } = new List<PropertyAmenity>();
}
