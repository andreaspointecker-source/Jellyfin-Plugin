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
namespace Jellyfin.Xtream.Configuration;

/// <summary>
/// Represents a custom channel category for grouping live TV channels.
/// </summary>
public class CustomChannelCategory
{
    /// <summary>
    /// Gets or sets the unique identifier for this category.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the category.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the description of the category.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the list of channel stream IDs included in this category.
    /// </summary>
    public HashSet<int> ChannelIds { get; set; } = [];

    /// <summary>
    /// Gets or sets the sort order for this category.
    /// </summary>
    public int SortOrder { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this category is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the icon URL for this category.
    /// </summary>
    public string? IconUrl { get; set; }

    /// <summary>
    /// Gets or sets the date when this category was created.
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the date when this category was last modified.
    /// </summary>
    public DateTime ModifiedDate { get; set; } = DateTime.UtcNow;
}
#pragma warning restore CA2227
