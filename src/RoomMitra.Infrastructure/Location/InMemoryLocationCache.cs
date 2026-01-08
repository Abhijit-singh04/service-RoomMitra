using System.Collections.Concurrent;
using System.Text.Json;
using RoomMitra.Application.Abstractions.Location;

namespace RoomMitra.Infrastructure.Location;

/// <summary>
/// In-memory cache implementation for location data.
/// Thread-safe using ConcurrentDictionary.
/// Falls back to this if Redis is not configured.
/// </summary>
internal sealed class InMemoryLocationCache : ILocationCache
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    
    // Periodic cleanup of expired entries
    private readonly Timer _cleanupTimer;
    
    public InMemoryLocationCache()
    {
        // Cleanup every 10 minutes
        _cleanupTimer = new Timer(CleanupExpired, null, TimeSpan.FromMinutes(10), TimeSpan.FromMinutes(10));
    }
    
    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            if (entry.ExpiresAt > DateTime.UtcNow)
            {
                // Deserialize from JSON to handle complex types
                var value = JsonSerializer.Deserialize<T>(entry.JsonValue);
                return Task.FromResult(value);
            }
            
            // Entry expired, remove it
            _cache.TryRemove(key, out _);
        }
        
        return Task.FromResult<T?>(null);
    }
    
    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default) where T : class
    {
        var entry = new CacheEntry
        {
            JsonValue = JsonSerializer.Serialize(value),
            ExpiresAt = DateTime.UtcNow.Add(ttl)
        };
        
        _cache[key] = entry;
        return Task.CompletedTask;
    }
    
    public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        _cache.TryRemove(key, out _);
        return Task.CompletedTask;
    }
    
    private void CleanupExpired(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = _cache
            .Where(kv => kv.Value.ExpiresAt <= now)
            .Select(kv => kv.Key)
            .ToList();
        
        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }
    }
    
    private sealed class CacheEntry
    {
        public required string JsonValue { get; init; }
        public required DateTime ExpiresAt { get; init; }
    }
}
