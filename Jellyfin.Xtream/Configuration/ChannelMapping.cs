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

namespace Jellyfin.Xtream.Configuration;

/// <summary>
/// Represents a mapping between a channel list entry and an Xtream stream.
/// </summary>
public class ChannelMapping
{
    /// <summary>
    /// Gets or sets the channel list ID.
    /// </summary>
    public string ListId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the entry name from the list.
    /// </summary>
    public string EntryName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the matched Xtream stream ID.
    /// </summary>
    public int StreamId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the mapping was manually confirmed.
    /// </summary>
    public bool IsManual { get; set; }

    /// <summary>
    /// Gets or sets the position in the list (for ordering).
    /// </summary>
    public int Position { get; set; }
}
