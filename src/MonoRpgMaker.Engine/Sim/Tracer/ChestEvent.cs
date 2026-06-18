using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim.Tracer;

/// <summary>
/// The hand-authored M0 chest event: the first time the player steps onto the chest it grants one
/// potion (an <see cref="AddCounter"/> outcome), narrates the take, and latches the
/// <see cref="OpenedSwitch"/>; every later step <em>returns</em> only the "empty" message. The
/// give-once variant of <see cref="LeverEvent"/> — the same "read a flag, RETURN effects" target
/// shape the scaffolder will later emit.
/// </summary>
public sealed class ChestEvent : IMapEvent
{
    /// <summary>The game-state switch latched true once the chest has been opened.</summary>
    public const string OpenedSwitch = "chest_opened";

    /// <summary>The game-state counter the granted potion is added to.</summary>
    public const string PotionCount = "potions";

    /// <summary>Create the chest event at <paramref name="cell"/>.</summary>
    public ChestEvent(GridPoint cell) => Cell = cell;

    /// <inheritdoc />
    public GridPoint Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger => EventTrigger.StepOn;

    /// <inheritdoc />
    public IReadOnlyList<Outcome> Run(IEventContext context)
    {
        if (context.GetSwitch(OpenedSwitch))
        {
            return [new ShowMessage("The chest is empty.")];
        }

        return
        [
            new AddCounter(PotionCount, 1),
            new SetSwitch(OpenedSwitch, true),
            new ShowMessage("You open the chest and take a potion."),
        ];
    }
}
