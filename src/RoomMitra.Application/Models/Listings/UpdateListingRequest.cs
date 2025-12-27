using RoomMitra.Domain.Enums;

namespace RoomMitra.Application.Models.Listings;

public sealed record UpdateListingRequest(
    string Title,
    string Description,
    string City,
    string Locality,
    FlatType FlatType,
    RoomType RoomType,
    Furnishing Furnishing,
    decimal Rent,
    decimal Deposit,
    List<string> Amenities,
    List<string> Preferences,
    DateOnly? AvailableFrom,
    List<string> Images
);
