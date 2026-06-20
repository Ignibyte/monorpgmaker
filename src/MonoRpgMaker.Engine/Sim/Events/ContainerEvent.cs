using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in <c>Chest</c> behaviour: a give-once container. The first time it fires (its open-switch unset) it
/// grants <c>amount</c> of an item (a <see cref="GameState"/> counter keyed <c>item.&lt;id&gt;</c>), sets the
/// open-switch, and shows a message; afterwards it shows an "empty" message. Returns declarative outcomes (D-0017)
/// composed from the EXISTING vocabulary (<see cref="AddCounter"/> + <see cref="SetSwitch"/> +
/// <see cref="ShowMessage"/>) — no new outcome case. Implements <see cref="IMapEvent"/> (D-0024); materialised
/// from a <c>Chest</c> placement by the <see cref="BehaviourRegistry"/>. Named <c>ContainerEvent</c> to avoid a
/// clash with the #13 tracer-bullet <c>Tracer.ChestEvent</c>.
/// </summary>
public sealed class ContainerEvent : IMapEvent
{
    private readonly string _itemId;
    private readonly int _amount;
    private readonly string _openSwitch;
    private readonly string _message;

    /// <summary>Create the chest at <paramref name="cell"/>, fired by <paramref name="trigger"/>, granting <paramref name="amount"/> of <paramref name="itemId"/> once (gated by <paramref name="openSwitch"/>) with <paramref name="message"/>.</summary>
    public ContainerEvent(GridPoint cell, EventTrigger trigger, string itemId, int amount, string openSwitch, string message)
    {
        ArgumentNullException.ThrowIfNull(itemId);
        ArgumentNullException.ThrowIfNull(openSwitch);
        ArgumentNullException.ThrowIfNull(message);
        Cell = cell;
        Trigger = trigger;
        _itemId = itemId;
        _amount = amount;
        _openSwitch = openSwitch;
        _message = message;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context)
    {
        if (context.GetSwitch(_openSwitch))
        {
            return [new ShowMessage("The chest is empty.")];
        }

        return [new AddCounter("item." + _itemId, _amount), new SetSwitch(_openSwitch, true), new ShowMessage(_message)];
    }
}
