using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The runtime's bundled start map — the canonical first room a freshly-cloned game boots into (D-0023: the
/// engine loads only its own bundled content). Built programmatically and serialized to
/// <c>content/maps/start.json</c> (the committed <c>$data</c> the Player embeds + loads). A walkable floor inside
/// a <see cref="Tile.Blocking"/> wall border, painted with LPC mountains-sheet tile indices, with one placed
/// <c>ShowText</c> event (materialised through the behaviour registry, D-0024).
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

    /// <summary>The start map's placed events — data the runtime materialises through the <see cref="BehaviourRegistry"/>.</summary>
    public static EventData[] Events() =>
    [
        new EventData
        {
            Id = "sign-welcome",
            X = 6,
            Y = 4,
            Trigger = "ActionButton",
            Kind = "ShowText",
            Params = { ["text"] = "Welcome to monorpgmaker! Use the arrow keys to explore." },
        },
        new EventData
        {
            Id = "to-town",
            X = 14,
            Y = 9,
            Trigger = "StepOn",
            Kind = "Warp",
            Params = { ["map"] = "town", ["x"] = "2", ["y"] = "2" },
        },
    ];

    /// <summary>Build a <see cref="WorldSim"/> over <see cref="Build"/> + <see cref="Events"/>, the player at <see cref="PlayerStart"/>.</summary>
    public static WorldSim BuildWorld()
    {
        WorldSimResult result = CreateWorld(Build(), Events());
        return result.Sim ?? throw new InvalidOperationException(result.Error);
    }

    /// <summary>The town map — a second bundled room the start map warps to (proving the runtime map switch).</summary>
    public static TileMap TownBuild()
    {
        const int width = 16;
        const int height = 12;
        var map = new TileMap(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                map.SetTile(new Point(x, y), border ? Wall : Floor);
            }
        }

        return map;
    }

    /// <summary>The town map's placed events — a Warp back to the start map.</summary>
    public static EventData[] TownEvents() =>
    [
        new EventData
        {
            Id = "to-start",
            X = 12,
            Y = 9,
            Trigger = "StepOn",
            Kind = "Warp",
            Params = { ["map"] = "start", ["x"] = "2", ["y"] = "2" },
        },
    ];

    /// <summary>
    /// Build the canonical two-map <see cref="GameSession"/> (<c>start</c> + <c>town</c>, addressable by id), the
    /// player at <see cref="PlayerStart"/> on the start map — the programmatic bootstrap the Player falls back to,
    /// and the shape the bundled content (<c>game.json</c> + per-map <c>$data</c>) mirrors.
    /// </summary>
    public static GameSession BuildSession()
    {
        var maps = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["start"] = MapSerializer.Serialize(Build(), Events()),
            ["town"] = MapSerializer.Serialize(TownBuild(), TownEvents()),
        };
        GameSessionResult result = GameSession.Create(maps, "start", new GridPoint(PlayerStart.X, PlayerStart.Y));
        return result.Session ?? throw new InvalidOperationException(result.Error);
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

        return CreateWorld(loaded.Map!, loaded.Events);
    }

    private static WorldSimResult CreateWorld(TileMap map, IReadOnlyList<EventData> placements)
    {
        var events = new List<IMapEvent>(placements.Count);
        for (var i = 0; i < placements.Count; i++)
        {
            BehaviourResult materialised = BehaviourRegistry.TryMaterialize(placements[i]);
            if (!materialised.Ok)
            {
                return WorldSimResult.Failure(materialised.Error!);
            }

            events.Add(materialised.Event!);
        }

        var player = new Actor("Hero", PlayerStart, maxHp: 30);
        return WorldSim.TryCreate(map, player, events, Array.Empty<DoorRule>());
    }
}
