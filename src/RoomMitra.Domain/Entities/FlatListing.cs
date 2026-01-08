using RoomMitra.Domain.Common;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Domain.Entities;

public sealed class FlatListing : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string City { get; set; } = "Bengaluru";
    public string Locality { get; set; } = string.Empty;

    public FlatType FlatType { get; set; }
    public RoomType RoomType { get; set; }
    public Furnishing Furnishing { get; set; }

    public decimal Rent { get; set; }
    public decimal Deposit { get; set; }

    public List<string> Amenities { get; set; } = new();
    public List<string> Preferences { get; set; } = new();

    public DateOnly? AvailableFrom { get; set; }

    public List<string> Images { get; set; } = new();

    /// <summary>
    /// Latitude for geo-search. Only lat/lon stored, no full address.
    /// </summary>
    public double? Latitude { get; set; }
    
    /// <summary>
    /// Longitude for geo-search. Only lat/lon stored, no full address.
    /// </summary>
    public double? Longitude { get; set; }

    public Guid PostedByUserId { get; set; }
    
    /// <summary>
    /// Navigation property for nearby essentials (cached POI data)
    /// </summary>
    public List<NearbyEssential> NearbyEssentials { get; set; } = new();

    public ListingStatus Status { get; set; } = ListingStatus.Active;
}
