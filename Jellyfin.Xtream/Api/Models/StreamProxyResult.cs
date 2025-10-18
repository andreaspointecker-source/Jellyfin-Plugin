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
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Xtream.Service;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Xtream.Api.Models;

/// <summary>
/// Streams provider content to the HTTP response.
/// </summary>
public sealed class StreamProxyResult(StreamTokenService.StreamAccess access) : IActionResult
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(5);

    /// <inheritdoc />
    public async Task ExecuteResultAsync(ActionContext context)
    {
        CancellationToken requestAborted = context.HttpContext.RequestAborted;

        context.HttpContext.Response.StatusCode = StatusCodes.Status200OK;
        context.HttpContext.Response.ContentType = access.ContentType;
        context.HttpContext.Response.Headers.CacheControl = "no-store";
        context.HttpContext.Response.Headers["X-CandyTv-Proxy"] = "true";

        if (access.ContentLength.HasValue)
        {
            context.HttpContext.Response.ContentLength = access.ContentLength.Value;
        }
        else
        {
            context.HttpContext.Response.Headers.Remove("Content-Length");
        }

        byte[] buffer = new byte[64 * 1024];
        int bytesRead;
        DateTimeOffset lastFlush = DateTimeOffset.UtcNow;

        StreamTokenService.StreamAccess leasedAccess = access;
        try
        {
            while ((bytesRead = await leasedAccess.Stream.ReadAsync(buffer.AsMemory(0, buffer.Length), requestAborted).ConfigureAwait(false)) > 0)
            {
                await context.HttpContext.Response.Body.WriteAsync(buffer.AsMemory(0, bytesRead), requestAborted).ConfigureAwait(false);

                if (DateTimeOffset.UtcNow - lastFlush > FlushInterval)
                {
                    await context.HttpContext.Response.Body.FlushAsync(requestAborted).ConfigureAwait(false);
                    lastFlush = DateTimeOffset.UtcNow;
                }
            }

            await context.HttpContext.Response.Body.FlushAsync(requestAborted).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Client disconnected.
        }
        finally
        {
            await leasedAccess.DisposeAsync().ConfigureAwait(false);
        }
    }
}
