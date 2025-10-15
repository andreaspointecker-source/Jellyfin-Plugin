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
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Xtream.Api;

/// <summary>
/// Controller for serving the plugin icon image.
/// </summary>
[ApiController]
[Route("Plugins/{pluginId}/{version}")]
public class PluginImageController : ControllerBase
{
    /// <summary>
    /// Get the plugin thumbnail image.
    /// </summary>
    /// <param name="pluginId">The plugin ID.</param>
    /// <param name="version">The plugin version.</param>
    /// <returns>The plugin icon image.</returns>
    [HttpGet("Image")]
    [AllowAnonymous]
    [Produces("image/png")]
    public async Task<IActionResult> GetImage(string pluginId, string version)
    {
        try
        {
            // Method 1: Try to load from GitHub URL
            try
            {
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                var iconUrl = "https://raw.githubusercontent.com/andreaspointecker-source/Jellyfin-Plugin/master/Jellyfin.Xtream/icon/CandyTV.png";
                var response = await httpClient.GetAsync(iconUrl).ConfigureAwait(false);

                if (response.IsSuccessStatusCode)
                {
                    var imageBytes = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
                    return File(imageBytes, "image/png");
                }
            }
            catch
            {
                // Fallback to next method
            }

            // Method 2: Try to load from embedded resource
            var type = typeof(Plugin);
            var resourceName = $"{type.Namespace}.icon.CandyTV.png";
            var stream = type.Assembly.GetManifestResourceStream(resourceName);

            if (stream != null)
            {
                return File(stream, "image/png");
            }

            // Method 3: Try to load from file system
            var assemblyLocation = type.Assembly.Location;
            var assemblyDirectory = Path.GetDirectoryName(assemblyLocation);

            if (!string.IsNullOrEmpty(assemblyDirectory))
            {
                var thumbPath = Path.Combine(assemblyDirectory, "thumb.png");

                if (System.IO.File.Exists(thumbPath))
                {
                    var fileBytes = await System.IO.File.ReadAllBytesAsync(thumbPath).ConfigureAwait(false);
                    return File(fileBytes, "image/png");
                }
            }

            // No icon found
            return NotFound();
        }
        catch (Exception)
        {
            return NotFound();
        }
    }
}
