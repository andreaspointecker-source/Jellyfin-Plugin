// Copyright (C) 2025  CandyTv Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Issues short-lived stream tokens and enforces a single provider connection.
/// </summary>
public sealed class StreamTokenService : IDisposable
{
    private const int TokenBytes = 16;

    private static StreamTokenService? _instance;

    private readonly ConcurrentDictionary<string, StreamToken> _tokens = new();
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IServerApplicationHost _appHost;
    private readonly ILogger<StreamTokenService> _logger;
    private readonly SemaphoreSlim _providerSemaphore = new(1, 1);
    private readonly TimeSpan _tokenLifetime = TimeSpan.FromMinutes(5);
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="StreamTokenService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="appHost">The Jellyfin application host.</param>
    /// <param name="logger">The logger.</param>
    public StreamTokenService(IHttpClientFactory httpClientFactory, IServerApplicationHost appHost, ILogger<StreamTokenService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _appHost = appHost;
        _logger = logger;

        StreamTokenService? previous = Interlocked.CompareExchange(ref _instance, this, null);
        if (previous != null && !ReferenceEquals(previous, this))
        {
            throw new InvalidOperationException("StreamTokenService already initialised.");
        }
    }

    /// <summary>
    /// Gets the singleton instance.
    /// </summary>
    public static StreamTokenService Instance => _instance ?? throw new InvalidOperationException("StreamTokenService not initialised.");

    /// <summary>
    /// Tries to get the singleton instance.
    /// </summary>
    /// <param name="service">The resolved service.</param>
    /// <returns>True if the instance is available.</returns>
    public static bool TryGetInstance([NotNullWhen(true)] out StreamTokenService? service)
    {
        service = Volatile.Read(ref _instance);
        return service != null;
    }

    /// <summary>
    /// Issues a short-lived proxy URL for the given provider stream.
    /// </summary>
    /// <param name="type">The stream type.</param>
    /// <param name="streamId">The provider stream identifier.</param>
    /// <param name="providerUrl">The provider URL with credentials.</param>
    /// <param name="extension">The expected container extension.</param>
    /// <returns>An absolute proxy URL that can be used by Jellyfin clients.</returns>
    public string CreateProxyUrl(StreamType type, int streamId, string providerUrl, string? extension)
    {
        CleanupExpiredTokens();

        string token = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes));
        DateTimeOffset expiresAt = DateTimeOffset.UtcNow.Add(_tokenLifetime);
        string streamKey = CreateStreamKey(type, streamId);

        StreamToken entry = new(streamKey, providerUrl, extension, expiresAt);
        _tokens[token] = entry;

        string smartApiUrl = _appHost.GetSmartApiUrl(IPAddress.Any);
        if (!smartApiUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            smartApiUrl = "http://" + smartApiUrl;
        }

        return $"{smartApiUrl.TrimEnd('/')}/Xtream/Stream/{token}";
    }

    /// <summary>
    /// Opens a provider stream for the supplied token.
    /// </summary>
    /// <param name="token">The token supplied in the proxy URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A <see cref="StreamAccess"/> instance when successful; otherwise null.</returns>
    public async Task<StreamAccess?> OpenStreamAsync(string token, CancellationToken cancellationToken)
    {
        if (!_tokens.TryRemove(token, out StreamToken? entry) || entry is null)
        {
            _logger.LogWarning("Rejected unknown stream token {Token}", token);
            return null;
        }

        if (entry.ExpiresAt < DateTimeOffset.UtcNow)
        {
            _logger.LogWarning("Rejected expired stream token {Token}", token);
            return null;
        }

        StreamLease lease = await AcquireLeaseAsync(entry.StreamKey, cancellationToken).ConfigureAwait(false);
        HttpResponseMessage? response = null;

        try
        {
            HttpClient client = _httpClientFactory.CreateClient(NamedClient.Default);
            response = await client.GetAsync(entry.ProviderUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            Stream stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            string contentType = response.Content.Headers.ContentType?.MediaType ?? entry.GetContentType();
            long? contentLength = response.Content.Headers.ContentLength;

            _logger.LogDebug("Proxy stream {Token} opened for {Key}", token, entry.StreamKey);
            return new StreamAccess(this, lease, response, stream, contentType, contentLength);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open provider stream for token {Token}", token);
            response?.Dispose();
            await lease.DisposeAsync().ConfigureAwait(false);
            return null;
        }
    }

    /// <summary>
    /// Acquires the single provider connection lease for direct restream usage.
    /// </summary>
    /// <param name="streamKey">A descriptive key for the stream.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="StreamLease"/> that must be disposed to release the slot.</returns>
    public Task<StreamLease> AcquireLeaseAsync(string streamKey, CancellationToken cancellationToken)
    {
        return AcquireLeaseInternalAsync(streamKey, cancellationToken);
    }

    private async Task<StreamLease> AcquireLeaseInternalAsync(string streamKey, CancellationToken cancellationToken)
    {
        await _providerSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Acquired provider slot for {Key}", streamKey);
        return new StreamLease(this, streamKey);
    }

    private void ReleaseLease(StreamLease lease)
    {
        _providerSemaphore.Release();
        _logger.LogDebug("Released provider slot for {Key}", lease.StreamKey);
    }

    private void CleanupExpiredTokens()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        foreach (var kvp in _tokens)
        {
            if (kvp.Value.ExpiresAt < now)
            {
                _tokens.TryRemove(kvp.Key, out _);
            }
        }
    }

    private static string CreateStreamKey(StreamType type, int streamId) =>
        string.Create(CultureInfo.InvariantCulture, $"{type}:{streamId}");

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _providerSemaphore.Dispose();
        Interlocked.CompareExchange(ref _instance, null, this);
    }

    private sealed record StreamToken(string StreamKey, string ProviderUrl, string? Extension, DateTimeOffset ExpiresAt)
    {
        public string GetContentType()
        {
            return Extension?.ToLowerInvariant() switch
            {
                "ts" => "video/mp2t",
                "m3u8" => "application/vnd.apple.mpegurl",
                "mp4" => "video/mp4",
                "mkv" => "video/x-matroska",
                "mp3" => "audio/mpeg",
                "aac" => "audio/aac",
                _ => "application/octet-stream",
            };
        }
    }

    /// <summary>
    /// Represents an acquired provider lease.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:DoNotNestTypes", Justification = "Lease type scoped to StreamTokenService.")]
    public sealed class StreamLease : IAsyncDisposable
    {
        private readonly StreamTokenService _owner;
        private bool _disposed;

        internal StreamLease(StreamTokenService owner, string streamKey)
        {
            _owner = owner;
            StreamKey = streamKey;
        }

        /// <summary>
        /// Gets the descriptive stream key.
        /// </summary>
        public string StreamKey { get; }

        /// <inheritdoc />
        public ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return ValueTask.CompletedTask;
            }

            _disposed = true;
            _owner.ReleaseLease(this);
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Represents an open proxied stream.
    /// </summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:DoNotNestTypes", Justification = "Access type scoped to StreamTokenService.")]
    public sealed class StreamAccess : IAsyncDisposable
    {
        [SuppressMessage("Usage", "CA2213:DisposableFieldsShouldBeDisposed", Justification = "Owner lifetime managed by dependency injection.")]
        private readonly StreamTokenService _owner;
        private readonly StreamLease _lease;
        private readonly HttpResponseMessage _response;
        private readonly Stream _stream;
        private bool _disposed;

        internal StreamAccess(StreamTokenService owner, StreamLease lease, HttpResponseMessage response, Stream stream, string contentType, long? contentLength)
        {
            _owner = owner;
            _lease = lease;
            _response = response;
            _stream = stream;
            ContentType = contentType;
            ContentLength = contentLength;
        }

        /// <summary>
        /// Gets the provider content stream.
        /// </summary>
        public Stream Stream => _stream;

        /// <summary>
        /// Gets the response content type.
        /// </summary>
        public string ContentType { get; }

        /// <summary>
        /// Gets the response content length when supplied by the provider.
        /// </summary>
        public long? ContentLength { get; }

        /// <inheritdoc />
        public async ValueTask DisposeAsync()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stream.Dispose();
            _response.Dispose();
            await _lease.DisposeAsync().ConfigureAwait(false);
        }
    }
}
