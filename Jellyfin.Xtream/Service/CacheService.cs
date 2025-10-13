// Copyright (C) 2022  Kevin Jilissen

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Cache time-to-live presets.
/// </summary>
public enum CacheDuration
{
    /// <summary>EPG data - 10 minutes.</summary>
    Epg,

    /// <summary>Stream URLs - 30 minutes.</summary>
    StreamUrl,

    /// <summary>Categories - 12 hours.</summary>
    Categories,

    /// <summary>VOD/Series metadata - 24 hours.</summary>
    Metadata,

    /// <summary>Channel lists - 6 hours.</summary>
    ChannelLists,
}

/// <summary>
/// Enhanced caching service with extended TTLs for Xtream data.
/// </summary>
public class CacheService
{
    private static int _cacheHits = 0;
    private static int _cacheMisses = 0;
    private readonly IMemoryCache _cache;

    /// <summary>
    /// Initializes a new instance of the <see cref="CacheService"/> class.
    /// </summary>
    /// <param name="cache">The memory cache instance.</param>
    public CacheService(IMemoryCache cache)
    {
        _cache = cache;
    }

    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public static double CacheHitRate
    {
        get
        {
            var total = _cacheHits + _cacheMisses;
            return total == 0 ? 0 : (_cacheHits * 100.0) / total;
        }
    }

    /// <summary>
    /// Gets the total number of cache hits.
    /// </summary>
    public static int CacheHits => _cacheHits;

    /// <summary>
    /// Gets the total number of cache misses.
    /// </summary>
    public static int CacheMisses => _cacheMisses;

    /// <summary>
    /// Gets or sets a cached value with automatic TTL based on data type.
    /// </summary>
    /// <typeparam name="T">The type of data to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">Factory function to create the value if not cached.</param>
    /// <param name="duration">The cache duration type.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached or newly created value.</returns>
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<Task<T>> factory,
        CacheDuration duration,
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
        {
            Interlocked.Increment(ref _cacheHits);
            return cachedValue;
        }

        Interlocked.Increment(ref _cacheMisses);

        var value = await factory().ConfigureAwait(false);

        var ttl = GetTtl(duration);
        _cache.Set(key, value, DateTimeOffset.UtcNow.Add(ttl));

        return value;
    }

    /// <summary>
    /// Clears the entire cache.
    /// </summary>
    public void Clear()
    {
        if (_cache is MemoryCache memCache)
        {
            memCache.Compact(1.0);
        }
    }

    /// <summary>
    /// Removes a specific cache entry.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    public void Remove(string key)
    {
        _cache.Remove(key);
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public static void ResetStatistics()
    {
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
    }

    private static TimeSpan GetTtl(CacheDuration duration)
    {
        return duration switch
        {
            CacheDuration.Epg => TimeSpan.FromMinutes(10),
            CacheDuration.StreamUrl => TimeSpan.FromMinutes(30),
            CacheDuration.Categories => TimeSpan.FromHours(12),
            CacheDuration.Metadata => TimeSpan.FromHours(24),
            CacheDuration.ChannelLists => TimeSpan.FromHours(6),
            _ => TimeSpan.FromMinutes(10),
        };
    }
}
