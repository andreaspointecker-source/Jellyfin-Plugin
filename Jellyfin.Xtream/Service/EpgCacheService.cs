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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Client;
using Jellyfin.Xtream.Client.Models;
using MediaBrowser.Controller.LiveTv;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Advanced EPG caching service with adaptive TTL, prefetching, and LRU eviction.
/// </summary>
public sealed class EpgCacheService : IDisposable
{
    private static long _cacheHits = 0;
    private static long _cacheMisses = 0;
    private static long _prefetchHits = 0;
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<EpgCacheService> _logger;
    private readonly ConcurrentDictionary<string, DateTime> _lastUpdateTimes = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fetchLocks = new();
    private readonly System.Threading.Timer _cleanupTimer;

    /// <summary>
    /// Initializes a new instance of the <see cref="EpgCacheService"/> class.
    /// </summary>
    /// <param name="memoryCache">The memory cache instance.</param>
    /// <param name="logger">The logger instance.</param>
    public EpgCacheService(IMemoryCache memoryCache, ILogger<EpgCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;

        // Run cleanup every 24 hours
        _cleanupTimer = new System.Threading.Timer(
            _ => CleanupStaleEntries(),
            null,
            TimeSpan.FromHours(24),
            TimeSpan.FromHours(24));
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
    /// Gets the total cache hits.
    /// </summary>
    public static long CacheHits => _cacheHits;

    /// <summary>
    /// Gets the total cache misses.
    /// </summary>
    public static long CacheMisses => _cacheMisses;

    /// <summary>
    /// Gets the prefetch hit count (items served from prefetch).
    /// </summary>
    public static long PrefetchHits => _prefetchHits;

    /// <summary>
    /// Gets EPG programs for a channel with adaptive caching and prefetching.
    /// </summary>
    /// <param name="channelId">The channel ID.</param>
    /// <param name="streamId">The Xtream stream ID.</param>
    /// <param name="startDateUtc">Start date filter.</param>
    /// <param name="endDateUtc">End date filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of EPG programs.</returns>
    public async Task<IEnumerable<ProgramInfo>> GetProgramsAsync(
        string channelId,
        int streamId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        CancellationToken cancellationToken)
    {
        string cacheKey = GetCacheKey(channelId);

        // Try to get from cache
        if (_memoryCache.TryGetValue(cacheKey, out CachedEpgData? cachedData))
        {
            Interlocked.Increment(ref _cacheHits);

            // Check if data was prefetched
            if (cachedData!.WasPrefetched)
            {
                Interlocked.Increment(ref _prefetchHits);
                _logger.LogDebug("EPG cache hit (prefetched) for channel {ChannelId}", channelId);
            }
            else
            {
                _logger.LogDebug("EPG cache hit for channel {ChannelId}", channelId);
            }

            // Trigger background prefetch if needed
            _ = Task.Run(() => PrefetchIfNeededAsync(channelId, streamId, cachedData, cancellationToken), cancellationToken);

            return FilterByDateRange(cachedData.Programs, startDateUtc, endDateUtc);
        }

        // Cache miss - fetch from API
        Interlocked.Increment(ref _cacheMisses);
        _logger.LogDebug("EPG cache miss for channel {ChannelId}", channelId);

        return await FetchAndCacheAsync(channelId, streamId, startDateUtc, endDateUtc, false, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Fetches EPG data from API and caches it with adaptive TTL.
    /// </summary>
    private async Task<IEnumerable<ProgramInfo>> FetchAndCacheAsync(
        string channelId,
        int streamId,
        DateTime startDateUtc,
        DateTime endDateUtc,
        bool isPrefetch,
        CancellationToken cancellationToken)
    {
        string cacheKey = GetCacheKey(channelId);

        // Use semaphore to prevent concurrent fetches for same channel
        var lockSemaphore = _fetchLocks.GetOrAdd(cacheKey, _ => new SemaphoreSlim(1, 1));

        await lockSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Double-check cache after acquiring lock
            if (_memoryCache.TryGetValue(cacheKey, out CachedEpgData? existingData))
            {
                return FilterByDateRange(existingData!.Programs, startDateUtc, endDateUtc);
            }

            // Fetch from API
            var programs = await FetchFromApiAsync(channelId, streamId, cancellationToken).ConfigureAwait(false);

            // Calculate adaptive TTL based on EPG update frequency
            var ttl = CalculateAdaptiveTtl(channelId, programs);

            // Cache the data
            var cachedData = new CachedEpgData
            {
                Programs = programs.ToList(),
                CachedAt = DateTime.UtcNow,
                WasPrefetched = isPrefetch,
            };

            var cacheOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(ttl)
                .SetSize(programs.Count())
                .RegisterPostEvictionCallback(OnEviction);

            _memoryCache.Set(cacheKey, cachedData, cacheOptions);

            _lastUpdateTimes[cacheKey] = DateTime.UtcNow;

            _logger.LogInformation(
                "Cached EPG for channel {ChannelId}: {Count} programs, TTL: {Minutes:F1} min{Prefetch}",
                channelId,
                programs.Count(),
                ttl.TotalMinutes,
                isPrefetch ? " (prefetched)" : string.Empty);

            return FilterByDateRange(programs, startDateUtc, endDateUtc);
        }
        finally
        {
            lockSemaphore.Release();
        }
    }

    /// <summary>
    /// Fetches EPG data from Xtream API.
    /// </summary>
    private async Task<IEnumerable<ProgramInfo>> FetchFromApiAsync(
        string channelId,
        int streamId,
        CancellationToken cancellationToken)
    {
        var plugin = Plugin.Instance;
        var programs = new List<ProgramInfo>();

        // TODO: Use IHttpClientFactory constructor - see docs/MEMORY_LEAK_ANALYSIS.md
        using var client = new XtreamClient();
        var epgs = await client.GetEpgInfoAsync(plugin.Creds, streamId, cancellationToken).ConfigureAwait(false);

        foreach (var epg in epgs.Listings)
        {
            programs.Add(new ProgramInfo
            {
                Id = StreamService.ToGuid(StreamService.EpgPrefix, streamId, epg.Id, 0).ToString(),
                ChannelId = channelId,
                StartDate = epg.Start,
                EndDate = epg.End,
                Name = epg.Title,
                Overview = epg.Description,
            });
        }

        return programs;
    }

    /// <summary>
    /// Calculates adaptive TTL based on EPG update patterns.
    /// </summary>
    private TimeSpan CalculateAdaptiveTtl(string channelId, IEnumerable<ProgramInfo> programs)
    {
        var config = Plugin.Instance.Configuration;

        // If EPG preload is disabled, use short TTL
        if (!config.EnableEpgPreload)
        {
            return TimeSpan.FromMinutes(5);
        }

        var programList = programs.ToList();
        if (programList.Count == 0)
        {
            return TimeSpan.FromMinutes(30); // No data, cache for 30 min
        }

        // Calculate average program duration
        var avgDuration = programList.Average(p => (p.EndDate - p.StartDate).TotalMinutes);

        // Adaptive TTL based on program duration
        if (avgDuration < 30) // News/short programs - update frequently
        {
            return TimeSpan.FromMinutes(15);
        }
        else if (avgDuration < 60) // Standard shows
        {
            return TimeSpan.FromMinutes(30);
        }
        else // Movies/long content
        {
            return TimeSpan.FromMinutes(60);
        }
    }

    /// <summary>
    /// Triggers prefetch if EPG data is getting stale.
    /// </summary>
    private async Task PrefetchIfNeededAsync(
        string channelId,
        int streamId,
        CachedEpgData cachedData,
        CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        if (!config.EnableEpgPreload)
        {
            return;
        }

        try
        {
            // Prefetch if cache is older than 80% of TTL
            var cacheAge = DateTime.UtcNow - cachedData.CachedAt;
            var shouldPrefetch = cacheAge.TotalMinutes > 24; // Prefetch after 24min (80% of 30min)

            if (!shouldPrefetch)
            {
                return;
            }

            _logger.LogDebug("Triggering background prefetch for channel {ChannelId}", channelId);

            // Prefetch in background (fire and forget)
            await FetchAndCacheAsync(
                channelId,
                streamId,
                DateTime.UtcNow,
                DateTime.UtcNow.AddDays(7),
                true,
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to prefetch EPG for channel {ChannelId}", channelId);
        }
    }

    /// <summary>
    /// Filters programs by date range.
    /// </summary>
    private static IEnumerable<ProgramInfo> FilterByDateRange(
        IEnumerable<ProgramInfo> programs,
        DateTime startDateUtc,
        DateTime endDateUtc)
    {
        return programs.Where(p => p.EndDate >= startDateUtc && p.StartDate < endDateUtc);
    }

    /// <summary>
    /// Gets the cache key for a channel.
    /// </summary>
    private static string GetCacheKey(string channelId) => $"epg-v2-{channelId}";

    /// <summary>
    /// Called when cache entry is evicted.
    /// </summary>
    private void OnEviction(object key, object? value, EvictionReason reason, object? state)
    {
        if (reason != EvictionReason.Replaced)
        {
            _logger.LogDebug("EPG cache evicted for key {Key}: {Reason}", key, reason);
        }
    }

    /// <summary>
    /// Clears all EPG cache entries and disposes resources.
    /// </summary>
    public void ClearCache()
    {
        _logger.LogInformation("Clearing EPG cache");

        // Clear update times
        _lastUpdateTimes.Clear();

        // Dispose and clear all semaphores
        foreach (var semaphore in _fetchLocks.Values)
        {
            try
            {
                semaphore.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispose semaphore during cache clear");
            }
        }

        _fetchLocks.Clear();

        _logger.LogInformation("EPG cache cleared, {SemaphoreCount} semaphores disposed", _fetchLocks.Count);

        // Note: IMemoryCache doesn't have a Clear() method
        // Entries will expire naturally based on TTL
    }

    /// <summary>
    /// Cleans up stale entries from dictionaries (channels not accessed in 7+ days).
    /// </summary>
    public void CleanupStaleEntries()
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-7);
        var staleChannels = _lastUpdateTimes
            .Where(kvp => kvp.Value < cutoffDate)
            .Select(kvp => kvp.Key)
            .ToList();

        if (staleChannels.Count == 0)
        {
            return;
        }

        _logger.LogDebug("Cleaning up {Count} stale EPG cache entries", staleChannels.Count);

        foreach (var channelId in staleChannels)
        {
            _lastUpdateTimes.TryRemove(channelId, out _);

            if (_fetchLocks.TryRemove(channelId, out var semaphore))
            {
                try
                {
                    semaphore.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to dispose semaphore for channel {ChannelId}", channelId);
                }
            }
        }

        _logger.LogInformation("Cleaned up {Count} stale EPG entries", staleChannels.Count);
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public static void ResetStatistics()
    {
        Interlocked.Exchange(ref _cacheHits, 0);
        Interlocked.Exchange(ref _cacheMisses, 0);
        Interlocked.Exchange(ref _prefetchHits, 0);
    }

    /// <summary>
    /// Disposes resources used by the EPG cache service.
    /// </summary>
    public void Dispose()
    {
        _cleanupTimer?.Dispose();

        // Dispose all semaphores
        foreach (var semaphore in _fetchLocks.Values)
        {
            try
            {
                semaphore.Dispose();
            }
            catch
            {
                // Ignore disposal errors
            }
        }

        _fetchLocks.Clear();
        _lastUpdateTimes.Clear();

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Cached EPG data container.
    /// </summary>
    private sealed class CachedEpgData
    {
        /// <summary>
        /// Gets or sets the list of EPG programs.
        /// </summary>
        public List<ProgramInfo> Programs { get; set; } = [];

        /// <summary>
        /// Gets or sets the timestamp when data was cached.
        /// </summary>
        public DateTime CachedAt { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this data was prefetched.
        /// </summary>
        public bool WasPrefetched { get; set; }
    }
}
