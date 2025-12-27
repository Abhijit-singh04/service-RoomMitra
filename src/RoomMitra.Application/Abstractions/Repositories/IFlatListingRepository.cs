using RoomMitra.Application.Abstractions.Common;
using RoomMitra.Application.Models.Listings;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Application.Abstractions.Repositories;

public interface IFlatListingRepository
{
    Task<FlatListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<PagedResult<FlatListing>> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken);

    Task AddAsync(FlatListing listing, CancellationToken cancellationToken);

    Task UpdateAsync(FlatListing listing, CancellationToken cancellationToken);

    Task DeleteAsync(FlatListing listing, CancellationToken cancellationToken);
}
