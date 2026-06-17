using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.Sim.Tracer;

/// <summary>
/// The hand-authored M0 chest event: the first time the player steps onto the chest
/// it grants one potion (a <see cref="GameState"/> counter), narrates the take, and
/// latches the <see cref="OpenedSwitch"/>; every later step finds it empty. The
/// give-once variant of <see cref="LeverEvent"/> — the same "check a flag, change
/// state, narrate" target shape the scaffolder will later emit.
/// </summary>
public sealed class ChestEvent : IMapEvent
{
    /// <summary>The game-state switch latched true once the chest has been opened.</summary>
    public const string OpenedSwitch = "chest_opened";

    /// <summary>The game-state counter the granted potion is added to.</summary>
    public const string PotionCount = "potions";

    /// <summary>Create the chest event at <paramref name="cell"/>.</summary>
    public ChestEvent(Point cell) => Cell = cell;

    /// <inheritdoc />
    public Point Cell { get; }

    /// <inheritdoc />
    public EventTrigger Trigger => EventTrigger.StepOn;

    /// <inheritdoc />
    public void Run(EventContext context)
    {
        if (context.State.Get(OpenedSwitch))
        {
            context.ShowMessage("The chest is empty.");
            return;
        }

        context.State.Add(PotionCount, 1);
        context.State.Set(OpenedSwitch, true);
        context.ShowMessage("You open the chest and take a potion.");
    }
}
