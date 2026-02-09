using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace ErpSystem.BuildingBlocks.Caching;

/// <summary>
/// Distributed Cache Extensions - Provides typed get/set operations with optional compression.
/// </summary>
public static class DistributedCacheExtensions
{
    public static async Task<T?> GetAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken cancellationToken = default) where T : class
    {
        string? data = await cache.GetStringAsync(key, cancellationToken);
        return data is null ? null : JsonSerializer.Deserialize<T>(data);
    }

    public static async Task SetAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        TimeSpan? absoluteExpiration = null,
        TimeSpan? slidingExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = absoluteExpiration,
            SlidingExpiration = slidingExpiration
        };

        string serialized = JsonSerializer.Serialize(value);
        await cache.SetStringAsync(key, serialized, options, cancellationToken);
    }

    public static async Task<T> GetOrSetAsync<T>(
        this IDistributedCache cache,
        string key,
        Func<Task<T>> factory,
        TimeSpan? absoluteExpiration = null,
        CancellationToken cancellationToken = default) where T : class
    {
        T? cached = await cache.GetAsync<T>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        T value = await factory();
        await cache.SetAsync(key, value, absoluteExpiration, null, cancellationToken);
        return value;
    }
}

/// <summary>
/// Cache Key Builder - Provides consistent cache key generation.
/// </summary>
public static class CacheKeyBuilder
{
    public static string Build(string category, params object[] parts)
    {
        List<string> keyParts = [category];
        keyParts.AddRange(parts.Select(p => p?.ToString() ?? "null"));
        return string.Join(":", keyParts);
    }

    public static string ForEntity<T>(object id) => Build(typeof(T).Name, id);
    public static string ForList<T>(string? filter = null) => Build($"{typeof(T).Name}:List", filter ?? "all");
}
