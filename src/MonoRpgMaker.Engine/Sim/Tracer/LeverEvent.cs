using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Tracer;

/// <summary>
/// The hand-authored M0 exemplar event: stepping onto the lever shows a message once and opens the
/// door (by <em>returning</em> a <see cref="SetSwitch"/> outcome for the <see cref="DoorSwitch"/>).
/// This is the target shape the scaffolder will later emit — read a flag, RETURN effects; the door
/// reacts via its <see cref="DoorRule"/>.
/// </summary>
public sealed class LeverEvent : IMapEvent
{
    /// <summary>The game-state switch this lever sets.</summary>
    public const string DoorSwitch = "door_open";

    /// <summary>Create the lever event at <paramref name="cell"/>.</summary>
    public LeverEvent(GridPoint cell) => Cell = cell;

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger => EventTrigger.StepOn;

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context)
    {
        if (context.GetSwitch(DoorSwitch))
        {
            return [];
        }

        return [new ShowMessage("You pull the lever. The door grinds open."), new SetSwitch(DoorSwitch, true)];
    }
}
