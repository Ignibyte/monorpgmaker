using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The runtime orchestrator above <see cref="WorldSim"/>: owns the embedded map registry (id → <c>$data</c>
/// JSON) and the active per-map simulation, and applies <see cref="Warp"/> transitions. After driving the active
/// sim it switches maps when the sim signals a <see cref="WorldSim.PendingWarp"/> — loading the target map,
/// building a fresh sim, placing the player at the target cell, and CARRYING the <see cref="GameState"/> across
/// (so switches/counters persist). Holds no rendering state; the host renders <see cref="Active"/>. Deterministic
/// — the switch carries no clock/RNG.
/// </summary>
public sealed class GameSession
{
    private readonly IReadOnlyDictionary<string, string> _maps;

    private GameSession(IReadOnlyDictionary<string, string> maps, WorldSim active, string activeMapId)
    {
        _maps = maps;
        Active = active;
        ActiveMapId = activeMapId;
    }

    /// <summary>The simulation for the map currently being played — the host renders its <c>Map</c> + <c>Player</c>.</summary>
    public WorldSim Active { get; private set; }

    /// <summary>The id of the map currently being played.</summary>
    public string ActiveMapId { get; private set; }

    /// <summary>
    /// Build a session over the embedded map set <paramref name="maps"/> (id → <c>$data</c> JSON), booting into
    /// <paramref name="startMapId"/> with the player at <paramref name="startCell"/>. Returns a typed failure for
    /// an unknown start id or a start map that fails to load/materialise — never throws on bad data.
    /// </summary>
    public static GameSessionResult Create(IReadOnlyDictionary<string, string> maps, string startMapId, GridPoint startCell)
    {
        ArgumentNullException.ThrowIfNull(maps);
        ArgumentNullException.ThrowIfNull(startMapId);

        WorldSimResult loaded = LoadMap(maps, startMapId, state: null, placeAt: startCell);
        return loaded.Ok
            ? GameSessionResult.Success(new GameSession(maps, loaded.Sim!, startMapId))
            : GameSessionResult.Failure(loaded.Error!);
    }

    /// <summary>Step the active map's player one tile, then apply any transition it requested; returns the active sim's step result.</summary>
    public bool MovePlayer(Direction direction)
    {
        bool moved = Active.MovePlayer(direction);
        ApplyPendingWarp();
        return moved;
    }

    /// <summary>Press the action button on the active map, then apply any transition it requested; returns whether an event fired.</summary>
    public bool PressAction()
    {
        bool fired = Active.PressAction();
        ApplyPendingWarp();
        return fired;
    }

    /// <summary>
    /// Restore the session from <paramref name="save"/> IN PLACE: switch the active map to the saved map id, with
    /// the <see cref="GameState"/> rebuilt from the saved switches/counters and the player at the saved cell.
    /// Returns <see langword="false"/> — leaving the active map + state UNCHANGED (no corruption) — when the saved
    /// map id is not in the registry; never throws on a structurally-valid save. The player's facing + the RNG
    /// state are not restored (this slice carries the map, the switches/counters, and the position).
    /// </summary>
    public bool TryRestore(SaveState save)
    {
        ArgumentNullException.ThrowIfNull(save);

        var switches = new KeyValuePair<string, bool>[save.Switches.Length];
        for (var i = 0; i < save.Switches.Length; i++)
        {
            switches[i] = new KeyValuePair<string, bool>(save.Switches[i].Key, save.Switches[i].Value);
        }

        var counters = new KeyValuePair<string, int>[save.Counters.Length];
        for (var i = 0; i < save.Counters.Length; i++)
        {
            counters[i] = new KeyValuePair<string, int>(save.Counters[i].Key, save.Counters[i].Value);
        }

        GameState restored = GameState.Restore(switches, counters);
        WorldSimResult loaded = LoadMap(_maps, save.MapId, restored, new GridPoint(save.PlayerX, save.PlayerY));
        if (!loaded.Ok)
        {
            return false;
        }

        Active = loaded.Sim!;
        ActiveMapId = save.MapId;
        return true;
    }

    private void ApplyPendingWarp()
    {
        if (Active.PendingWarp is not { } warp)
        {
            return;
        }

        // Carry the game state across the switch and place the player at the target cell. An unknown target map
        // or a load failure is a safe no-op (the player stays on the current map) — totality, never a throw.
        WorldSimResult loaded = LoadMap(_maps, warp.MapId, Active.State, warp.Cell);
        if (loaded.Ok)
        {
            Active = loaded.Sim!;
            ActiveMapId = warp.MapId;
        }
    }

    private static WorldSimResult LoadMap(
        IReadOnlyDictionary<string, string> maps, string mapId, GameState? state, GridPoint placeAt)
    {
        if (!maps.TryGetValue(mapId, out string? json))
        {
            return WorldSimResult.Failure("unknown map '" + mapId + "'");
        }

        MapLoadResult map = MapSerializer.Deserialize(json);
        if (!map.Ok)
        {
            return WorldSimResult.Failure(map.Error!);
        }

        var events = new List<IMapEvent>(map.Events.Count);
        for (var i = 0; i < map.Events.Count; i++)
        {
            BehaviourResult materialised = BehaviourRegistry.TryMaterialize(map.Events[i]);
            if (!materialised.Ok)
            {
                return WorldSimResult.Failure(materialised.Error!);
            }

            events.Add(materialised.Event!);
        }

        var player = new Actor("Hero", PlacePlayer(map.Map!, placeAt), maxHp: 30);
        return WorldSim.TryCreate(map.Map!, player, events, Array.Empty<DoorRule>(), state);
    }

    // The player spawns at the target cell when it is in-bounds; an off-map cell falls back to a safe interior
    // cell, so a bad warp never lands the player out of bounds (totality).
    private static Point PlacePlayer(TileMap map, GridPoint placeAt)
    {
        var point = new Point(placeAt.X, placeAt.Y);
        return map.InBounds(point) ? point : new Point(Math.Min(1, map.Width - 1), Math.Min(1, map.Height - 1));
    }
}

/// <summary>
/// The result of <see cref="GameSession.Create"/>: either a built <see cref="Session"/> or a single
/// <see cref="Error"/> reason. A total result — never throws on a malformed map set.
/// </summary>
public sealed class GameSessionResult
{
    private GameSessionResult(GameSession? session, string? error)
    {
        Session = session;
        Error = error;
    }

    /// <summary>The session when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public GameSession? Session { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the build succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful build carrying <paramref name="session"/>.</summary>
    public static GameSessionResult Success(GameSession session) => new(session, null);

    /// <summary>A failed build carrying the <paramref name="error"/> reason.</summary>
    public static GameSessionResult Failure(string error) => new(null, error);
}
