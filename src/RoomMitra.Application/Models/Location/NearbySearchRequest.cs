namespace RoomMitra.Application.Models.Location;

/// <summary>
/// Request for radius-based listing search.
/// </summary>
public sealed record NearbySearchRequest(
    double Lat,
    double Lon,
    double RadiusKm
)
{
    /// <summary>
    /// Clamped radius: min 0.5km, max 20km
    /// </summary>
    public double ClampedRadiusKm => Math.Min(Math.Max(RadiusKm, 0.5), 20.0);
}
