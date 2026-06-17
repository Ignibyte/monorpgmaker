using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The deterministic map simulation: the player walking a <see cref="TileMap"/>, the
/// switches in <see cref="GameState"/>, the map events, and switch-driven doors. Holds
/// no rendering state — the host reads <see cref="CurrentMessage"/> to draw.
/// </summary>
public sealed class WorldSim
{
    private readonly List<IMapEvent> _events;
    private readonly List<DoorRule> _doors;
    private readonly EventContext _eventContext;

    /// <summary>Assemble a simulation from a map, a player, its events and its doors.</summary>
    public WorldSim(TileMap map, Actor player, IEnumerable<IMapEvent> events, IEnumerable<DoorRule> doors)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(events);
        ArgumentNullException.ThrowIfNull(doors);

        Map = map;
        Player = player;
        State = new GameState();
        _events = new List<IMapEvent>(events);
        _doors = new List<DoorRule>(doors);
        _eventContext = new EventContext(State, message => CurrentMessage = message);
        SyncDoors();
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
    /// Try to step the player one tile. Clears any pending message first; on a
    /// successful step, fires step-on events at the new cell and re-syncs doors.
    /// Returns true when the player moved.
    /// </summary>
    public bool MovePlayer(Direction direction)
    {
        CurrentMessage = null;
        if (!Player.TryStep(direction, Map))
            return false;

        FireStepOn(Player.Cell);
        SyncDoors();
        return true;
    }

    /// <summary>Apply every door rule: open/close each door cell per its switch.</summary>
    public void SyncDoors()
    {
        foreach (var door in _doors)
            Map.SetTile(door.Cell, State.Get(door.Switch) ? door.OpenTile : door.ClosedTile);
    }

    private void FireStepOn(Point cell)
    {
        foreach (var mapEvent in _events)
        {
            if (mapEvent.Trigger == EventTrigger.StepOn && mapEvent.Cell == cell)
                mapEvent.Run(_eventContext);
        }
    }
}
