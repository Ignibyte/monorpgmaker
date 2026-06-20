using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in dialogue behaviour: on its trigger it shows a configured line. Implements the published
/// <see cref="IMapEvent"/> seam (D-0024) and returns the existing <see cref="ShowMessage"/> outcome — no new
/// vocabulary, no direct mutation (D-0017). Materialised from a <c>ShowText</c> placement by the
/// <see cref="BehaviourRegistry"/> — the repeatable recipe: implement <see cref="IMapEvent"/>, register the
/// kind, declare its <c>.expect</c>.
/// </summary>
public sealed class ShowTextEvent : IMapEvent
{
    private readonly string _text;

    /// <summary>Create the dialogue event at <paramref name="cell"/>, fired by <paramref name="trigger"/>, showing <paramref name="text"/>.</summary>
    public ShowTextEvent(GridPoint cell, EventTrigger trigger, string text)
    {
        ArgumentNullException.ThrowIfNull(text);
        Cell = cell;
        Trigger = trigger;
        _text = text;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context) => [new ShowMessage(_text)];
}
