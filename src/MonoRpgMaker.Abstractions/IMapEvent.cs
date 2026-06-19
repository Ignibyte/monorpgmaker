using System.Collections.Generic;

namespace MonoRpgMaker.Abstractions;

/// <summary>A scripted interaction placed on a single map cell — the published seam an
/// agent-authored event implements.</summary>
public interface IMapEvent
{
    /// <summary>The cell this event occupies.</summary>
    GridPoint Cell { get; }

    /// <summary>What activates the event.</summary>
    EventTrigger Trigger { get; }

    /// <summary>
    /// Relative run order within a (cell, trigger) group; lower runs first. Two events that share a cell,
    /// trigger, and order are an ambiguity error at sim build (there is no implicit tie-break). Defaults to 0.
    /// </summary>
    int Order => 0;

    /// <summary>
    /// Run the event's behaviour: read state through <paramref name="context"/> and <em>return</em> the
    /// declarative <see cref="Outcome"/>s to apply, in order (D-0017). The event never mutates state
    /// directly; an empty list means "do nothing".
    /// </summary>
    IReadOnlyList<Outcome> Run(IEventContext context);
}
