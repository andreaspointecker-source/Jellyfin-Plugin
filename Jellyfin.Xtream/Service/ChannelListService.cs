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
using System.Collections.ObjectModel;
using System.Linq;
using FuzzySharp;
using Jellyfin.Xtream.Client.Models;

namespace Jellyfin.Xtream.Service;

/// <summary>
/// Service for managing channel lists and fuzzy matching.
/// </summary>
public class ChannelListService
{
    private const int FuzzyMatchThreshold = 40;
    private const int TopMatchCount = 15;

    private static string NormalizeChannelName(string name)
    {
        return name
            .ToLowerInvariant()
            .Replace(".", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal)
            .Replace("  ", " ", StringComparison.Ordinal)
            .Trim();
    }

    /// <summary>
    /// Finds the best matching streams for a channel name using fuzzy matching.
    /// </summary>
    /// <param name="channelName">The channel name to match.</param>
    /// <param name="streams">Available streams from Xtream.</param>
    /// <returns>List of potential matches ordered by score.</returns>
    public IReadOnlyCollection<MatchResult> FindMatches(string channelName, IEnumerable<StreamInfo> streams)
    {
        var streamList = streams.ToList();
        var results = new List<MatchResult>();
        var normalizedChannel = NormalizeChannelName(channelName);

        foreach (var stream in streamList)
        {
            var streamName = stream.Name ?? string.Empty;
            var normalizedStream = NormalizeChannelName(streamName);
            var score = Fuzz.Ratio(normalizedChannel, normalizedStream);

            if (score >= FuzzyMatchThreshold)
            {
                results.Add(new MatchResult
                {
                    Stream = stream,
                    Score = score,
                    IsExact = score == 100,
                });
            }
        }

        return results
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.Stream.Name)
            .ToList();
    }

    /// <summary>
    /// Gets the top N best matches for a channel name.
    /// </summary>
    /// <param name="channelName">The channel name to match.</param>
    /// <param name="streams">Available streams from Xtream.</param>
    /// <param name="count">Number of top matches to return.</param>
    /// <returns>Top matching streams.</returns>
    public IReadOnlyCollection<MatchResult> GetTopMatches(string channelName, IEnumerable<StreamInfo> streams, int count = TopMatchCount)
    {
        var matches = FindMatches(channelName, streams);
        return matches.Take(count).ToList();
    }

    /// <summary>
    /// Gets the best automatic match for a channel name (if score is high enough).
    /// </summary>
    /// <param name="channelName">The channel name to match.</param>
    /// <param name="streams">Available streams from Xtream.</param>
    /// <returns>Best match or null if no confident match found.</returns>
    public MatchResult? GetBestMatch(string channelName, IEnumerable<StreamInfo> streams)
    {
        var matches = FindMatches(channelName, streams);
        var bestMatch = matches.FirstOrDefault();

        // Only return if it's a very confident match (>= 90) or exact match
        if (bestMatch != null && (bestMatch.IsExact || bestMatch.Score >= 90))
        {
            return bestMatch;
        }

        return null;
    }

    /// <summary>
    /// Parses a TXT file content into channel list entries (one per line).
    /// </summary>
    /// <param name="content">The TXT file content.</param>
    /// <returns>List of channel names.</returns>
    public IReadOnlyCollection<string> ParseTxtContent(string content)
    {
        return content
            .Split('\n')
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();
    }
}
