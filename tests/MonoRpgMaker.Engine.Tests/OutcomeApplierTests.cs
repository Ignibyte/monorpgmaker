using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class OutcomeApplierTests
{
    private static (OutcomeApplier applier, GameState state, List<string> messages) Build()
    {
        var state = new GameState();
        var messages = new List<string>();
        return (new OutcomeApplier(state, messages.Add), state, messages);
    }

    [Fact] // T4 — SetSwitch writes through to state, honoring Value (re-homes the dropped write-through assert)
    public void Apply_SetSwitch_WritesThroughHonoringValue()
    {
        var (applier, state, _) = Build();

        applier.Apply([new SetSwitch("door", true)]);
        Assert.True(state.Get("door"));

        applier.Apply([new SetSwitch("door", false)]);
        Assert.False(state.Get("door"));   // the outcome's Value is applied, not a hardcoded true
    }

    [Fact] // T4 — AddCounter accumulates on state (kills an Add→Set overwrite mutant)
    public void Apply_AddCounter_AccumulatesOnState()
    {
        var (applier, state, _) = Build();

        applier.Apply([new AddCounter("potions", 2)]);
        Assert.Equal(2, state.GetCount("potions"));

        applier.Apply([new AddCounter("potions", 3)]);
        Assert.Equal(5, state.GetCount("potions"));
    }

    [Fact] // T4 — ShowMessage invokes the sink exactly once with the text
    public void Apply_ShowMessage_InvokesSinkOnceWithText()
    {
        var (applier, _, messages) = Build();

        applier.Apply([new ShowMessage("hello")]);

        Assert.Equal("hello", Assert.Single(messages));
    }

    [Fact] // T4 — a multi-outcome list applies every kind, in list order
    public void Apply_MultipleOutcomes_AppliesEveryKind()
    {
        var (applier, state, messages) = Build();

        applier.Apply(
        [
            new AddCounter("potions", 1),
            new SetSwitch("chest_opened", true),
            new ShowMessage("You open the chest and take a potion."),
        ]);

        Assert.Equal(1, state.GetCount("potions"));
        Assert.True(state.Get("chest_opened"));
        Assert.Equal("You open the chest and take a potion.", Assert.Single(messages));
    }

    [Fact] // T4 — application order is the list order (last write to a key wins; sink order preserved)
    public void Apply_AppliesInListOrder()
    {
        var (applier, state, messages) = Build();

        applier.Apply([new SetSwitch("k", true), new SetSwitch("k", false)]);
        Assert.False(state.Get("k"));   // last (false) wins → applied in order, not reversed

        applier.Apply([new ShowMessage("first"), new ShowMessage("second")]);
        Assert.Equal(2, messages.Count);
        Assert.Equal("first", messages[0]);
        Assert.Equal("second", messages[1]);
    }

    [Fact] // an empty outcome list is a no-op (no writes, no messages)
    public void Apply_EmptyList_IsNoOp()
    {
        var (applier, state, messages) = Build();

        applier.Apply([]);

        Assert.False(state.Get("door"));
        Assert.Empty(messages);
    }

    [Fact] // T7 — null guards (re-homes the dropped null-sink guard; kills ThrowIfNull-removal mutants)
    public void Ctor_And_Apply_NullArgs_Throw()
    {
        var state = new GameState();

        Assert.Throws<ArgumentNullException>(() => new OutcomeApplier(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new OutcomeApplier(state, null!));

        var applier = new OutcomeApplier(state, _ => { });
        Assert.Throws<ArgumentNullException>(() => applier.Apply(null!));
    }
}
