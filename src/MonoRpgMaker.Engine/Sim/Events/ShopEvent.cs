using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Events;

/// <summary>
/// The built-in <c>Shop</c> behaviour: on its trigger it opens a buy/sell modal carrying its encoded offers
/// (<c>id:buy:sell,…</c>) — a single <see cref="OpenShop"/> outcome (D-0017) the sim-host renders. Implements
/// <see cref="IMapEvent"/> (D-0024); materialised from a <c>Shop</c> placement by the <c>BehaviourRegistry</c>.
/// </summary>
public sealed class ShopEvent : IMapEvent
{
    private readonly string _items;

    /// <summary>Create the shop at <paramref name="cell"/>, fired by <paramref name="trigger"/>, offering the encoded <paramref name="items"/> (<c>id:buy:sell,…</c>).</summary>
    public ShopEvent(GridPoint cell, EventTrigger trigger, string items)
    {
        ArgumentNullException.ThrowIfNull(items);
        Cell = cell;
        Trigger = trigger;
        _items = items;
    }

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger { get; }

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context) => [new OpenShop(_items)];
}
