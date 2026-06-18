using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Tracer;

/// <summary>
/// The hand-authored M0 exemplar event: stepping onto the lever shows a message once
/// and opens the door (by setting the <see cref="DoorSwitch"/> switch). This is the
/// target shape the scaffolder will later emit — set a flag and narrate; the door
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
    public void Run(IEventContext context)
    {
        if (context.GetSwitch(DoorSwitch))
            return;

        context.ShowMessage("You pull the lever. The door grinds open.");
        context.SetSwitch(DoorSwitch, true);
    }
}
