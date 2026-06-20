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
    private static readonly Dictionary<string, Func<GridPoint, EventTrigger, IReadOnlyDictionary<string, string>, BehaviourResult>> Factories =
        new(StringComparer.Ordinal)
        {
            ["ShowText"] = static (cell, trigger, p) =>
                p.TryGetValue("text", out string? text)
                    ? BehaviourResult.Success(new ShowTextEvent(cell, trigger, text))
                    : BehaviourResult.Failure("ShowText requires a 'text' parameter"),
        };

    private static readonly IReadOnlyDictionary<string, string> EmptyParams = new Dictionary<string, string>(StringComparer.Ordinal);

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
