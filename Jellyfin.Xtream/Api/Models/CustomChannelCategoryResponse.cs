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

#pragma warning disable CA2227
namespace Jellyfin.Xtream.Api.Models;

/// <summary>
/// Response model for custom channel categories.
/// </summary>
public class CustomChannelCategoryResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for this category.
    /// </summary>
    public required Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the category.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of channel stream IDs included in this category.
    /// </summary>
    public required HashSet<int> ChannelIds { get; set; }

    /// <summary>
    /// Gets or sets the number of channels in this category.
    /// </summary>
    public int ChannelCount { get; set; }

    /// <summary>
    /// Gets or sets the sort order for this category.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this category is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the icon URL for this category.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the date when this category was created.
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Gets or sets the date when this category was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; }
}
#pragma warning restore CA2227
