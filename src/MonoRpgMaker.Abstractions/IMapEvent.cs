namespace MonoRpgMaker.Abstractions;

/// <summary>A scripted interaction placed on a single map cell — the published seam an
/// agent-authored event implements.</summary>
public interface IMapEvent
{
    /// <summary>The cell this event occupies.</summary>
    GridPoint Cell { get; }

    /// <summary>What activates the event.</summary>
    EventTrigger Trigger { get; }

    /// <summary>Run the event's behaviour against <paramref name="context"/>.</summary>
    void Run(IEventContext context);
}
