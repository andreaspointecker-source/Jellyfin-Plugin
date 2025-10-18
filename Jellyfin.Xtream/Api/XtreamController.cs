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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Api.Models;
using Jellyfin.Xtream.Client;
using Jellyfin.Xtream.Client.Models;
using Jellyfin.Xtream.Configuration;
using Jellyfin.Xtream.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Api;

/// <summary>
/// The Jellyfin Xtream configuration API.
/// </summary>
[ApiController]
[Route("[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class XtreamController : ControllerBase
{
    private static CategoryResponse CreateCategoryResponse(Category category) =>
        new()
        {
            Id = category.CategoryId,
            Name = category.CategoryName,
        };

    private static ItemResponse CreateItemResponse(StreamInfo stream) =>
        new()
        {
            Id = stream.StreamId,
            Name = stream.Name,
            HasCatchup = stream.TvArchive,
            CatchupDuration = stream.TvArchiveDuration,
        };

    private static ItemResponse CreateItemResponse(Series series) =>
        new()
        {
            Id = series.SeriesId,
            Name = series.Name,
            HasCatchup = false,
            CatchupDuration = 0,
        };

    private static ChannelResponse CreateChannelResponse(StreamInfo stream) =>
        new()
        {
            Id = stream.StreamId,
            LogoUrl = stream.StreamIcon,
            Name = stream.Name,
            Number = stream.Num,
        };

    /// <summary>
    /// Get all Live TV categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the categories.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("LiveCategories")]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetLiveCategories(CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<Category> categories = await client.GetLiveCategoryAsync(plugin.Creds, cancellationToken).ConfigureAwait(false);
        return Ok(categories.Select(CreateCategoryResponse));
    }

    /// <summary>
    /// Get all Live TV streams for the given category.
    /// </summary>
    /// <param name="categoryId">The category for which to fetch the streams.</param>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("LiveCategories/{categoryId}")]
    public async Task<ActionResult<IEnumerable<StreamInfo>>> GetLiveStreams(int categoryId, CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<StreamInfo> streams = await client.GetLiveStreamsByCategoryAsync(
          plugin.Creds,
          categoryId,
          cancellationToken).ConfigureAwait(false);
        return Ok(streams.Select(CreateItemResponse));
    }

    /// <summary>
    /// Get all VOD categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the categories.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("VodCategories")]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetVodCategories(CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<Category> categories = await client.GetVodCategoryAsync(plugin.Creds, cancellationToken).ConfigureAwait(false);
        return Ok(categories.Select(CreateCategoryResponse));
    }

    /// <summary>
    /// Get all VOD streams for the given category.
    /// </summary>
    /// <param name="categoryId">The category for which to fetch the streams.</param>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("VodCategories/{categoryId}")]
    public async Task<ActionResult<IEnumerable<StreamInfo>>> GetVodStreams(int categoryId, CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<StreamInfo> streams = await client.GetVodStreamsByCategoryAsync(
          plugin.Creds,
          categoryId,
          cancellationToken).ConfigureAwait(false);
        return Ok(streams.Select(CreateItemResponse));
    }

    /// <summary>
    /// Get all Series categories.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the categories.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("SeriesCategories")]
    public async Task<ActionResult<IEnumerable<CategoryResponse>>> GetSeriesCategories(CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<Category> categories = await client.GetSeriesCategoryAsync(plugin.Creds, cancellationToken).ConfigureAwait(false);
        return Ok(categories.Select(CreateCategoryResponse));
    }

    /// <summary>
    /// Get all Series streams for the given category.
    /// </summary>
    /// <param name="categoryId">The category for which to fetch the streams.</param>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("SeriesCategories/{categoryId}")]
    public async Task<ActionResult<IEnumerable<StreamInfo>>> GetSeriesStreams(int categoryId, CancellationToken cancellationToken)
    {
        Plugin plugin = Plugin.Instance;
        using XtreamClient client = new XtreamClient();
        List<Series> series = await client.GetSeriesByCategoryAsync(
          plugin.Creds,
          categoryId,
          cancellationToken).ConfigureAwait(false);
        return Ok(series.Select(CreateItemResponse));
    }

    /// <summary>
    /// Get all configured TV channels.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token for cancelling requests.</param>
    /// <returns>An enumerable containing the streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("LiveTv")]
    public async Task<ActionResult<IEnumerable<StreamInfo>>> GetLiveTvChannels(CancellationToken cancellationToken)
    {
        IEnumerable<StreamInfo> streams = await Plugin.Instance.StreamService.GetLiveStreams(cancellationToken).ConfigureAwait(false);
        var channels = streams.Select(CreateChannelResponse).ToList();
        return Ok(channels);
    }

    /// <summary>
    /// Parse TXT file content into channel list entries.
    /// </summary>
    /// <param name="request">The parse request containing TXT content.</param>
    /// <returns>List of channel names.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpPost("ChannelLists/Parse")]
    public ActionResult<IReadOnlyCollection<string>> ParseChannelList([FromBody] ParseChannelListRequest request)
    {
        var service = new ChannelListService();
        var entries = service.ParseTxtContent(request.Content);
        return Ok(entries);
    }

    /// <summary>
    /// Find fuzzy matches for a channel name.
    /// </summary>
    /// <param name="request">The match request.</param>
    /// <returns>List of matching streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpPost("ChannelLists/Match")]
    public async Task<IActionResult> MatchChannel([FromBody] MatchChannelRequest request)
    {
        try
        {
            var service = new ChannelListService();
            IEnumerable<StreamInfo> allStreams = await Plugin.Instance.StreamService.GetLiveStreams(CancellationToken.None).ConfigureAwait(false);

            var matches = service.GetTopMatches(request.ChannelName, allStreams, 10);

            var response = matches.Select(m => new ChannelMatchResponse
            {
                StreamId = m.Stream.StreamId,
                StreamName = m.Stream.Name ?? string.Empty,
                StreamIcon = m.Stream.StreamIcon,
                Score = m.Score,
                IsExact = m.IsExact,
            }).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Get all available live streams for manual selection.
    /// </summary>
    /// <returns>All live streams.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("ChannelLists/AllStreams")]
    public async Task<IActionResult> GetAllStreams()
    {
        try
        {
            IEnumerable<StreamInfo> streams = await Plugin.Instance.StreamService.GetLiveStreams(CancellationToken.None).ConfigureAwait(false);

            var response = streams.Select(s => new ChannelMatchResponse
            {
                StreamId = s.StreamId,
                StreamName = s.Name ?? string.Empty,
                StreamIcon = s.StreamIcon,
                Score = 0,
                IsExact = false,
            }).OrderBy(s => s.StreamName).ToList();

            return Ok(response);
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message, stackTrace = ex.StackTrace, type = ex.GetType().Name });
        }
    }

    /// <summary>
    /// Get optimization statistics (connection status, cache hit rate, etc.).
    /// </summary>
    /// <returns>Current optimization statistics.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("OptimizationStats")]
    public ActionResult<object> GetOptimizationStats()
    {
        return Ok(new
        {
            isBusy = ConnectionManager.IsBusy,
            queuedRequests = ConnectionManager.QueuedRequests,
            totalRequests = ConnectionManager.TotalRequests,
            cacheHitRate = CacheService.CacheHitRate,
            cacheHits = CacheService.CacheHits,
            cacheMisses = CacheService.CacheMisses,
            thumbnailCacheHitRate = ThumbnailCacheService.CacheHitRate,
            thumbnailCachedImages = ThumbnailCacheService.CachedImages,
            thumbnailCacheRequests = ThumbnailCacheService.CacheRequests,
            epgCacheHitRate = EpgCacheService.CacheHitRate,
            epgCacheHits = EpgCacheService.CacheHits,
            epgCacheMisses = EpgCacheService.CacheMisses,
            epgPrefetchHits = EpgCacheService.PrefetchHits,
        });
    }

    /// <summary>
    /// Reset channel mappings and force reload from provider.
    /// </summary>
    /// <returns>Success message.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpPost("ResetChannelOrder")]
    public ActionResult<object> ResetChannelOrder()
    {
        try
        {
            var config = Plugin.Instance.Configuration;

            // Clear channel mappings
            if (config.ChannelMappings != null)
            {
                int count = config.ChannelMappings.Count;
                config.ChannelMappings.Clear();

                // Save configuration
                Plugin.Instance.UpdateConfiguration(config);

                return Ok(new { success = true, message = $"Cleared {count} channel mappings. Channels will reload from provider on next refresh." });
            }

            return Ok(new { success = true, message = "No channel mappings to clear." });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, error = ex.Message });
        }
    }

    /// <summary>
    /// Get user and server information from the Xtream provider.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>User and server information.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("UserInfo")]
    public async Task<IActionResult> GetUserInfo(CancellationToken cancellationToken)
    {
        try
        {
            Plugin plugin = Plugin.Instance;
            using XtreamClient client = new XtreamClient();
            var playerApi = await client.GetUserAndServerInfoAsync(plugin.Creds, cancellationToken).ConfigureAwait(false);

            return Ok(new
            {
                username = playerApi.UserInfo.Username,
                status = playerApi.UserInfo.Status,
                expDate = playerApi.UserInfo.ExpDate,
                isTrial = playerApi.UserInfo.IsTrial,
                activeCons = playerApi.UserInfo.ActiveCons,
                maxConnections = playerApi.UserInfo.MaxConnections,
                createdAt = playerApi.UserInfo.CreatedAt,
                serverUrl = playerApi.ServerSnfo.Url,
                serverTimezone = playerApi.ServerSnfo.Timezone,
            });
        }
        catch (Exception ex)
        {
            return Ok(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Proxy provider content through a single authenticated session.
    /// </summary>
    /// <param name="token">The short-lived stream token.</param>
    /// <param name="tokenService">The token service.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The proxied stream.</returns>
    [AllowAnonymous]
    [HttpGet("Stream/{token}")]
    public async Task<IActionResult> StreamByToken(
        string token,
        [FromServices] StreamTokenService tokenService,
        CancellationToken cancellationToken)
    {
        StreamTokenService.StreamAccess? access = await tokenService.OpenStreamAsync(token, cancellationToken).ConfigureAwait(false);
        if (access == null)
        {
            return NotFound();
        }

        return new StreamProxyResult(access);
    }

    /// <summary>
    /// Get thumbnail cache statistics (file count and total size).
    /// </summary>
    /// <returns>Cache statistics.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpGet("ThumbnailCacheStats")]
    public ActionResult<object> GetThumbnailCacheStats()
    {
        try
        {
            return Ok(new
            {
                fileCount = ThumbnailCacheService.TotalCachedFiles,
                totalSizeMB = ThumbnailCacheService.TotalCacheSize / 1024.0 / 1024.0,
                cacheHitRate = ThumbnailCacheService.CacheHitRate,
                cacheRequests = ThumbnailCacheService.CacheRequests,
                cachedImages = ThumbnailCacheService.CachedImages,
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                fileCount = 0,
                totalSizeMB = 0.0,
                error = ex.Message,
            });
        }
    }

    /// <summary>
    /// Clear all cached thumbnails.
    /// </summary>
    /// <param name="thumbnailCacheService">The thumbnail cache service.</param>
    /// <returns>Success message.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpPost("ClearThumbnailCache")]
    public ActionResult<object> ClearThumbnailCache([FromServices] ThumbnailCacheService thumbnailCacheService)
    {
        try
        {
            var beforeFiles = ThumbnailCacheService.TotalCachedFiles;
            var beforeSize = ThumbnailCacheService.TotalCacheSize;

            thumbnailCacheService.ClearCache();

            return Ok(new
            {
                success = true,
                message = "Thumbnail cache cleared successfully",
                deletedFiles = beforeFiles,
                freedSpaceMB = beforeSize / 1024.0 / 1024.0,
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
            });
        }
    }

    /// <summary>
    /// Trigger manual thumbnail cache cleanup (removes old files based on retention policy).
    /// </summary>
    /// <param name="thumbnailCacheService">The thumbnail cache service.</param>
    /// <returns>Success message.</returns>
    [Authorize(Policy = "RequiresElevation")]
    [HttpPost("TriggerThumbnailCleanup")]
    public ActionResult<object> TriggerThumbnailCleanup([FromServices] ThumbnailCacheService thumbnailCacheService)
    {
        try
        {
            var beforeFiles = ThumbnailCacheService.TotalCachedFiles;
            var beforeSize = ThumbnailCacheService.TotalCacheSize;

            thumbnailCacheService.TriggerCleanup();

            var deletedFiles = beforeFiles - ThumbnailCacheService.TotalCachedFiles;
            var freedSize = beforeSize - ThumbnailCacheService.TotalCacheSize;

            return Ok(new
            {
                success = true,
                message = "Thumbnail cleanup completed",
                deletedFiles = deletedFiles,
                freedSpaceMB = freedSize / 1024.0 / 1024.0,
                remainingFiles = ThumbnailCacheService.TotalCachedFiles,
                remainingSizeMB = ThumbnailCacheService.TotalCacheSize / 1024.0 / 1024.0,
            });
        }
        catch (Exception ex)
        {
            return Ok(new
            {
                success = false,
                error = ex.Message,
            });
        }
    }

    private static string GetThumbnailCacheDirectory()
    {
        var dataPath = Plugin.Instance.DataFolderPath;
        return Path.Combine(dataPath, "thumbnails");
    }
}
