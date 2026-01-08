namespace RoomMitra.Application.Abstractions.Location;

/// <summary>
/// Simple in-memory cache abstraction for location data.
/// Falls back to in-memory if Redis is not configured.
/// </summary>
public interface ILocationCache
{
    /// <summary>
    /// Get cached value
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Set cached value with TTL
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class;
    
    /// <summary>
    /// Remove cached value
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);
}
