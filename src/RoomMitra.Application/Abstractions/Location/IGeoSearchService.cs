using RoomMitra.Application.Models.Location;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Application.Abstractions.Location;

/// <summary>
/// Service for radius-based listing search using Haversine formula.
/// No external API calls - purely DB-based.
/// </summary>
public interface IGeoSearchService
{
    /// <summary>
    /// Calculate distance between two points using Haversine formula.
    /// </summary>
    double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2);
    
    /// <summary>
    /// Search listings within radius.
    /// Radius is hard-limited to 5km max for cost/performance.
    /// </summary>
    Task<IReadOnlyList<ListingWithDistance>> SearchWithinRadiusAsync(
        NearbySearchRequest request,
        CancellationToken cancellationToken = default);
}
