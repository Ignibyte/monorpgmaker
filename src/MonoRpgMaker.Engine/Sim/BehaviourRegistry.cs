using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The single binding point (D-0024) between a placed event's <c>kind</c> (data) and its behaviour (an
/// <see cref="IMapEvent"/>). Built-in kinds register here; future agent-authored kinds register the SAME way —
/// the runtime materialises every placement through this one registry, so built-in and custom behaviours are
/// interchangeable. The factory table is read via <c>TryGetValue</c> only (never enumerated), keeping it
/// determinism-analyzer clean.
/// </summary>
public static class BehaviourRegistry
{
    // The SINGLE source of truth for every behaviour kind: its name, its parameter keys, and its factory. The
    // editor reads <see cref="Kinds"/> (name + param keys); the runtime reads <see cref="Factories"/> — both are
    // derived from this one table, so the editor's options and the runtime's materialisation can never drift
    // (D-0024). A new built-in (or a future agent kind) is one entry here.
    private static readonly Registration[] Registrations =
    [
        new(
            "ShowText",
            ["text"],
            static (cell, trigger, p) =>
                p.TryGetValue("text", out string? text)
                    ? BehaviourResult.Success(new ShowTextEvent(cell, trigger, text))
                    : BehaviourResult.Failure("ShowText requires a 'text' parameter")),
        new(
            "Warp",
            ["map", "x", "y"],
            static (cell, trigger, p) =>
                p.TryGetValue("map", out string? map)
                && p.TryGetValue("x", out string? sx) && int.TryParse(sx, out int x)
                && p.TryGetValue("y", out string? sy) && int.TryParse(sy, out int y)
                    ? BehaviourResult.Success(new WarpEvent(cell, trigger, map, new GridPoint(x, y)))
                    : BehaviourResult.Failure("Warp requires a 'map' + integer 'x' + integer 'y'")),
    ];

    private static readonly Dictionary<string, Func<GridPoint, EventTrigger, IReadOnlyDictionary<string, string>, BehaviourResult>> Factories = BuildFactories();

    private static readonly IReadOnlyDictionary<string, string> EmptyParams = new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// The registered behaviour kinds and their parameter keys — the single source an editor reads to offer the
    /// kind list + its param fields, guaranteed to match what <see cref="TryMaterialize"/> accepts (D-0024).
    /// </summary>
    public static IReadOnlyList<BehaviourKindInfo> Kinds { get; } = BuildKinds();

    private static Dictionary<string, Func<GridPoint, EventTrigger, IReadOnlyDictionary<string, string>, BehaviourResult>> BuildFactories()
    {
        var factories = new Dictionary<string, Func<GridPoint, EventTrigger, IReadOnlyDictionary<string, string>, BehaviourResult>>(StringComparer.Ordinal);
        foreach (Registration registration in Registrations)
        {
            factories[registration.Name] = registration.Factory;
        }

        return factories;
    }

    private static List<BehaviourKindInfo> BuildKinds()
    {
        var kinds = new List<BehaviourKindInfo>(Registrations.Length);
        foreach (Registration registration in Registrations)
        {
            kinds.Add(new BehaviourKindInfo(registration.Name, registration.ParamKeys));
        }

        return kinds;
    }

    private sealed record Registration(
        string Name,
        IReadOnlyList<string> ParamKeys,
        Func<GridPoint, EventTrigger, IReadOnlyDictionary<string, string>, BehaviourResult> Factory);

    /// <summary>
    /// Materialise a placed <paramref name="placement"/> into its <see cref="IMapEvent"/> via its <c>kind</c>.
    /// Returns a typed failure for an unknown trigger, an unknown kind, or missing/invalid params — never throws
    /// on bad data (only a null argument, a programmer error, throws).
    /// </summary>
    public static BehaviourResult TryMaterialize(EventData placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        if (!Enum.TryParse(placement.Trigger, out EventTrigger trigger) || !Enum.IsDefined(trigger))
        {
            return BehaviourResult.Failure("unknown trigger '" + placement.Trigger + "'");
        }

        if (!Factories.TryGetValue(placement.Kind, out var factory))
        {
            return BehaviourResult.Failure("unknown behaviour kind '" + placement.Kind + "'");
        }

        IReadOnlyDictionary<string, string> p = placement.Params ?? EmptyParams;
        return factory(new GridPoint(placement.X, placement.Y), trigger, p);
    }
}

/// <summary>
/// The result of <see cref="BehaviourRegistry.TryMaterialize"/>: either a materialised <see cref="Event"/> or a
/// single <see cref="Error"/> reason. A total result — never throws on a malformed placement.
/// </summary>
public sealed class BehaviourResult
{
    private BehaviourResult(IMapEvent? @event, string? error)
    {
        Event = @event;
        Error = error;
    }

    /// <summary>The materialised event when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public IMapEvent? Event { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether materialisation succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful materialisation carrying <paramref name="event"/>.</summary>
    public static BehaviourResult Success(IMapEvent @event) => new(@event, null);

    /// <summary>A failed materialisation carrying the <paramref name="error"/> reason.</summary>
    public static BehaviourResult Failure(string error) => new(null, error);
}

/// <summary>
/// A behaviour kind an editor can offer: its registry <see cref="Name"/> and the parameter keys it expects
/// (e.g. <c>ShowText</c> → <c>["text"]</c>). Exposed by <see cref="BehaviourRegistry.Kinds"/> as the single
/// source the editor and the runtime registry share, so a placement authored in the editor always materialises
/// (D-0024).
/// </summary>
/// <param name="Name">The behaviour kind's registry key.</param>
/// <param name="ParamKeys">The parameter keys this kind reads (the fields an editor should offer).</param>
public sealed record BehaviourKindInfo(string Name, IReadOnlyList<string> ParamKeys);
