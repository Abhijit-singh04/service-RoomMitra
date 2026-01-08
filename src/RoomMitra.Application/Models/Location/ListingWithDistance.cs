using RoomMitra.Domain.Entities;

namespace RoomMitra.Application.Models.Location;

/// <summary>
/// Listing result with calculated distance from search point.
/// </summary>
public sealed record ListingWithDistance(
    FlatListing Listing,
    double DistanceKm
);
