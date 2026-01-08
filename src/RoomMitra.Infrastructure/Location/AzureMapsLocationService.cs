using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RoomMitra.Application.Abstractions.Location;
using RoomMitra.Application.Models.Location;
using RoomMitra.Infrastructure.Options;

namespace RoomMitra.Infrastructure.Location;

/// <summary>
/// Azure Maps service implementation.
/// - All API keys kept server-side
/// - Rate limiting to control costs
/// - Aggressive caching (24h+ for autocomplete, permanent for POIs)
/// </summary>
internal sealed class AzureMapsLocationService : ILocationService
{
    private readonly HttpClient _httpClient;
    private readonly AzureMapsOptions _options;
    private readonly ILocationCache _cache;
    private readonly ILogger<AzureMapsLocationService> _logger;
    
    // Rate limiting: track calls per minute
    private readonly ConcurrentQueue<DateTime> _recentCalls = new();
    private readonly object _rateLimitLock = new();
    
    // Cache key prefixes
    private const string AutocompleteCachePrefix = "location:autocomplete:";
    private const string ReverseGeocodeCachePrefix = "location:reverse:";
    
    public AzureMapsLocationService(
        HttpClient httpClient,
        IOptions<AzureMapsOptions> options,
        ILocationCache cache,
        ILogger<AzureMapsLocationService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _cache = cache;
        _logger = logger;
    }
    
    public bool IsEnabled => _options.Enabled;
    
    public async Task<IReadOnlyList<LocationSuggestion>> AutocompleteAsync(string query, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Azure Maps is disabled via feature flag");
            return [];
        }
        
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
        {
            return [];
        }
        
        // Normalize query for cache key
        var normalizedQuery = query.Trim().ToLowerInvariant();
        var cacheKey = $"{AutocompleteCachePrefix}{normalizedQuery}";
        
        // Check cache first
        var cached = await _cache.GetAsync<List<LocationSuggestion>>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Autocomplete cache hit for: {Query}", normalizedQuery);
            return cached;
        }
        
        // Rate limit check
        if (!TryAcquireRateLimit())
        {
            _logger.LogWarning("Azure Maps rate limit exceeded");
            return [];
        }
        
        try
        {
            // Azure Maps Fuzzy Search API with typeahead for better autocomplete
            // Enhanced parameters for better locality/area/building matching
            var url = $"{_options.BaseUrl}/search/fuzzy/json" +
                      $"?api-version=1.0" +
                      $"&subscription-key={_options.SubscriptionKey}" +
                      $"&query={Uri.EscapeDataString(query)}" +
                      $"&countrySet=IN" +              // India only
                      $"&limit=20" +                   // Fetch more to filter best
                      $"&typeahead=true" +             // Enable typeahead for partial matches
                      $"&idxSet=Geo,PAD,Addr,Str,POI" + // Geography, Point Addresses, Address ranges, Streets, POIs (buildings)
                      $"&language=en-US";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadFromJsonAsync<AzureMapsSearchResponse>(JsonOptions, cancellationToken);
            
            var suggestions = json?.Results?
                .Where(r => r.Position is not null)
                .Select(r => new { Result = r, Label = FormatLabel(r) })
                .DistinctBy(x => x.Label) // Remove duplicates based on label
                .Take(10)                  // Return up to 10 unique results
                .Select(x => new LocationSuggestion(
                    Label: x.Label,
                    Lat: x.Result.Position!.Lat,
                    Lon: x.Result.Position.Lon
                ))
                .ToList() ?? [];
            
            // Cache for 24+ hours
            var ttl = TimeSpan.FromHours(_options.AutocompleteCacheTtlHours);
            await _cache.SetAsync(cacheKey, suggestions, ttl, cancellationToken);
            
            _logger.LogDebug("Autocomplete API call for: {Query}, found {Count} results", query, suggestions.Count);
            return suggestions;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Maps autocomplete failed for query: {Query}", query);
            return [];
        }
    }
    
    public async Task<string?> ReverseGeocodeAsync(double lat, double lon, CancellationToken cancellationToken = default)
    {
        if (!_options.Enabled)
        {
            _logger.LogWarning("Azure Maps is disabled via feature flag");
            return null;
        }
        
        // Round coordinates to 4 decimal places for cache key (about 10m precision)
        var roundedLat = Math.Round(lat, 4);
        var roundedLon = Math.Round(lon, 4);
        var cacheKey = $"{ReverseGeocodeCachePrefix}{roundedLat},{roundedLon}";
        
        // Check cache first
        var cached = await _cache.GetAsync<string>(cacheKey, cancellationToken);
        if (cached is not null)
        {
            _logger.LogDebug("Reverse geocode cache hit for: {Lat},{Lon}", roundedLat, roundedLon);
            return cached;
        }
        
        // Rate limit check
        if (!TryAcquireRateLimit())
        {
            _logger.LogWarning("Azure Maps rate limit exceeded");
            return null;
        }
        
        try
        {
            // First try: Find nearest POI (building, shop, restaurant, landmark) within 200m
            var poiResult = await FindNearestPoiAsync(lat, lon, cancellationToken);
            if (poiResult is not null)
            {
                // Cache for 24+ hours
                var ttl = TimeSpan.FromHours(_options.AutocompleteCacheTtlHours);
                await _cache.SetAsync(cacheKey, poiResult, ttl, cancellationToken);
                
                _logger.LogDebug("Reverse geocode (POI) for: {Lat},{Lon}, result: {Result}", lat, lon, poiResult);
                return poiResult;
            }
            
            // Fallback: Use reverse address geocode
            var addressResult = await GetReverseAddressAsync(lat, lon, cancellationToken);
            if (addressResult is not null)
            {
                var ttl = TimeSpan.FromHours(_options.AutocompleteCacheTtlHours);
                await _cache.SetAsync(cacheKey, addressResult, ttl, cancellationToken);
                
                _logger.LogDebug("Reverse geocode (address) for: {Lat},{Lon}, result: {Result}", lat, lon, addressResult);
                return addressResult;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Azure Maps reverse geocode failed for: {Lat},{Lon}", lat, lon);
            return null;
        }
    }
    
    /// <summary>
    /// Find nearest POI (building, shop, restaurant, landmark) using Nearby Search
    /// </summary>
    private async Task<string?> FindNearestPoiAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        try
        {
            // Azure Maps Nearby Search - find POIs within 200m radius
            var url = $"{_options.BaseUrl}/search/nearby/json" +
                      $"?api-version=1.0" +
                      $"&subscription-key={_options.SubscriptionKey}" +
                      $"&lat={lat}" +
                      $"&lon={lon}" +
                      $"&radius=200" +   // 200 meters radius
                      $"&limit=5" +      // Get top 5 nearest
                      $"&language=en-US";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadFromJsonAsync<AzureMapsSearchResponse>(JsonOptions, cancellationToken);
            
            // Find first POI with a name
            var poi = json?.Results?
                .Where(r => !string.IsNullOrEmpty(r.Poi?.Name))
                .OrderBy(r => r.Distance ?? double.MaxValue)
                .FirstOrDefault();
            
            if (poi?.Poi?.Name is not null)
            {
                // Format as "POI Name, Locality" for context
                var poiName = poi.Poi.Name;
                var locality = poi.Address?.MunicipalitySubdivision ?? poi.Address?.Municipality;
                
                return !string.IsNullOrEmpty(locality) 
                    ? $"{poiName}, {locality}" 
                    : poiName;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Nearby POI search failed, will fallback to address");
            return null;
        }
    }
    
    /// <summary>
    /// Get address from coordinates using reverse geocode
    /// </summary>
    private async Task<string?> GetReverseAddressAsync(double lat, double lon, CancellationToken cancellationToken)
    {
        try
        {
            var url = $"{_options.BaseUrl}/search/address/reverse/json" +
                      $"?api-version=1.0" +
                      $"&subscription-key={_options.SubscriptionKey}" +
                      $"&query={lat},{lon}" +
                      $"&language=en-US";
            
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var json = await response.Content.ReadFromJsonAsync<AzureMapsReverseResponse>(JsonOptions, cancellationToken);
            
            var address = json?.Addresses?.FirstOrDefault()?.Address;
            if (address is null)
            {
                return null;
            }
            
            return FormatReverseAddress(address);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reverse address geocode failed");
            return null;
        }
    }
    
    private static string FormatReverseAddress(AzureMapsAddress address)
    {
        // Prefer street + locality for more specific addresses
        var parts = new List<string>();
        
        // Street name for specificity
        if (!string.IsNullOrEmpty(address.StreetName))
            parts.Add(address.StreetName);
        
        // Locality/Area (e.g., Koramangala, Indiranagar)
        if (!string.IsNullOrEmpty(address.MunicipalitySubdivision))
            parts.Add(address.MunicipalitySubdivision);
        
        // City
        if (!string.IsNullOrEmpty(address.Municipality))
            parts.Add(address.Municipality);
        
        if (parts.Count >= 1)
            return string.Join(", ", parts);
        
        // Fall back to freeform address
        if (!string.IsNullOrEmpty(address.FreeformAddress))
            return address.FreeformAddress;
        
        return "Unknown location";
    }
    
    /// <summary>
    /// Rate limiting using sliding window
    /// </summary>
    private bool TryAcquireRateLimit()
    {
        lock (_rateLimitLock)
        {
            var now = DateTime.UtcNow;
            var oneMinuteAgo = now.AddMinutes(-1);
            
            // Remove old entries
            while (_recentCalls.TryPeek(out var oldest) && oldest < oneMinuteAgo)
            {
                _recentCalls.TryDequeue(out _);
            }
            
            // Check if we're at the limit
            if (_recentCalls.Count >= _options.RateLimitPerMinute)
            {
                return false;
            }
            
            // Record this call
            _recentCalls.Enqueue(now);
            return true;
        }
    }
    
    private static string FormatLabel(AzureMapsResult result)
    {
        // For POIs (buildings, landmarks), prioritize POI name
        if (!string.IsNullOrEmpty(result.Poi?.Name))
        {
            var poiParts = new List<string> { result.Poi.Name };
            
            // Add locality for context
            if (!string.IsNullOrEmpty(result.Address?.MunicipalitySubdivision))
                poiParts.Add(result.Address.MunicipalitySubdivision);
            else if (!string.IsNullOrEmpty(result.Address?.Municipality))
                poiParts.Add(result.Address.Municipality);
            
            return string.Join(", ", poiParts);
        }
        
        // For addresses: "StreetName, Locality, City"
        var parts = new List<string>();
        
        // Street name (if available)
        if (!string.IsNullOrEmpty(result.Address?.StreetName))
            parts.Add(result.Address.StreetName);
        
        // Locality/Area (e.g., Koramangala, Indiranagar)
        if (!string.IsNullOrEmpty(result.Address?.MunicipalitySubdivision))
            parts.Add(result.Address.MunicipalitySubdivision);
        
        // City
        if (!string.IsNullOrEmpty(result.Address?.Municipality))
            parts.Add(result.Address.Municipality);
        
        // If we have at least locality + city, use that
        if (parts.Count >= 2)
            return string.Join(", ", parts);
        
        // Fall back to freeform address if available and more descriptive
        if (!string.IsNullOrEmpty(result.Address?.FreeformAddress) && result.Address.FreeformAddress.Length > 5)
            return result.Address.FreeformAddress;
        
        return parts.Count > 0 ? string.Join(", ", parts) : "Unknown location";
    }
    
    // JSON options for case-insensitive deserialization (Azure Maps uses camelCase)
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };
    
    // Azure Maps API response models (internal)
    private sealed class AzureMapsSearchResponse
    {
        [JsonPropertyName("results")]
        public List<AzureMapsResult>? Results { get; set; }
    }
    
    private sealed class AzureMapsResult
    {
        [JsonPropertyName("position")]
        public AzureMapsPosition? Position { get; set; }
        
        [JsonPropertyName("address")]
        public AzureMapsAddress? Address { get; set; }
        
        [JsonPropertyName("poi")]
        public AzureMapsPoi? Poi { get; set; }
        
        [JsonPropertyName("dist")]
        public double? Distance { get; set; }
        
        [JsonPropertyName("type")]
        public string? Type { get; set; }
    }
    
    private sealed class AzureMapsPosition
    {
        [JsonPropertyName("lat")]
        public double Lat { get; set; }
        
        [JsonPropertyName("lon")]
        public double Lon { get; set; }
    }
    
    private sealed class AzureMapsAddress
    {
        [JsonPropertyName("freeformAddress")]
        public string? FreeformAddress { get; set; }
        
        [JsonPropertyName("municipality")]
        public string? Municipality { get; set; }
        
        [JsonPropertyName("municipalitySubdivision")]
        public string? MunicipalitySubdivision { get; set; }
        
        [JsonPropertyName("streetName")]
        public string? StreetName { get; set; }
        
        [JsonPropertyName("countrySubdivision")]
        public string? CountrySubdivision { get; set; } // State
    }
    
    private sealed class AzureMapsPoi
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
    
    private sealed class AzureMapsReverseResponse
    {
        [JsonPropertyName("addresses")]
        public List<AzureMapsReverseAddress>? Addresses { get; set; }
    }
    
    private sealed class AzureMapsReverseAddress
    {
        [JsonPropertyName("address")]
        public AzureMapsAddress? Address { get; set; }
    }
}
