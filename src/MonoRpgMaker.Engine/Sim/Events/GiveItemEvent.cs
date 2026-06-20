using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in <c>GiveItem</c> behaviour: on its trigger it grants <c>amount</c> of an item — a
/// <see cref="GameState"/> counter keyed <c>item.&lt;id&gt;</c> — and shows a message. Returns declarative
/// outcomes (D-0017) composed from the EXISTING vocabulary (<see cref="AddCounter"/> + <see cref="ShowMessage"/>),
/// so no new outcome case is needed. Implements the published <see cref="IMapEvent"/> seam (D-0024); materialised
/// from a <c>GiveItem</c> placement by the <see cref="BehaviourRegistry"/> — the repeatable recipe.
/// </summary>
public sealed class GiveItemEvent : IMapEvent
{
    private readonly string _itemId;
    private readonly int _amount;
    private readonly string _message;

    /// <summary>Create the give-item at <paramref name="cell"/>, fired by <paramref name="trigger"/>, granting <paramref name="amount"/> of <paramref name="itemId"/> with <paramref name="message"/>.</summary>
    public GiveItemEvent(GridPoint cell, EventTrigger trigger, string itemId, int amount, string message)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(message);
        Cell = cell;
        Trigger = trigger;
        _itemId = itemId;
        _amount = amount;
        _message = message;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context) =>
        [new AddCounter("item." + _itemId, _amount), new ShowMessage(_message)];
}
