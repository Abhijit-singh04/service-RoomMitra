using RoomMitra.Domain.Enums;

namespace RoomMitra.Application.Models.Listings;

public sealed record ListingSearchQuery(
    string? City,
    string? Locality,
    decimal? MinRent,
    decimal? MaxRent,
    FlatType? FlatType,
    ListingSort Sort,
    int Page,
    int PageSize
);
