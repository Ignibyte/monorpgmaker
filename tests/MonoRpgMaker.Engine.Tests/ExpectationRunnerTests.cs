using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Editor.Expectations;
using MonoRpgMaker.Engine.Sim.Tracer;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class ExpectationRunnerTests
{
    private static Func<IMapEvent> Lever => static () => new LeverEvent(GridPoint.Zero);

    private static ExpectationRow Row(params Outcome[] expected) =>
        new(1, new Dictionary<string, bool>(), new Dictionary<string, int>(), expected);

    private static readonly Outcome[] LeverFirstPull =
    {
        new ShowMessage("You pull the lever. The door grinds open."),
        new SetSwitch("door_open", true),
    };

    [Fact] // a row the handler reproduces exactly → no mismatch
    public void Check_ExactMatch_NoMismatch()
    {
        Assert.Empty(ExpectationRunner.Check("LeverEvent", new[] { Row(LeverFirstPull) }, Lever));
    }

    [Fact] // COUNT mismatch (expected fewer than the handler returns)
    public void Check_CountMismatch_ReportsMismatch()
    {
        Mismatch m = Assert.Single(ExpectationRunner.Check(
            "LeverEvent",
            new[] { Row(new ShowMessage("You pull the lever. The door grinds open.")) },
            Lever));

        Assert.Equal("LeverEvent", m.Module);
        Assert.Equal(1, m.Row.Line);
        Assert.Equal(2, m.Actual.Count);
    }

    [Fact] // ORDER mismatch (right outcomes, swapped)
    public void Check_OrderMismatch_ReportsMismatch()
    {
        Assert.Single(ExpectationRunner.Check(
            "LeverEvent",
            new[] { Row(new SetSwitch("door_open", true), new ShowMessage("You pull the lever. The door grinds open.")) },
            Lever));
    }

    [Fact] // PAYLOAD mismatch (wrong message text)
    public void Check_PayloadMismatch_ReportsMismatch()
    {
        Assert.Single(ExpectationRunner.Check(
            "LeverEvent",
            new[] { Row(new ShowMessage("wrong"), new SetSwitch("door_open", true)) },
            Lever));
    }

    [Fact] // KIND mismatch (wrong outcome type)
    public void Check_KindMismatch_ReportsMismatch()
    {
        Assert.Single(ExpectationRunner.Check(
            "LeverEvent",
            new[] { Row(new SetSwitch("door_open", true), new SetSwitch("door_open", true)) },
            Lever));
    }

    [Fact] // the runner SEEDS switches — door_open=true makes the lever give-once (returns [])
    public void Check_SeedsSwitches()
    {
        var seeded = new ExpectationRow(
            1,
            new Dictionary<string, bool> { ["door_open"] = true },
            new Dictionary<string, int>(),
            Array.Empty<Outcome>());

        Assert.Empty(ExpectationRunner.Check("LeverEvent", new[] { seeded }, Lever));
    }

    [Fact] // the runner SEEDS counters — a handler that reads one observes the seeded value
    public void Check_SeedsCounters()
    {
        var seeded = new ExpectationRow(
            1,
            new Dictionary<string, bool>(),
            new Dictionary<string, int> { ["potions"] = 7 },
            new Outcome[] { new AddCounter("seen", 7) });
        Assert.Empty(ExpectationRunner.Check("CounterEcho", new[] { seeded }, static () => new CounterEcho()));

        var unseeded = new ExpectationRow(
            1,
            new Dictionary<string, bool>(),
            new Dictionary<string, int>(),
            new Outcome[] { new AddCounter("seen", 0) });
        Assert.Empty(ExpectationRunner.Check("CounterEcho", new[] { unseeded }, static () => new CounterEcho()));
    }

    // A test-only handler that echoes a seeded counter, to prove the runner seeds counters.
    private sealed class CounterEcho : IMapEvent
    {
        public GridPoint Cell => GridPoint.Zero;

        public EventTrigger Trigger => EventTrigger.StepOn;

        public IReadOnlyList<Outcome> Run(IEventContext context) =>
            new Outcome[] { new AddCounter("seen", context.GetCounter("potions")) };
    }
}

public class HandlerRegistryTests
{
    [Fact]
    public void TryGet_ResolvesKnownModules()
    {
        Assert.True(HandlerRegistry.TryGet("LeverEvent", out Func<IMapEvent>? lever));
        Assert.IsType<LeverEvent>(lever!());

        Assert.True(HandlerRegistry.TryGet("ChestEvent", out Func<IMapEvent>? chest));
        Assert.IsType<ChestEvent>(chest!());
    }

    [Fact]
    public void TryGet_UnknownModule_IsFalse()
    {
        Assert.False(HandlerRegistry.TryGet("Nope", out Func<IMapEvent>? factory));
        Assert.Null(factory);
    }
}

public class OutcomeFormatTests
{
    [Fact]
    public void Format_EmptyList_IsNone() => Assert.Equal("(none)", OutcomeFormat.Format(Array.Empty<Outcome>()));

    [Fact]
    public void Format_EachKind_AndSeparator()
    {
        Assert.Equal("SetSwitch(k, true)", OutcomeFormat.Format(new Outcome[] { new SetSwitch("k", true) }));
        Assert.Equal("SetSwitch(k, false)", OutcomeFormat.Format(new Outcome[] { new SetSwitch("k", false) }));
        Assert.Equal("AddCounter(p, 3)", OutcomeFormat.Format(new Outcome[] { new AddCounter("p", 3) }));
        Assert.Equal("ShowMessage(\"hi\")", OutcomeFormat.Format(new Outcome[] { new ShowMessage("hi") }));
        Assert.Equal(
            "SetSwitch(a, false); AddCounter(b, 2)",
            OutcomeFormat.Format(new Outcome[] { new SetSwitch("a", false), new AddCounter("b", 2) }));
    }

    [Fact]
    public void FormatInput_EmptyIsInitial_ElseKeyValues()
    {
        Assert.Equal(
            "(initial)",
            OutcomeFormat.FormatInput(new ExpectationRow(1, new Dictionary<string, bool>(), new Dictionary<string, int>(), Array.Empty<Outcome>())));

        var row = new ExpectationRow(
            1,
            new Dictionary<string, bool> { ["f"] = true },
            new Dictionary<string, int> { ["c"] = 5 },
            Array.Empty<Outcome>());
        Assert.Equal("f=true c=5", OutcomeFormat.FormatInput(row));
    }
}
