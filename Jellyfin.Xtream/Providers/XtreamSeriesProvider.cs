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
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Client;
using Jellyfin.Xtream.Client.Models;
using Jellyfin.Xtream.Service;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Providers;

/// <summary>
/// The Xtream Codes Series metadata provider with TMDB integration.
/// </summary>
/// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
/// <param name="providerManager">Instance of the <see cref="IProviderManager"/> interface.</param>
public class XtreamSeriesProvider(ILogger<SeriesChannel> logger, IProviderManager providerManager) : ICustomMetadataProvider<MediaBrowser.Controller.Entities.TV.Series>, IPreRefreshProvider
{
    /// <summary>
    /// The name of the provider.
    /// </summary>
    public const string ProviderName = "XtreamSeriesProvider";

    /// <inheritdoc/>
    public string Name => ProviderName;

    /// <inheritdoc/>
    public async Task<ItemUpdateType> FetchAsync(MediaBrowser.Controller.Entities.TV.Series item, MetadataRefreshOptions options, CancellationToken cancellationToken)
    {
        if (!Plugin.Instance.Configuration.EnableTmdbForSeries)
        {
            return ItemUpdateType.None;
        }

        string? idStr = item.GetProviderId(ProviderName);
        if (idStr is null)
        {
            return ItemUpdateType.None;
        }

        logger.LogDebug("Getting metadata for series {Id}", idStr);
        int id = int.Parse(idStr, CultureInfo.InvariantCulture);

        try
        {
            using XtreamClient client = new();
            SeriesStreamInfo seriesInfo = await client.GetSeriesStreamsBySeriesAsync(Plugin.Instance.Creds, id, cancellationToken).ConfigureAwait(false);
            Client.Models.SeriesInfo? info = seriesInfo.Info;

            if (info is null)
            {
                return ItemUpdateType.None;
            }

            // Update basic metadata from Xtream
            item.Overview ??= info.Plot;
            item.DateModified = info.LastModified;

            if (!string.IsNullOrEmpty(info.Genre))
            {
                item.Genres ??= info.Genre.Split(',').Select(g => g.Trim()).ToArray();
            }

            if (info.Rating5Based > 0)
            {
                item.CommunityRating = (float)info.Rating5Based;
            }

            // Try to fetch TMDB metadata
            if (!item.HasProviderId(MetadataProvider.Tmdb))
            {
                // Try to search for TMDB ID
                RemoteSearchQuery<MediaBrowser.Controller.Providers.SeriesInfo> query = new()
                {
                    SearchInfo = new()
                    {
                        Name = StreamService.ParseName(info.Name ?? string.Empty).Title,
                        Year = item.PremiereDate?.Year,
                    },
                    SearchProviderName = "TheMovieDb",
                };

                IEnumerable<RemoteSearchResult> results = await providerManager.GetRemoteSearchResults<MediaBrowser.Controller.Entities.TV.Series, MediaBrowser.Controller.Providers.SeriesInfo>(query, cancellationToken).ConfigureAwait(false);
                if (results.Any())
                {
                    RemoteSearchResult tmdbSeries = results.First();
                    if (tmdbSeries.HasProviderId(MetadataProvider.Tmdb))
                    {
                        string? queryId = tmdbSeries.GetProviderId(MetadataProvider.Tmdb);
                        if (queryId is not null)
                        {
                            options.ReplaceAllMetadata = true;
                            item.SetProviderId(MetadataProvider.Tmdb, queryId);
                        }
                    }
                }
            }

            return ItemUpdateType.MetadataImport;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch metadata for series {Id}", idStr);
            return ItemUpdateType.None;
        }
    }
}
