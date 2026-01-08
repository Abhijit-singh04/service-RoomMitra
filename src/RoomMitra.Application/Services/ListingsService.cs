using RoomMitra.Application.Abstractions.Common;
using RoomMitra.Application.Abstractions.Listings;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Abstractions.Security;
using RoomMitra.Application.Abstractions.Time;
using RoomMitra.Application.Models.Listings;
using RoomMitra.Domain.Entities;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Application.Services;

public sealed class ListingsService : IListingsService
{
    private readonly IFlatListingRepository _repository;
    private readonly IUserContext _userContext;
    private readonly IClock _clock;

    public ListingsService(IFlatListingRepository repository, IUserContext userContext, IClock clock)
    {
        _repository = repository;
        _userContext = userContext;
        _clock = clock;
    }

    public async Task<PagedResult<FlatListingSummaryDto>> SearchAsync(ListingSearchQuery query, CancellationToken cancellationToken)
    {
        var result = await _repository.SearchAsync(query, cancellationToken);

        var dtos = result.Items
            .Select(x => new FlatListingSummaryDto(
                x.Id,
                x.Title,
                x.City,
                x.Locality,
                x.FlatType,
                x.Rent,
                x.Images.Count > 0 ? x.Images[0] : null,
                x.CreatedAt
            ))
            .ToList();

        return new PagedResult<FlatListingSummaryDto>(dtos, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<PagedResult<FlatListingSummaryDto>> GetMyListingsAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        if (userId is null)
        {
            return new PagedResult<FlatListingSummaryDto>(new List<FlatListingSummaryDto>(), page, pageSize, 0);
        }

        var result = await _repository.GetByUserIdAsync(userId.Value, page, pageSize, cancellationToken);

        var dtos = result.Items
            .Select(x => new FlatListingSummaryDto(
                x.Id,
                x.Title,
                x.City,
                x.Locality,
                x.FlatType,
                x.Rent,
                x.Images.Count > 0 ? x.Images[0] : null,
                x.CreatedAt
            ))
            .ToList();

        return new PagedResult<FlatListingSummaryDto>(dtos, result.Page, result.PageSize, result.TotalCount);
    }

    public async Task<FlatListingDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return null;
        }

        return new FlatListingDetailDto(
            listing.Id,
            listing.Title,
            listing.Description,
            listing.City,
            listing.Locality,
            listing.FlatType,
            listing.RoomType,
            listing.Furnishing,
            listing.Rent,
            listing.Deposit,
            listing.Amenities,
            listing.Preferences,
            listing.AvailableFrom,
            listing.Images,
            listing.PostedByUserId,
            listing.Status,
            listing.CreatedAt,
            listing.Latitude,
            listing.Longitude
        );
    }

    public async Task<Guid> CreateAsync(CreateListingRequest request, CancellationToken cancellationToken)
    {
        // Use authenticated user or a dev user ID for local development
        var userId = _userContext.UserId ?? Guid.Parse("00000000-0000-0000-0000-000000000001");

        var listing = new FlatListing
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            City = string.IsNullOrWhiteSpace(request.City) ? "Bengaluru" : request.City.Trim(),
            Locality = request.Locality.Trim(),
            FlatType = request.FlatType,
            RoomType = request.RoomType,
            Furnishing = request.Furnishing,
            Rent = request.Rent,
            Deposit = request.Deposit,
            Amenities = request.Amenities ?? new(),
            Preferences = request.Preferences ?? new(),
            AvailableFrom = request.AvailableFrom,
            Images = request.Images ?? new(),
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            PostedByUserId = userId,
            Status = ListingStatus.Active,
            CreatedAt = _clock.UtcNow
        };

        await _repository.AddAsync(listing, cancellationToken);

        return listing.Id;
    }

    public async Task<bool> UpdateAsync(Guid id, UpdateListingRequest request, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        if (userId is null)
        {
            return false;
        }

        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null || listing.Status == ListingStatus.Deleted)
        {
            return false;
        }

        if (listing.PostedByUserId != userId.Value)
        {
            return false;
        }

        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.City = string.IsNullOrWhiteSpace(request.City) ? listing.City : request.City.Trim();
        listing.Locality = request.Locality.Trim();
        listing.FlatType = request.FlatType;
        listing.RoomType = request.RoomType;
        listing.Furnishing = request.Furnishing;
        listing.Rent = request.Rent;
        listing.Deposit = request.Deposit;
        listing.Amenities = request.Amenities ?? new();
        listing.Preferences = request.Preferences ?? new();
        listing.AvailableFrom = request.AvailableFrom;
        listing.Images = request.Images ?? new();
        listing.UpdatedAt = _clock.UtcNow;

        await _repository.UpdateAsync(listing, cancellationToken);
        return true;
    }

    public async Task<bool> SetStatusAsync(Guid id, bool isActive, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        if (userId is null)
        {
            return false;
        }

        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null || listing.Status == ListingStatus.Deleted)
        {
            return false;
        }

        if (listing.PostedByUserId != userId.Value)
        {
            return false;
        }

        listing.Status = isActive ? ListingStatus.Active : ListingStatus.Inactive;
        listing.UpdatedAt = _clock.UtcNow;

        await _repository.UpdateAsync(listing, cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId;
        if (userId is null)
        {
            return false;
        }

        var listing = await _repository.GetByIdAsync(id, cancellationToken);
        if (listing is null)
        {
            return false;
        }

        if (listing.PostedByUserId != userId.Value)
        {
            return false;
        }

        listing.Status = ListingStatus.Deleted;
        listing.UpdatedAt = _clock.UtcNow;

        await _repository.UpdateAsync(listing, cancellationToken);
        return true;
    }
}
