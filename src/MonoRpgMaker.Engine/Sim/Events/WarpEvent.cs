using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in transition behaviour: on its trigger it returns a <see cref="Warp"/> outcome to a target map +
/// cell. The sim-host orchestration (a <see cref="GameSession"/>) applies the switch — the behaviour stays pure
/// over the read context and never mutates (D-0017). Implements the published <see cref="IMapEvent"/> seam
/// (D-0024); materialised from a <c>Warp</c> placement by the <see cref="BehaviourRegistry"/> — the repeatable
/// recipe: implement <see cref="IMapEvent"/>, register the kind, declare its <c>.expect</c>.
/// </summary>
public sealed class WarpEvent : IMapEvent
{
    private readonly string _mapId;
    private readonly GridPoint _target;

    /// <summary>Create the warp at <paramref name="cell"/>, fired by <paramref name="trigger"/>, sending the player to <paramref name="target"/> on map <paramref name="mapId"/>.</summary>
    public WarpEvent(GridPoint cell, EventTrigger trigger, string mapId, GridPoint target)
    {
        ArgumentNullException.ThrowIfNull(mapId);
        Cell = cell;
        Trigger = trigger;
        _mapId = mapId;
        _target = target;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context) => [new Warp(_mapId, _target)];
}
