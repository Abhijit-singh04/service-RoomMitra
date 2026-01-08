using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Location;
using RoomMitra.Application.Models.Location;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Location API endpoints.
/// All Azure Maps calls go through backend - key never exposed to frontend.
/// </summary>
[ApiController]
[Route("api/location")]
public sealed class LocationController : ControllerBase
{
    private readonly ILocationService _locationService;
    private readonly IGeoSearchService _geoSearchService;
    private readonly ILogger<LocationController> _logger;
    
    public LocationController(
        ILocationService locationService,
        IGeoSearchService geoSearchService,
        ILogger<LocationController> logger)
    {
        _locationService = locationService;
        _geoSearchService = geoSearchService;
        _logger = logger;
    }
    
    /// <summary>
    /// Address autocomplete using Azure Maps Fuzzy Search.
    /// Results cached for 24h+ to minimize API costs.
    /// </summary>
    /// <param name="q">Search query (min 3 characters)</param>
    [HttpGet("autocomplete")]
    [ProducesResponseType(typeof(IReadOnlyList<LocationSuggestion>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Autocomplete([FromQuery] string? q, CancellationToken cancellationToken)
    {
        // Feature flag check
        if (!_locationService.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Location service temporarily unavailable" });
        }
        
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
        {
            return Ok(Array.Empty<LocationSuggestion>());
        }
        
        var suggestions = await _locationService.AutocompleteAsync(q, cancellationToken);
        return Ok(suggestions);
    }
    
    /// <summary>
    /// Reverse geocode coordinates to get a human-readable address.
    /// Results cached for 24h+ to minimize API costs.
    /// </summary>
    /// <param name="lat">Latitude</param>
    /// <param name="lon">Longitude</param>
    [HttpGet("reverse")]
    [ProducesResponseType(typeof(ReverseGeocodeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> ReverseGeocode([FromQuery] double lat, [FromQuery] double lon, CancellationToken cancellationToken)
    {
        // Feature flag check
        if (!_locationService.IsEnabled)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, 
                new { error = "Location service temporarily unavailable" });
        }
        
        var address = await _locationService.ReverseGeocodeAsync(lat, lon, cancellationToken);
        return Ok(new ReverseGeocodeResponse { Address = address });
    }
    
    /// <summary>
    /// Search listings within radius using Haversine formula.
    /// No external API call - purely DB-based.
    /// Radius clamped between 0.5km and 20km.
    /// </summary>
    [HttpPost("nearby-listings")]
    [ProducesResponseType(typeof(NearbyListingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> NearbyListings([FromBody] NearbySearchRequest request, CancellationToken cancellationToken)
    {
        var results = await _geoSearchService.SearchWithinRadiusAsync(request, cancellationToken);
        
        var response = new NearbyListingsResponse
        {
            Listings = results.Select(r => new NearbyListingDto
            {
                Id = r.Listing.Id,
                Title = r.Listing.Title,
                Locality = r.Listing.Locality,
                City = r.Listing.City,
                FlatType = r.Listing.FlatType.ToString(),
                RoomType = r.Listing.RoomType.ToString(),
                Furnishing = r.Listing.Furnishing.ToString(),
                Rent = r.Listing.Rent,
                DistanceKm = r.DistanceKm,
                CoverImage = r.Listing.Images.FirstOrDefault() ?? string.Empty,
                CreatedAt = r.Listing.CreatedAt
            }).ToList(),
            TotalCount = results.Count,
            RadiusKm = request.ClampedRadiusKm
        };
        
        return Ok(response);
    }
}

/// <summary>
/// Response model for nearby listings search
/// </summary>
public sealed class NearbyListingsResponse
{
    public List<NearbyListingDto> Listings { get; set; } = new();
    public int TotalCount { get; set; }
    public double RadiusKm { get; set; }
}

/// <summary>
/// Listing DTO with distance info
/// </summary>
public sealed class NearbyListingDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Locality { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string FlatType { get; set; } = string.Empty;
    public string RoomType { get; set; } = string.Empty;
    public string Furnishing { get; set; } = string.Empty;
    public decimal Rent { get; set; }
    public double DistanceKm { get; set; }
    public string CoverImage { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    
    public string DistanceDisplay => DistanceKm >= 1
        ? $"{DistanceKm:F1}km away"
        : $"{(int)(DistanceKm * 1000)}m away";
}

/// <summary>
/// Response model for reverse geocode
/// </summary>
public sealed class ReverseGeocodeResponse
{
    public string? Address { get; set; }
}
