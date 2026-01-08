using RoomMitra.Application.Models.Location;

namespace RoomMitra.Application.Abstractions.Location;

/// <summary>
/// Abstraction for location services.
/// All Azure Maps calls go through backend - never exposed to frontend.
/// </summary>
public interface ILocationService
{
    /// <summary>
    /// Search for locations using fuzzy search.
    /// Results are cached for 24h+ to minimize API calls.
    /// </summary>
    /// <param name="query">Search query (e.g., "Koramangala, Bengaluru")</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Top 5 location suggestions</returns>
    Task<IReadOnlyList<LocationSuggestion>> AutocompleteAsync(string query, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Reverse geocode coordinates to get a human-readable address.
    /// Results are cached for 24h+ to minimize API calls.
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lon">Longitude</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Human-readable address or null if not found</returns>
    Task<string?> ReverseGeocodeAsync(double lat, double lon, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if Azure Maps feature is enabled (feature flag for cost control)
    /// </summary>
    bool IsEnabled { get; }
}
