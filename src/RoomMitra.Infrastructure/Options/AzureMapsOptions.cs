namespace RoomMitra.Infrastructure.Options;

/// <summary>
/// Configuration for Azure Maps API.
/// Key is stored server-side only - never exposed to frontend.
/// </summary>
public sealed class AzureMapsOptions
{
    public const string SectionName = "AzureMaps";
    
    /// <summary>
    /// Azure Maps subscription key - NEVER expose to frontend
    /// </summary>
    public string SubscriptionKey { get; set; } = string.Empty;
    
    /// <summary>
    /// Base URL for Azure Maps REST API
    /// </summary>
    public string BaseUrl { get; set; } = "https://atlas.microsoft.com";
    
    /// <summary>
    /// Feature flag to disable maps if quota spikes
    /// </summary>
    public bool Enabled { get; set; } = true;
    
    /// <summary>
    /// Rate limit: max calls per minute to Azure Maps
    /// </summary>
    public int RateLimitPerMinute { get; set; } = 50;
    
    /// <summary>
    /// Cache TTL for autocomplete results in hours
    /// </summary>
    public int AutocompleteCacheTtlHours { get; set; } = 24;
}
