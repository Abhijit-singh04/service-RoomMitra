using Microsoft.EntityFrameworkCore;
using RoomMitra.Application.Abstractions.Common;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Models.Listings;
using RoomMitra.Domain.Entities;
using RoomMitra.Domain.Enums;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Infrastructure.Repositories;

internal sealed class EfFlatListingRepository : IFlatListingRepository
{
    private readonly RoomMitraDbContext _db;

    public EfFlatListingRepository(RoomMitraDbContext db)
    {
        _db = db;
    }

    public Task<FlatListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.FlatListings.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }

    public async Task<PagedResult<FlatListing>> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 12 : query.PageSize;

        IQueryable<FlatListing> q = _db.FlatListings.AsNoTracking();

        q = q.Where(x => x.Status == ListingStatus.Active);

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            q = q.Where(x => x.City == city);
        }

        if (!string.IsNullOrWhiteSpace(query.Locality))
        {
            var locality = query.Locality.Trim();
            q = q.Where(x => x.Locality == locality);
        }

        if (query.MinRent is not null)
        {
            q = q.Where(x => x.Rent >= query.MinRent.Value);
        }

        if (query.MaxRent is not null)
        {
            q = q.Where(x => x.Rent <= query.MaxRent.Value);
        }

        if (query.FlatType is not null)
        {
            q = q.Where(x => x.FlatType == query.FlatType.Value);
        }

        q = query.Sort switch
        {
            ListingSort.RentAsc => q.OrderBy(x => x.Rent).ThenByDescending(x => x.CreatedAt),
            ListingSort.RentDesc => q.OrderByDescending(x => x.Rent).ThenByDescending(x => x.CreatedAt),
            _ => q.OrderByDescending(x => x.CreatedAt)
        };

        var total = await q.LongCountAsync(cancellationToken);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FlatListing>(items, page, pageSize, total);
    }

    public async Task<PagedResult<FlatListing>> GetByUserIdAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = page <= 0 ? 1 : page;
        pageSize = pageSize is < 1 or > 100 ? 12 : pageSize;

        IQueryable<FlatListing> q = _db.FlatListings.AsNoTracking();

        // Filter by user and exclude deleted listings
        q = q.Where(x => x.PostedByUserId == userId && x.Status != ListingStatus.Deleted);

        // Order by newest first
        q = q.OrderByDescending(x => x.CreatedAt);

        var total = await q.LongCountAsync(cancellationToken);

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<FlatListing>(items, page, pageSize, total);
    }

    public async Task AddAsync(FlatListing listing, CancellationToken cancellationToken)
    {
        _db.FlatListings.Add(listing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FlatListing listing, CancellationToken cancellationToken)
    {
        _db.FlatListings.Update(listing);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(FlatListing listing, CancellationToken cancellationToken)
    {
        _db.FlatListings.Remove(listing);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
