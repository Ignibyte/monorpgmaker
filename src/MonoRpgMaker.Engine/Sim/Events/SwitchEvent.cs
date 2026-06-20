using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in <c>Lever</c> behaviour: on its trigger it sets a switch once — e.g. opens a door whose
/// <c>DoorRule</c> reads that switch — and shows a message; once the switch is set it does nothing (give-once).
/// Returns declarative outcomes (D-0017) composed from the EXISTING vocabulary (<see cref="SetSwitch"/> +
/// <see cref="ShowMessage"/>), so no new outcome case. Implements <see cref="IMapEvent"/> (D-0024); materialised
/// from a <c>Lever</c> placement by the <see cref="BehaviourRegistry"/>. Named <c>SwitchEvent</c> to avoid a clash
/// with the #13 tracer <c>Tracer.LeverEvent</c>.
/// </summary>
public sealed class SwitchEvent : IMapEvent
{
    private readonly string _switch;
    private readonly string _message;

    /// <summary>Create the lever at <paramref name="cell"/>, fired by <paramref name="trigger"/>, that sets <paramref name="switchName"/> (showing <paramref name="message"/>) the first time it fires.</summary>
    public SwitchEvent(GridPoint cell, EventTrigger trigger, string switchName, string message)
    {
        ArgumentNullException.ThrowIfNull(switchName);
        ArgumentNullException.ThrowIfNull(message);
        Cell = cell;
        Trigger = trigger;
        _switch = switchName;
        _message = message;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context)
    {
        if (context.GetSwitch(_switch))
        {
            return [];
        }

        return [new SetSwitch(_switch, true), new ShowMessage(_message)];
    }
}
