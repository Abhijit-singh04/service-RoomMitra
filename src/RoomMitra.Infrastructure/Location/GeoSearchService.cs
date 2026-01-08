using Microsoft.EntityFrameworkCore;
using RoomMitra.Application.Abstractions.Location;
using RoomMitra.Application.Models.Location;
using RoomMitra.Domain.Enums;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Infrastructure.Location;

/// <summary>
/// Geo search service using Haversine formula.
/// No external API calls - purely in-memory/DB calculation.
/// </summary>
internal sealed class GeoSearchService : IGeoSearchService
{
    private readonly RoomMitraDbContext _db;
    
    // Earth's radius in kilometers
    private const double EarthRadiusKm = 6371.0;
    
    public GeoSearchService(RoomMitraDbContext db)
    {
        _db = db;
    }
    
    /// <summary>
    /// Calculate distance using Haversine formula.
    /// Returns distance in kilometers.
    /// </summary>
    public double CalculateDistanceKm(double lat1, double lon1, double lat2, double lon2)
    {
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        
        return EarthRadiusKm * c;
    }
    
    public async Task<IReadOnlyList<ListingWithDistance>> SearchWithinRadiusAsync(
        NearbySearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var radiusKm = request.ClampedRadiusKm; // Clamped between 0.5-20km
        
        // First, get all active listings with coordinates
        // For MVP, we load in-memory and filter. For scale, use PostGIS.
        var listings = await _db.FlatListings
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active)
            .Where(l => l.Latitude != null && l.Longitude != null)
            .ToListAsync(cancellationToken);
        
        // Calculate distances and filter
        var results = listings
            .Select(l => new
            {
                Listing = l,
                Distance = CalculateDistanceKm(
                    request.Lat, request.Lon,
                    l.Latitude!.Value, l.Longitude!.Value)
            })
            .Where(x => x.Distance <= radiusKm)
            .OrderBy(x => x.Distance)
            .Select(x => new ListingWithDistance(x.Listing, Math.Round(x.Distance, 2)))
            .ToList();
        
        return results;
    }
    
    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
