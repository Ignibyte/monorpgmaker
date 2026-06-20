using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// Serializes a <see cref="TileMap"/> to / from the JSON <c>$data</c> format. Pure + framework-thin: it
/// operates on a <see cref="string"/> (the host owns file IO) and is integer/bool only, so a loaded map is
/// bit-identical across runs. <see cref="Deserialize"/> is total — a malformed input yields a typed
/// <see cref="MapLoadResult"/>, never a throw.
/// </summary>
public static class MapSerializer
{
    /// <summary>Serialize <paramref name="map"/> to the JSON <c>$data</c> string (row-major cells, no events).</summary>
    public static string Serialize(TileMap map) => Serialize(map, Array.Empty<EventData>());

    /// <summary>Serialize <paramref name="map"/> and its placed <paramref name="events"/> to the JSON <c>$data</c> string.</summary>
    public static string Serialize(TileMap map, IReadOnlyList<EventData> events)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(events);

        var tiles = new TileData[map.Width * map.Height];
        for (var y = 0; y < map.Height; y++)
        {
            for (var x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(new Point(x, y));
                tiles[(y * map.Width) + x] = new TileData { TilesetId = tile.TilesetId, Blocking = tile.Blocking };
            }
        }

        var placed = new EventData[events.Count];
        for (var i = 0; i < placed.Length; i++)
        {
            placed[i] = events[i];
        }

        var data = new TileMapData { Width = map.Width, Height = map.Height, Tiles = tiles, Events = placed };
        return JsonSerializer.Serialize(data, MapJsonContext.Default.TileMapData);
    }

    /// <summary>
    /// Parse a JSON <c>$data</c> <paramref name="json"/> string into a <see cref="TileMap"/>. Returns a typed
    /// <see cref="MapLoadResult.Failure"/> for malformed JSON, non-positive dimensions, missing tiles, or a
    /// tile count that does not match <c>Width * Height</c> — it never throws on the parse path.
    /// </summary>
    public static MapLoadResult Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        TileMapData? data;
        try
        {
            data = JsonSerializer.Deserialize(json, MapJsonContext.Default.TileMapData);
        }
        catch (JsonException ex)
        {
            return MapLoadResult.Failure("invalid JSON: " + ex.Message);
        }

        if (data is null)
        {
            return MapLoadResult.Failure("the $data document was null");
        }

        if (data.Width <= 0)
        {
            return MapLoadResult.Failure("width must be positive");
        }

        if (data.Height <= 0)
        {
            return MapLoadResult.Failure("height must be positive");
        }

        if (data.Tiles is null)
        {
            return MapLoadResult.Failure("tiles are missing");
        }

        // A long product: a huge Width*Height (e.g. 65536*65536) would overflow int and could WRAP to a value
        // that spuriously matches Tiles.Length, slipping past this guard into an out-of-bounds build loop.
        if ((long)data.Width * data.Height != data.Tiles.Length)
        {
            return MapLoadResult.Failure("tile count must equal width * height");
        }

        var map = new TileMap(data.Width, data.Height);
        for (var y = 0; y < data.Height; y++)
        {
            for (var x = 0; x < data.Width; x++)
            {
                var cell = data.Tiles[(y * data.Width) + x];
                map.SetTile(new Point(x, y), new Tile(cell.TilesetId, cell.Blocking));
            }
        }

        // Events are optional; validate each placement ELEMENT (totality — a hostile placement must not throw;
        // the semantic kind→behaviour binding is the registry's job, so only structural fields are checked here).
        EventData[] events = data.Events ?? [];
        for (var i = 0; i < events.Length; i++)
        {
            EventData placement = events[i];
            if (placement is null)
            {
                return MapLoadResult.Failure("event " + i + " is null");
            }

            if (string.IsNullOrEmpty(placement.Id) || string.IsNullOrEmpty(placement.Kind) || string.IsNullOrEmpty(placement.Trigger))
            {
                return MapLoadResult.Failure("event " + i + " is missing id, kind, or trigger");
            }

            if (placement.Trigger is not ("StepOn" or "ActionButton"))
            {
                return MapLoadResult.Failure("event " + i + " has an unknown trigger '" + placement.Trigger + "'");
            }
        }

        return MapLoadResult.Success(map, events);
    }
}

/// <summary>The System.Text.Json source-generation context for the map <c>$data</c> DTOs (AOT-safe, no reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(TileMapData))]
internal sealed partial class MapJsonContext : JsonSerializerContext
{
}
