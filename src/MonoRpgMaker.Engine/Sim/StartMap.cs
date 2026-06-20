using System;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The runtime's bundled start map — the canonical first room a freshly-cloned game boots into (D-0023: the
/// engine loads only its own bundled content). Built programmatically and serialized to
/// <c>content/maps/start.json</c> (the committed <c>$data</c> the Player embeds + loads). A walkable floor inside
/// a <see cref="Tile.Blocking"/> wall border, painted with LPC mountains-sheet tile indices. No events yet.
/// </summary>
public static class StartMap
{
    /// <summary>Map width in tiles (wider than the viewport so the camera scrolls).</summary>
    public const int Width = 28;

    /// <summary>Map height in tiles (taller than the viewport so the camera scrolls).</summary>
    public const int Height = 18;

    /// <summary>The walkable floor tile (an LPC mountains ground index).</summary>
    public static readonly Tile Floor = new(TilesetId: 66, Blocking: false);

    /// <summary>The solid wall tile (an LPC mountains cliff index).</summary>
    public static readonly Tile Wall = new(TilesetId: 1, Blocking: true);

    /// <summary>The player's start cell (a guaranteed-floor interior cell).</summary>
    public static readonly Point PlayerStart = new(2, 2);

    /// <summary>Build the canonical start map: a floor interior inside a blocking wall border.</summary>
    public static TileMap Build()
    {
        var map = new TileMap(Width, Height);
        for (var y = 0; y < Height; y++)
        {
            for (var x = 0; x < Width; x++)
            {
                var border = x == 0 || y == 0 || x == Width - 1 || y == Height - 1;
                map.SetTile(new Point(x, y), border ? Wall : Floor);
            }
        }

        return map;
    }

    /// <summary>Build a no-events <see cref="WorldSim"/> over <see cref="Build"/>, the player at <see cref="PlayerStart"/>.</summary>
    public static WorldSim BuildWorld()
    {
        WorldSimResult result = CreateWorld(Build());
        return result.Sim ?? throw new InvalidOperationException(result.Error);
    }

    /// <summary>
    /// Load a <see cref="WorldSim"/> from a <c>$data</c> JSON <paramref name="json"/> string (the bundled start
    /// map). Returns a typed failure for malformed data — never throws on the parse path.
    /// </summary>
    public static WorldSimResult LoadWorld(string json)
    {
        MapLoadResult loaded = MapSerializer.Deserialize(json);
        if (!loaded.Ok)
        {
            return WorldSimResult.Failure(loaded.Error!);
        }

        return CreateWorld(loaded.Map!);
    }

    private static WorldSimResult CreateWorld(TileMap map)
    {
        var player = new Actor("Hero", PlayerStart, maxHp: 30);
        return WorldSim.TryCreate(map, player, Array.Empty<IMapEvent>(), Array.Empty<DoorRule>());
    }
}
