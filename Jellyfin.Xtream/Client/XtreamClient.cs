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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Client.Models;
using Jellyfin.Xtream.Service;
using Newtonsoft.Json;

#pragma warning disable CS1591
namespace Jellyfin.Xtream.Client;

/// <summary>
/// The Xtream API client implementation.
/// </summary>
public class XtreamClient : IDisposable
{
    // TODO: MEMORY LEAK - See docs/MEMORY_LEAK_ANALYSIS.md #1
    // This shared client is a workaround. Proper fix requires refactoring all callers to use IHttpClientFactory.
    private static readonly Lazy<HttpClient> _sharedClient = new(CreateDefaultClient, LazyThreadSafetyMode.ExecutionAndPublication);

    private readonly HttpClient _client;
    private readonly bool _disposeClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="XtreamClient"/> class.
    /// WARNING: Uses shared HttpClient to avoid socket exhaustion. Consider using IHttpClientFactory constructor instead.
    /// </summary>
    public XtreamClient()
    {
        _client = _sharedClient.Value;
        _disposeClient = false; // Shared, do not dispose
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XtreamClient"/> class using IHttpClientFactory (recommended).
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    public XtreamClient(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient("XtreamClient");
        _disposeClient = false; // Factory manages lifecycle
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="XtreamClient"/> class with a custom HttpClient.
    /// </summary>
    /// <param name="client">The HTTP client used.</param>
    public XtreamClient(HttpClient client)
    {
        _client = client;
        _disposeClient = true; // We own this client, must dispose
    }

    private static HttpClient CreateDefaultClient()
    {
        HttpClient client = new HttpClient();

        ProductHeaderValue header = new ProductHeaderValue("Jellyfin.Xtream", Assembly.GetExecutingAssembly().GetName().Version?.ToString());
        ProductInfoHeaderValue userAgent = new ProductInfoHeaderValue(header);
        client.DefaultRequestHeaders.UserAgent.Add(userAgent);

        return client;
    }

    private async Task<T> QueryApi<T>(ConnectionInfo connectionInfo, string urlPath, CancellationToken cancellationToken)
    {
        // Check if connection queue is enabled in configuration
        var config = Plugin.Instance?.Configuration;
        bool useConnectionQueue = config?.EnableConnectionQueue ?? false;

        if (useConnectionQueue)
        {
            // Use ConnectionManager to enforce single-connection constraint
            return await ConnectionManager.ExecuteAsync(
                async () =>
                {
                    Uri uri = new Uri(connectionInfo.BaseUrl + urlPath);
                    string jsonContent = await _client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<T>(jsonContent)!;
                },
                null,
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            // Direct call without queueing (original behavior)
            Uri uri = new Uri(connectionInfo.BaseUrl + urlPath);
            string jsonContent = await _client.GetStringAsync(uri, cancellationToken).ConfigureAwait(false);
            return JsonConvert.DeserializeObject<T>(jsonContent)!;
        }
    }

    public Task<PlayerApi> GetUserAndServerInfoAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken) =>
        QueryApi<PlayerApi>(
          connectionInfo,
          $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}",
          cancellationToken);

    public Task<List<Series>> GetSeriesByCategoryAsync(ConnectionInfo connectionInfo, int categoryId, CancellationToken cancellationToken) =>
         QueryApi<List<Series>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_series&category_id={categoryId}",
           cancellationToken);

    public Task<SeriesStreamInfo> GetSeriesStreamsBySeriesAsync(ConnectionInfo connectionInfo, int seriesId, CancellationToken cancellationToken) =>
         QueryApi<SeriesStreamInfo>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_series_info&series_id={seriesId}",
           cancellationToken);

    public Task<List<StreamInfo>> GetVodStreamsByCategoryAsync(ConnectionInfo connectionInfo, int categoryId, CancellationToken cancellationToken) =>
         QueryApi<List<StreamInfo>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_vod_streams&category_id={categoryId}",
           cancellationToken);

    public Task<VodStreamInfo> GetVodInfoAsync(ConnectionInfo connectionInfo, int streamId, CancellationToken cancellationToken) =>
         QueryApi<VodStreamInfo>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_vod_info&vod_id={streamId}",
           cancellationToken);

    public Task<List<StreamInfo>> GetLiveStreamsAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken) =>
         QueryApi<List<StreamInfo>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_live_streams",
           cancellationToken);

    public Task<List<StreamInfo>> GetLiveStreamsByCategoryAsync(ConnectionInfo connectionInfo, int categoryId, CancellationToken cancellationToken) =>
         QueryApi<List<StreamInfo>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_live_streams&category_id={categoryId}",
           cancellationToken);

    public Task<List<Category>> GetSeriesCategoryAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken) =>
         QueryApi<List<Category>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_series_categories",
           cancellationToken);

    public Task<List<Category>> GetVodCategoryAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken) =>
         QueryApi<List<Category>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_vod_categories",
           cancellationToken);

    public Task<List<Category>> GetLiveCategoryAsync(ConnectionInfo connectionInfo, CancellationToken cancellationToken) =>
         QueryApi<List<Category>>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_live_categories",
           cancellationToken);

    public Task<EpgListings> GetEpgInfoAsync(ConnectionInfo connectionInfo, int streamId, CancellationToken cancellationToken) =>
         QueryApi<EpgListings>(
           connectionInfo,
           $"/player_api.php?username={connectionInfo.UserName}&password={connectionInfo.Password}&action=get_simple_data_table&stream_id={streamId}",
           cancellationToken);

    /// <summary>
    /// Dispose the HTTP client if owned by this instance.
    /// </summary>
    /// <param name="disposing">True if disposing managed resources.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (disposing && _disposeClient)
        {
            _client?.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
