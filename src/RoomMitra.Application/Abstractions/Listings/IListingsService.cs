using RoomMitra.Application.Abstractions.Common;
using RoomMitra.Application.Models.Listings;

namespace RoomMitra.Application.Abstractions.Listings;

public interface IListingsService
{
    Task<PagedResult<FlatListingSummaryDto>> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken);

    Task<FlatListingDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    Task<Guid> CreateAsync(CreateListingRequest request, CancellationToken cancellationToken);

    Task<bool> UpdateAsync(Guid id, UpdateListingRequest request, CancellationToken cancellationToken);

    Task<bool> SetStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken);
}
