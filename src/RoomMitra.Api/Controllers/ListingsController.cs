using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Listings;
using RoomMitra.Application.Abstractions.Common;
using RoomMitra.Application.Models.Listings;
using RoomMitra.Domain.Enums;

namespace RoomMitra.Api.Controllers;

[ApiController]
[Route("api/listings")]
public sealed class ListingsController : ControllerBase
{
    private readonly IListingsService _listings;

    public ListingsController(IListingsService listings)
    {
        _listings = listings;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<FlatListingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Search(
        [FromQuery] string? city,
        [FromQuery] string? locality,
        [FromQuery] decimal? minRent,
        [FromQuery] decimal? maxRent,
        [FromQuery] FlatType? flatType,
        [FromQuery] ListingSort sort = ListingSort.Newest,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var query = new ListingSearchQuery(city, locality, minRent, maxRent, flatType, sort, page, pageSize);
        var result = await _listings.SearchAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FlatListingDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var listing = await _listings.GetByIdAsync(id, cancellationToken);
        return listing is null ? NotFound() : Ok(listing);
    }

    [Authorize]
    [HttpGet("my")]
    [ProducesResponseType(typeof(PagedResult<FlatListingSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyListings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken cancellationToken = default)
    {
        var result = await _listings.GetMyListingsAsync(page, pageSize, cancellationToken);
        return Ok(result);
    }

    [Authorize]
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateListingRequest request, CancellationToken cancellationToken)
    {
        if (request.Images is { Count: > 10 })
        {
            return Problem(title: "Validation error", detail: "Max 10 images per listing.", statusCode: StatusCodes.Status400BadRequest);
        }

        var id = await _listings.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateListingRequest request, CancellationToken cancellationToken)
    {
        if (request.Images is { Count: > 10 })
        {
            return Problem(title: "Validation error", detail: "Max 10 images per listing.", statusCode: StatusCodes.Status400BadRequest);
        }

        var ok = await _listings.UpdateAsync(id, request, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    public sealed record SetListingStatusRequest(bool IsActive);

    [Authorize]
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetStatus([FromRoute] Guid id, [FromBody] SetListingStatusRequest request, CancellationToken cancellationToken)
    {
        var ok = await _listings.SetStatusAsync(id, request.IsActive, cancellationToken);
        return ok ? NoContent() : NotFound();
    }

    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var ok = await _listings.DeleteAsync(id, cancellationToken);
        return ok ? NoContent() : NotFound();
    }
}
