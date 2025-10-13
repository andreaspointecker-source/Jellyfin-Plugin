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
using Newtonsoft.Json;

namespace Jellyfin.Xtream.Client;

/// <summary>
/// Converts integer (0 or 1) to boolean.
/// </summary>
public class IntToBoolConverter : JsonConverter<bool>
{
    /// <inheritdoc />
    public override bool ReadJson(JsonReader reader, Type objectType, bool existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Integer)
        {
            long value = (long)reader.Value!;
            return value != 0;
        }

        if (reader.TokenType == JsonToken.String)
        {
            string? strValue = (string?)reader.Value;
            if (int.TryParse(strValue, out int intValue))
            {
                return intValue != 0;
            }
        }

        if (reader.TokenType == JsonToken.Boolean)
        {
            return (bool)reader.Value!;
        }

        return false;
    }

    /// <inheritdoc />
    public override void WriteJson(JsonWriter writer, bool value, JsonSerializer serializer)
    {
        writer.WriteValue(value ? 1 : 0);
    }
}
