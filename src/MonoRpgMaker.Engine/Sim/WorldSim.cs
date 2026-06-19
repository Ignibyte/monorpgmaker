using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The deterministic map simulation: the player walking a <see cref="TileMap"/>, the switches in
/// <see cref="GameState"/>, the map events (dispatched through an ordered, ambiguity-checked
/// <see cref="HookSchedule"/>), and switch-driven doors. Holds no rendering state — the host reads
/// <see cref="CurrentMessage"/> to draw.
/// </summary>
public sealed class WorldSim
{
    private readonly HookSchedule _schedule;
    private readonly List<DoorRule> _doors;
    private readonly EventContext _eventContext;
    private readonly OutcomeApplier _applier;

    private WorldSim(TileMap map, Actor player, HookSchedule schedule, IEnumerable<DoorRule> doors)
    {
        Map = map;
        Player = player;
        State = new GameState();
        _schedule = schedule;
        _doors = new List<DoorRule>(doors);
        _eventContext = new EventContext(State);
        _applier = new OutcomeApplier(State, message => CurrentMessage = message);
        SyncDoors();
    }

    /// <summary>
    /// Assemble a simulation from a map, a player, its events and its doors. Returns a typed
    /// <see cref="WorldSimResult.Failure"/> when the events are ambiguous (two share a cell, trigger, and
    /// order); the null-argument guards throw, as a null is a programmer error rather than bad data.
    /// </summary>
    public static WorldSimResult TryCreate(
        TileMap map, Actor player, IEnumerable<IMapEvent> events, IEnumerable<DoorRule> doors)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(doors);

        ScheduleResult built = HookSchedule.Build(new List<IMapEvent>(events));
        if (!built.Ok)
        {
            return WorldSimResult.Failure(built.Error!);
        }

        return WorldSimResult.Success(new WorldSim(map, player, built.Schedule!, doors));
    }

    /// <summary>The map being walked.</summary>
    public TileMap Map { get; }

    /// <summary>The party leader the input drives.</summary>
    public Actor Player { get; }

    /// <summary>The live switch store.</summary>
    public GameState State { get; }

    /// <summary>The message awaiting display, or null when none is active.</summary>
    public string? CurrentMessage { get; private set; }

    /// <summary>
    /// Try to step the player one tile. Clears any pending message first; on a successful step, fires
    /// step-on events at the new cell and re-syncs doors. Returns true when the player moved.
    /// </summary>
    public bool MovePlayer(Direction direction)
    {
        CurrentMessage = null;
        if (!Player.TryStep(direction, Map))
        {
            return false;
        }

        FireHooks(Player.Cell, EventTrigger.StepOn);
        SyncDoors();
        return true;
    }

    /// <summary>
    /// Press the action button: fire the action-button events on the cell the player faces, in order, and
    /// apply their outcomes; then re-sync doors. Clears any pending message first. Returns true when an
    /// event fired.
    /// </summary>
    public bool PressAction()
    {
        CurrentMessage = null;
        Point faced = Player.Cell + Player.Facing.ToStep();
        bool fired = FireHooks(faced, EventTrigger.ActionButton);
        SyncDoors();
        return fired;
    }

    /// <summary>Apply every door rule: open/close each door cell per its switch.</summary>
    public void SyncDoors()
    {
        foreach (DoorRule door in _doors)
        {
            Map.SetTile(door.Cell, State.Get(door.Switch) ? door.OpenTile : door.ClosedTile);
        }
    }

    private bool FireHooks(Point cell, EventTrigger trigger)
    {
        bool fired = false;
        foreach (IMapEvent mapEvent in _schedule.EventsAt(cell.ToGridPoint(), trigger))
        {
            _applier.Apply(mapEvent.Run(_eventContext));
            fired = true;
        }

        return fired;
    }
}

/// <summary>
/// The result of <see cref="WorldSim.TryCreate"/>: either a built <see cref="Sim"/> or a single
/// <see cref="Error"/> reason (an ambiguous hook schedule). A total result — never throws on the
/// validation path.
/// </summary>
public sealed class WorldSimResult
{
    private WorldSimResult(WorldSim? sim, string? error)
    {
        Sim = sim;
        Error = error;
    }

    /// <summary>The simulation when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public WorldSim? Sim { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the build succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful build carrying <paramref name="sim"/>.</summary>
    public static WorldSimResult Success(WorldSim sim) => new(sim, null);

    /// <summary>A failed build carrying the <paramref name="error"/> reason.</summary>
    public static WorldSimResult Failure(string error) => new(null, error);
}
