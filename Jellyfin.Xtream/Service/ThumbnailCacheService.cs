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
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Service for caching thumbnail images from remote URLs.
/// </summary>
public sealed class ThumbnailCacheService : IDisposable
{
    private static int _cachedImages = 0;
    private static int _cacheRequests = 0;
    private static long _totalCacheSize = 0;
    private static int _totalCachedFiles = 0;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ThumbnailCacheService> _logger;
    private readonly ConcurrentDictionary<string, Task<string?>> _inProgressDownloads = new();
    private readonly object _initLock = new();
    private string? _cacheDirectory;
    private bool _cleanupTaskStarted = false;
    private CancellationTokenSource? _cleanupCancellation;
    private Task? _cleanupTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThumbnailCacheService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">The HTTP client factory.</param>
    /// <param name="logger">The logger.</param>
    public ThumbnailCacheService(IHttpClientFactory httpClientFactory, ILogger<ThumbnailCacheService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets the number of cached images served.
    /// </summary>
    public static int CachedImages => _cachedImages;

    /// <summary>
    /// Gets the total number of cache requests.
    /// </summary>
    public static int CacheRequests => _cacheRequests;

    /// <summary>
    /// Gets the cache hit rate as a percentage.
    /// </summary>
    public static double CacheHitRate
    {
        get
        {
            if (_cacheRequests == 0)
            {
                return 0;
            }

            return (_cachedImages * 100.0) / _cacheRequests;
        }
    }

    /// <summary>
    /// Gets the total cache size in bytes.
    /// </summary>
    public static long TotalCacheSize => _totalCacheSize;

    /// <summary>
    /// Gets the total number of cached files.
    /// </summary>
    public static int TotalCachedFiles => _totalCachedFiles;

    /// <summary>
    /// Initializes the cache directory. Called lazily on first use.
    /// </summary>
    private void EnsureInitialized()
    {
        if (_cacheDirectory != null)
        {
            return;
        }

        lock (_initLock)
        {
            if (_cacheDirectory != null)
            {
                return;
            }

            try
            {
                _cacheDirectory = Path.Combine(Plugin.Instance.DataFolderPath, "thumbnails");

                // Create cache directory if it doesn't exist
                if (!Directory.Exists(_cacheDirectory))
                {
                    Directory.CreateDirectory(_cacheDirectory);
                }

                // Start cleanup task once
                if (!_cleanupTaskStarted)
                {
                    _cleanupTaskStarted = true;
                    _cleanupCancellation = new CancellationTokenSource();
                    _cleanupTask = Task.Run(() => CleanupOldFilesAsync(_cleanupCancellation.Token));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize thumbnail cache directory");
                _cacheDirectory = Path.GetTempPath(); // Fallback to temp directory
            }
        }
    }

    /// <summary>
    /// Gets a cached URL or returns the original if caching is disabled or fails.
    /// </summary>
    /// <param name="imageUrl">The original image URL.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached local path or original URL.</returns>
    public async Task<string?> GetCachedUrlAsync(string? imageUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            return imageUrl;
        }

        try
        {
            // Initialize on first use
            EnsureInitialized();

            // Check if caching is enabled
            if (!Plugin.Instance.Configuration.EnableThumbnailCache)
            {
                return imageUrl;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to check cache configuration, returning original URL");
            return imageUrl;
        }

        Interlocked.Increment(ref _cacheRequests);

        try
        {
            // Generate cache key from URL
            string cacheKey = GetCacheKey(imageUrl);
            string cachedFilePath = GetCachedFilePath(cacheKey);

            // Check if already cached
            if (File.Exists(cachedFilePath))
            {
                // Update access time
                File.SetLastAccessTime(cachedFilePath, DateTime.UtcNow);
                Interlocked.Increment(ref _cachedImages);
                return cachedFilePath;
            }

            // Check if download is already in progress
            var downloadTask = _inProgressDownloads.GetOrAdd(cacheKey, _ => DownloadAndCacheAsync(imageUrl, cachedFilePath, cancellationToken));

            var result = await downloadTask.ConfigureAwait(false);

            // Remove from in-progress dictionary
            _inProgressDownloads.TryRemove(cacheKey, out _);

            if (result != null)
            {
                Interlocked.Increment(ref _cachedImages);
                return result;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cache thumbnail from {Url}", imageUrl);
        }

        // Return original URL if caching fails
        return imageUrl;
    }

    /// <summary>
    /// Downloads and caches an image from a URL.
    /// </summary>
    /// <param name="imageUrl">The image URL.</param>
    /// <param name="cachedFilePath">The local cache file path.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The cached file path or null if failed.</returns>
    private async Task<string?> DownloadAndCacheAsync(string imageUrl, string cachedFilePath, CancellationToken cancellationToken)
    {
        try
        {
            using var httpClient = _httpClientFactory.CreateClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);

            using var response = await httpClient.GetAsync(imageUrl, cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download thumbnail from {Url}: {StatusCode}", imageUrl, response.StatusCode);
                return null;
            }

            // Ensure directory exists
            var directory = Path.GetDirectoryName(cachedFilePath);
            if (directory != null && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Download to temporary file first
            var tempFile = cachedFilePath + ".tmp";

            using (var fileStream = File.Create(tempFile))
            {
                await response.Content.CopyToAsync(fileStream, cancellationToken).ConfigureAwait(false);
            }

            // Move temp file to final location
            File.Move(tempFile, cachedFilePath, overwrite: true);

            // Set file times
            File.SetCreationTime(cachedFilePath, DateTime.UtcNow);
            File.SetLastAccessTime(cachedFilePath, DateTime.UtcNow);

            _logger.LogDebug("Cached thumbnail from {Url} to {Path}", imageUrl, cachedFilePath);

            return cachedFilePath;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download and cache thumbnail from {Url}", imageUrl);
            return null;
        }
    }

    /// <summary>
    /// Generates a cache key from a URL.
    /// </summary>
    /// <param name="url">The URL.</param>
    /// <returns>The cache key.</returns>
    private static string GetCacheKey(string url)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(url));
        return BitConverter.ToString(hash).Replace("-", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }

    /// <summary>
    /// Gets the cached file path for a cache key.
    /// </summary>
    /// <param name="cacheKey">The cache key.</param>
    /// <returns>The file path.</returns>
    private string GetCachedFilePath(string cacheKey)
    {
        EnsureInitialized();

        // Use subdirectories to avoid too many files in one directory
        var subDir = cacheKey.Substring(0, 2);
        return Path.Combine(_cacheDirectory!, subDir, cacheKey);
    }

    /// <summary>
    /// Cleans up old cached files based on retention policy.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for graceful shutdown.</param>
    private async Task CleanupOldFilesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Wait until maintenance window on first run
            await WaitForMaintenanceWindowAsync(cancellationToken).ConfigureAwait(false);

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    PerformCleanupAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during thumbnail cache cleanup");
                }

                // Wait until next maintenance window
                await WaitForMaintenanceWindowAsync(cancellationToken).ConfigureAwait(false);
            }

            _logger.LogInformation("Thumbnail cache cleanup task cancelled gracefully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Thumbnail cache cleanup task cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Thumbnail cache cleanup task stopped unexpectedly");
        }
    }

    /// <summary>
    /// Waits until the next maintenance window starts.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    private async Task WaitForMaintenanceWindowAsync(CancellationToken cancellationToken)
    {
        var config = Plugin.Instance.Configuration;
        var now = DateTime.Now;
        var startHour = config.MaintenanceStartHour;

        // Calculate next maintenance window start
        var nextRun = now.Date.AddHours(startHour);
        if (nextRun <= now)
        {
            nextRun = nextRun.AddDays(1);
        }

        var delay = nextRun - now;
        _logger.LogDebug("Next thumbnail cache cleanup scheduled at {Time} (in {Hours:F1} hours)", nextRun, delay.TotalHours);

        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Performs the actual cache cleanup and statistics update.
    /// </summary>
    private void PerformCleanupAsync()
    {
        EnsureInitialized();

        if (_cacheDirectory == null || !Directory.Exists(_cacheDirectory))
        {
            return;
        }

        var retentionDays = Plugin.Instance.Configuration.ThumbnailCacheRetentionDays;
        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        var files = Directory.GetFiles(_cacheDirectory, "*", SearchOption.AllDirectories);
        int deletedCount = 0;
        long deletedSize = 0;
        long totalSize = 0;
        int remainingFiles = 0;

        foreach (var file in files)
        {
            try
            {
                var fileInfo = new FileInfo(file);
                var fileSize = fileInfo.Length;

                // Delete files that haven't been accessed in retention period
                var lastAccess = File.GetLastAccessTime(file);
                if (lastAccess < cutoffDate)
                {
                    File.Delete(file);
                    deletedCount++;
                    deletedSize += fileSize;
                }
                else
                {
                    totalSize += fileSize;
                    remainingFiles++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to process cached file: {File}", file);
            }
        }

        // Update statistics
        Interlocked.Exchange(ref _totalCacheSize, totalSize);
        Interlocked.Exchange(ref _totalCachedFiles, remainingFiles);

        if (deletedCount > 0)
        {
            _logger.LogInformation(
                "Cleaned up {Count} old cached thumbnails ({Size:F2} MB freed). Cache: {Files} files, {TotalSize:F2} MB",
                deletedCount,
                deletedSize / (1024.0 * 1024.0),
                remainingFiles,
                totalSize / (1024.0 * 1024.0));
        }
        else
        {
            _logger.LogDebug("No thumbnails to clean up. Cache: {Files} files, {Size:F2} MB", remainingFiles, totalSize / (1024.0 * 1024.0));
        }

        // Clean up empty subdirectories
        var dirs = Directory.GetDirectories(_cacheDirectory, "*", SearchOption.AllDirectories);
        foreach (var dir in dirs)
        {
            try
            {
                if (!Directory.EnumerateFileSystemEntries(dir).Any())
                {
                    Directory.Delete(dir);
                }
            }
            catch
            {
                // Ignore errors when deleting empty directories
            }
        }
    }

    /// <summary>
    /// Manually triggers a cache cleanup.
    /// </summary>
    public void TriggerCleanup()
    {
        _logger.LogInformation("Manual thumbnail cache cleanup triggered");
        PerformCleanupAsync();
    }

    /// <summary>
    /// Clears the entire thumbnail cache.
    /// </summary>
    public void ClearCache()
    {
        EnsureInitialized();

        if (_cacheDirectory == null || !Directory.Exists(_cacheDirectory))
        {
            return;
        }

        try
        {
            var files = Directory.GetFiles(_cacheDirectory, "*", SearchOption.AllDirectories);
            int deletedCount = 0;
            long deletedSize = 0;

            foreach (var file in files)
            {
                try
                {
                    var fileSize = new FileInfo(file).Length;
                    File.Delete(file);
                    deletedCount++;
                    deletedSize += fileSize;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to delete cached file: {File}", file);
                }
            }

            // Update statistics
            Interlocked.Exchange(ref _totalCacheSize, 0);
            Interlocked.Exchange(ref _totalCachedFiles, 0);

            _logger.LogInformation(
                "Cleared thumbnail cache: {Count} files deleted ({Size:F2} MB freed)",
                deletedCount,
                deletedSize / (1024.0 * 1024.0));

            // Clean up empty subdirectories
            var dirs = Directory.GetDirectories(_cacheDirectory, "*", SearchOption.AllDirectories);
            foreach (var dir in dirs)
            {
                try
                {
                    if (!Directory.EnumerateFileSystemEntries(dir).Any())
                    {
                        Directory.Delete(dir);
                    }
                }
                catch
                {
                    // Ignore errors
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear thumbnail cache");
        }
    }

    /// <summary>
    /// Resets cache statistics.
    /// </summary>
    public static void ResetStatistics()
    {
        Interlocked.Exchange(ref _cachedImages, 0);
        Interlocked.Exchange(ref _cacheRequests, 0);
    }

    /// <summary>
    /// Stops the background cleanup task gracefully.
    /// </summary>
    public void Shutdown()
    {
        if (_cleanupCancellation == null || _cleanupTask == null)
        {
            return;
        }

        _logger.LogInformation("Shutting down thumbnail cache service...");

        try
        {
            _cleanupCancellation.Cancel();

            // Wait max 5 seconds for graceful shutdown
            if (!_cleanupTask.Wait(TimeSpan.FromSeconds(5)))
            {
                _logger.LogWarning("Thumbnail cleanup task did not complete within timeout");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during thumbnail cache shutdown");
        }
    }

    /// <summary>
    /// Disposes resources used by the thumbnail cache service.
    /// </summary>
    public void Dispose()
    {
        Shutdown();
        _cleanupCancellation?.Dispose();
        GC.SuppressFinalize(this);
    }
}
