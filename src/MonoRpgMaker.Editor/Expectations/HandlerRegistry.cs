using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.Sim.Tracer;

namespace MonoRpgMaker.Editor.Expectations;

/// <summary>
/// Maps a module name (a <c>.expect</c> file name, sans extension) to a factory for its handler.
/// An explicit table — no reflection — so the oracle is deterministic and the set of checkable
/// modules is reviewable. The handlers take a map cell their <c>Run</c> ignores, so a default
/// (<see cref="GridPoint.Zero"/>) is used.
/// </summary>
public static class HandlerRegistry
{
    private static readonly Dictionary<string, Func<IMapEvent>> Factories =
        new(StringComparer.Ordinal)
        {
            ["LeverEvent"] = static () => new LeverEvent(GridPoint.Zero),
            ["ChestEvent"] = static () => new ChestEvent(GridPoint.Zero),
            ["ShowText"] = static () => new ShowTextEvent(GridPoint.Zero, EventTrigger.ActionButton, "Welcome, traveller!"),
            ["Warp"] = static () => new WarpEvent(GridPoint.Zero, EventTrigger.StepOn, "town", new GridPoint(3, 4)),
            ["GiveItem"] = static () => new GiveItemEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 1, "You found a potion!"),
            ["Chest"] = static () => new ContainerEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 1, "chest_opened", "You open the chest."),
            ["Lever"] = static () => new SwitchEvent(GridPoint.Zero, EventTrigger.ActionButton, "door_open", "You pull the lever."),
            ["Shop"] = static () => new ShopEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion:5:2"),
        };

    /// <summary>Get the handler factory for <paramref name="module"/>; false if none is registered.</summary>
    public static bool TryGet(string module, [NotNullWhen(true)] out Func<IMapEvent>? factory)
    {
        ArgumentNullException.ThrowIfNull(module);
        return Factories.TryGetValue(module, out factory);
    }
}
