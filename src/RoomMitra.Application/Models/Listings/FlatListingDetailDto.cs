using RoomMitra.Domain.Enums;

namespace RoomMitra.Application.Models.Listings;

public sealed record FlatListingDetailDto(
    Guid Id,
    string Title,
    string Description,
    string City,
    string Locality,
    FlatType FlatType,
    RoomType RoomType,
    Furnishing Furnishing,
    decimal Rent,
    decimal Deposit,
    IReadOnlyList<string> Amenities,
    IReadOnlyList<string> Preferences,
    DateOnly? AvailableFrom,
    IReadOnlyList<string> Images,
    Guid PostedByUserId,
    ListingStatus Status,
    DateTimeOffset CreatedAt
);
