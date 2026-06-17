using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>A scripted interaction placed on a single map cell.</summary>
public interface IMapEvent
{
    /// <summary>The cell this event occupies.</summary>
    Point Cell { get; }

    /// <summary>What activates the event.</summary>
    EventTrigger Trigger { get; }

    /// <summary>Run the event's behaviour against <paramref name="context"/>.</summary>
    void Run(EventContext context);
}
