namespace RoomMitra.Domain.Entities;

/// <summary>
/// Stores cached nearby POI data for a flat listing.
/// Persisted permanently unless listing location changes.
/// </summary>
public sealed class NearbyEssential
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    /// <summary>
    /// Reference to the parent FlatListing
    /// </summary>
    public Guid FlatListingId { get; set; }
    
    /// <summary>
    /// POI category: metro_station, grocery_or_supermarket, hospital
    /// </summary>
    public string Category { get; set; } = string.Empty;
    
    /// <summary>
    /// Name of the POI (e.g., "Indiranagar Metro Station")
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Distance from listing in meters
    /// </summary>
    public int DistanceMeters { get; set; }
    
    /// <summary>
    /// When this POI data was fetched
    /// </summary>
    public DateTime FetchedAt { get; set; } = DateTime.UtcNow;
}
