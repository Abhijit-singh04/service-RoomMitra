using RoomMitra.Domain.Enums;

namespace RoomMitra.Application.Models.Listings;

public sealed record FlatListingSummaryDto(
    Guid Id,
    string Title,
    string City,
    string Locality,
    FlatType FlatType,
    decimal Rent,
    string? CoverImageUrl,
    DateTimeOffset CreatedAt
);
