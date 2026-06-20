using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The validated, ordered dispatch plan for a map's events. Within each (cell, trigger) group events run in
/// ascending <see cref="IMapEvent.Order"/>; two events that share a cell, trigger, and order are an
/// ambiguity error (no implicit tie-break) surfaced by <see cref="Build"/> as a typed
/// <see cref="ScheduleResult"/> — the dispatch order is total, so replay stays bit-identical.
/// </summary>
public sealed class HookSchedule
{
    private readonly IReadOnlyList<IMapEvent> _events;

    private HookSchedule(IReadOnlyList<IMapEvent> events) => _events = events;

    /// <summary>The schedule's events, in validated dispatch order.</summary>
    public IReadOnlyList<IMapEvent> Events => _events;

    /// <summary>
    /// Validate <paramref name="events"/> into a schedule: succeeds unless two events share the same cell,
    /// trigger, and order (ambiguous). Never throws on the validation path — a genuine null argument is a
    /// programmer error and still throws.
    /// </summary>
    public static ScheduleResult Build(IReadOnlyList<IMapEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        var sorted = new List<IMapEvent>(events);
        sorted.Sort(static (a, b) =>
        {
            int c = a.Cell.X.CompareTo(b.Cell.X);
            if (c != 0)
            {
                return c;
            }

            c = a.Cell.Y.CompareTo(b.Cell.Y);
            if (c != 0)
            {
                return c;
            }

            c = ((int)a.Trigger).CompareTo((int)b.Trigger);
            return c != 0 ? c : a.Order.CompareTo(b.Order);
        });

        for (var i = 1; i < sorted.Count; i++)
        {
            IMapEvent previous = sorted[i - 1];
            IMapEvent current = sorted[i];
            if (previous.Cell == current.Cell && previous.Trigger == current.Trigger && previous.Order == current.Order)
            {
                return ScheduleResult.Failure(
                    $"ambiguous: events at cell ({current.Cell.X},{current.Cell.Y}) trigger {current.Trigger} share order {current.Order}");
            }
        }

        return ScheduleResult.Success(new HookSchedule(new List<IMapEvent>(events)));
    }

    /// <summary>The events at <paramref name="cell"/> for <paramref name="trigger"/>, in ascending order.</summary>
    public IReadOnlyList<IMapEvent> EventsAt(GridPoint cell, EventTrigger trigger)
    {
        var matches = new List<IMapEvent>();
        foreach (IMapEvent mapEvent in _events)
        {
            if (mapEvent.Cell == cell && mapEvent.Trigger == trigger)
            {
                matches.Add(mapEvent);
            }
        }

        matches.Sort(static (a, b) => a.Order.CompareTo(b.Order));
        return matches;
    }
}

/// <summary>
/// The result of <see cref="HookSchedule.Build"/>: either a validated <see cref="Schedule"/> or a single
/// <see cref="Error"/> reason (an ambiguous event set). A total result — never throws on the validation path.
/// </summary>
public sealed class ScheduleResult
{
    private ScheduleResult(HookSchedule? schedule, string? error)
    {
        Schedule = schedule;
        Error = error;
    }

    /// <summary>The validated schedule when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public HookSchedule? Schedule { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether validation succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful validation carrying <paramref name="schedule"/>.</summary>
    public static ScheduleResult Success(HookSchedule schedule) => new(schedule, null);

    /// <summary>A failed validation carrying the <paramref name="error"/> reason.</summary>
    public static ScheduleResult Failure(string error) => new(null, error);
}
