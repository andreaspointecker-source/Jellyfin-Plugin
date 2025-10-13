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
using Microsoft.Extensions.Logging;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Manages single-connection constraint for Xtream API.
/// </summary>
public static class ConnectionManager
{
    private static readonly SemaphoreSlim _connectionSemaphore = new SemaphoreSlim(1, 1);
    private static readonly TimeSpan _minRequestInterval = TimeSpan.FromMilliseconds(100);
    private static DateTime _lastRequestTime = DateTime.MinValue;
    private static int _totalRequests = 0;
    private static int _queuedRequests = 0;

    /// <summary>
    /// Gets the total number of API requests made.
    /// </summary>
    public static int TotalRequests => _totalRequests;

    /// <summary>
    /// Gets the number of currently queued requests.
    /// </summary>
    public static int QueuedRequests => _queuedRequests;

    /// <summary>
    /// Gets a value indicating whether the connection is currently busy.
    /// </summary>
    public static bool IsBusy => _connectionSemaphore.CurrentCount == 0;

    /// <summary>
    /// Executes an API call with connection queueing.
    /// </summary>
    /// <typeparam name="T">The return type of the API call.</typeparam>
    /// <param name="apiCall">The API call function to execute.</param>
    /// <param name="logger">Optional logger for diagnostics.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The result of the API call.</returns>
    public static async Task<T> ExecuteAsync<T>(
        Func<Task<T>> apiCall,
        ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        Interlocked.Increment(ref _queuedRequests);

        try
        {
            logger?.LogDebug("Connection request queued. Queue size: {QueueSize}", _queuedRequests);

            await _connectionSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

            try
            {
                Interlocked.Decrement(ref _queuedRequests);

                // Enforce minimum interval between requests
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime;
                if (timeSinceLastRequest < _minRequestInterval)
                {
                    var delay = _minRequestInterval - timeSinceLastRequest;
                    logger?.LogDebug("Throttling request for {Delay}ms", delay.TotalMilliseconds);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }

                _lastRequestTime = DateTime.UtcNow;
                Interlocked.Increment(ref _totalRequests);

                logger?.LogDebug("Executing API call. Total requests: {Total}", _totalRequests);

                return await apiCall().ConfigureAwait(false);
            }
            finally
            {
                _connectionSemaphore.Release();
            }
        }
        catch (OperationCanceledException)
        {
            Interlocked.Decrement(ref _queuedRequests);
            throw;
        }
    }

    /// <summary>
    /// Resets the connection statistics.
    /// </summary>
    public static void ResetStatistics()
    {
        Interlocked.Exchange(ref _totalRequests, 0);
    }
}
