using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Tracer;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class EventOutcomeTests
{
    private static IEventContext Context(GameState state) => new EventContext(state);

    [Fact] // T2 — the lever's first pull narrates then sets the door switch, in that order
    public void Lever_FirstRun_ReturnsMessageThenSetSwitch()
    {
        var outcomes = new LeverEvent(new GridPoint(0, 0)).Run(Context(new GameState()));

        Assert.Equal(
            new Outcome[]
            {
                new ShowMessage("You pull the lever. The door grinds open."),
                new SetSwitch(LeverEvent.DoorSwitch, true),
            },
            outcomes);
    }

    [Fact] // T2 — give-once: once the door is open the lever returns no outcomes
    public void Lever_DoorAlreadyOpen_ReturnsNothing()
    {
        var state = new GameState();
        state.Set(LeverEvent.DoorSwitch, true);

        var outcomes = new LeverEvent(new GridPoint(0, 0)).Run(Context(state));

        Assert.Empty(outcomes);
    }

    [Fact] // T3 — the chest's first open grants one potion, latches opened, then narrates, in that order
    public void Chest_FirstRun_GrantsPotion_Latches_AndNarrates()
    {
        var outcomes = new ChestEvent(new GridPoint(0, 0)).Run(Context(new GameState()));

        Assert.Equal(
            new Outcome[]
            {
                new AddCounter(ChestEvent.PotionCount, 1),
                new SetSwitch(ChestEvent.OpenedSwitch, true),
                new ShowMessage("You open the chest and take a potion."),
            },
            outcomes);
    }

    [Fact] // T3 — give-once: an opened chest returns only the "empty" message (no grant, no re-latch)
    public void Chest_AlreadyOpened_ReturnsEmptyMessageOnly()
    {
        var state = new GameState();
        state.Set(ChestEvent.OpenedSwitch, true);

        var outcomes = new ChestEvent(new GridPoint(0, 0)).Run(Context(state));

        Assert.Equal(new Outcome[] { new ShowMessage("The chest is empty.") }, outcomes);
    }
}
