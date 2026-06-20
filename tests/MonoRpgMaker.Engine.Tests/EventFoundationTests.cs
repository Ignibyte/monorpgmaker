using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Editor.Expectations;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>Tests for the <c>$data</c> placed-event schema (round-trip + total deserialize).</summary>
public class MapSerializerEventTests
{
    private static TileMap SmallMap() => new(4, 4);

    private static EventData ShowTextEv(string id = "e1", int x = 1, int y = 1, string text = "Hi") =>
        new() { Id = id, X = x, Y = y, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = text } };

    [Fact] // events round-trip with every field intact
    public void RoundTrip_TwoEvents_PreservesFields()
    {
        string json = MapSerializer.Serialize(SmallMap(), [ShowTextEv("a", 1, 2, "One"), ShowTextEv("b", 3, 0, "Two")]);
        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.True(result.Ok);
        Assert.Equal(2, result.Events.Count);
        Assert.Equal("a", result.Events[0].Id);
        Assert.Equal(1, result.Events[0].X);
        Assert.Equal(2, result.Events[0].Y);
        Assert.Equal("ActionButton", result.Events[0].Trigger);
        Assert.Equal("ShowText", result.Events[0].Kind);
        Assert.Equal("One", result.Events[0].Params["text"]);
        Assert.Equal("Two", result.Events[1].Params["text"]);
    }

    [Fact] // a map with no events serialized via the no-arg overload loads with empty Events
    public void NoEvents_RoundTrips_Empty()
    {
        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(SmallMap()));
        Assert.True(result.Ok);
        Assert.Empty(result.Events);
    }

    [Fact] // an absent / null "events" key → Ok with empty Events (kills the `?? []` coalesce)
    public void EventsNull_IsTreatedAsEmpty()
    {
        const string json = "{\"width\":1,\"height\":1,\"tiles\":[{\"tilesetId\":0,\"blocking\":false}],\"events\":null}";
        MapLoadResult result = MapSerializer.Deserialize(json);
        Assert.True(result.Ok);
        Assert.Empty(result.Events);
    }

    [Theory] // total: every malformed placement → typed failure, never a throw
    [InlineData("{\"width\":1,\"height\":1,\"tiles\":[{\"tilesetId\":0,\"blocking\":false}],\"events\":[null]}", "event 0 is null")]
    public void Malformed_NullElement_FailsWithoutThrowing(string json, string expected)
    {
        MapLoadResult? result = null;
        Assert.Null(Record.Exception(() => result = MapSerializer.Deserialize(json)));
        Assert.False(result!.Ok);
        Assert.Contains(expected, result.Error!, StringComparison.Ordinal);
    }

    [Fact] // empty Id → typed failure (kills the Id term of the || guard)
    public void Malformed_EmptyId_Fails()
    {
        var ev = ShowTextEv();
        ev.Id = "";
        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(SmallMap(), [ev]));
        Assert.False(result.Ok);
        Assert.Contains("missing id, kind, or trigger", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // empty Kind → typed failure (kills the Kind term)
    public void Malformed_EmptyKind_Fails()
    {
        var ev = ShowTextEv();
        ev.Kind = "";
        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(SmallMap(), [ev]));
        Assert.False(result.Ok);
        Assert.Contains("missing id, kind, or trigger", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // empty Trigger → typed failure (kills the Trigger term)
    public void Malformed_EmptyTrigger_Fails()
    {
        var ev = ShowTextEv();
        ev.Trigger = "";
        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(SmallMap(), [ev]));
        Assert.False(result.Ok);
        Assert.Contains("missing id, kind, or trigger", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // an unknown trigger string → typed failure (kills the {StepOn, ActionButton} structural check)
    public void Malformed_UnknownTrigger_Fails()
    {
        var ev = ShowTextEv();
        ev.Trigger = "Foo";
        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(SmallMap(), [ev]));
        Assert.False(result.Ok);
        Assert.Contains("unknown trigger 'Foo'", result.Error!, StringComparison.Ordinal);
    }
}

/// <summary>Tests for the <see cref="BehaviourRegistry"/> — the single kind→behaviour binding point.</summary>
public class BehaviourRegistryTests
{
    private static EventData Placement(string kind = "ShowText", string trigger = "ActionButton", string? text = "Hi")
    {
        var ev = new EventData { Id = "e", X = 3, Y = 4, Trigger = trigger, Kind = kind };
        if (text is not null)
        {
            ev.Params["text"] = text;
        }

        return ev;
    }

    [Fact] // a valid ShowText placement materialises at the right cell + trigger
    public void Valid_ShowText_Materialises()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(Placement());
        Assert.True(result.Ok);
        Assert.Equal(new GridPoint(3, 4), result.Event!.Cell);
        Assert.Equal(EventTrigger.ActionButton, result.Event.Trigger);
        Assert.IsType<ShowTextEvent>(result.Event);
    }

    [Fact] // an unknown trigger NAME → typed failure (kills the Enum.TryParse name path)
    public void UnknownTriggerName_Fails()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(Placement(trigger: "Foo"));
        Assert.False(result.Ok);
        Assert.Null(result.Event);
        Assert.Contains("unknown trigger 'Foo'", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // a NUMERIC out-of-range trigger → typed failure (kills Enum.IsDefined; TryParse SUCCEEDS on "99")
    public void NumericOutOfRangeTrigger_Fails()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(Placement(trigger: "99"));
        Assert.False(result.Ok);
        Assert.Contains("unknown trigger '99'", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // an unknown behaviour kind → typed failure
    public void UnknownKind_Fails()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(Placement(kind: "Nope"));
        Assert.False(result.Ok);
        Assert.Contains("unknown behaviour kind 'Nope'", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // ShowText with no "text" param → typed failure
    public void ShowText_MissingText_Fails()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(Placement(text: null));
        Assert.False(result.Ok);
        Assert.Contains("ShowText requires a 'text' parameter", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // null Params → typed failure (missing text), never a throw (kills the `?? EmptyParams` coalesce)
    public void NullParams_FailsWithoutThrowing()
    {
        var ev = Placement(text: null);
        ev.Params = null!;
        BehaviourResult? result = null;
        Assert.Null(Record.Exception(() => result = BehaviourRegistry.TryMaterialize(ev)));
        Assert.False(result!.Ok);
        Assert.Contains("ShowText requires", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // BehaviourResult Success / Failure shape
    public void BehaviourResult_SuccessAndFailure()
    {
        IMapEvent ev = new ShowTextEvent(GridPoint.Zero, EventTrigger.ActionButton, "x");
        BehaviourResult ok = BehaviourResult.Success(ev);
        Assert.True(ok.Ok);
        Assert.Same(ev, ok.Event);
        Assert.Null(ok.Error);

        BehaviourResult bad = BehaviourResult.Failure("boom");
        Assert.False(bad.Ok);
        Assert.Null(bad.Event);
        Assert.Equal("boom", bad.Error);
    }
}

/// <summary>Tests for the built-in <see cref="ShowTextEvent"/> behaviour.</summary>
public class ShowTextEventTests
{
    private static IEventContext Context() => new EventContext(new GameState());

    [Fact] // Run returns exactly one ShowMessage with the configured text (parameterised)
    public void Run_ReturnsSingleShowMessage()
    {
        Outcome only = Assert.Single(new ShowTextEvent(new GridPoint(1, 2), EventTrigger.ActionButton, "Hi").Run(Context()));
        Assert.Equal(new ShowMessage("Hi"), only);
    }

    [Fact] // the text is forwarded, not a constant
    public void Run_ForwardsDifferentText()
    {
        Outcome only = Assert.Single(new ShowTextEvent(GridPoint.Zero, EventTrigger.ActionButton, "Bye").Run(Context()));
        Assert.Equal(new ShowMessage("Bye"), only);
    }

    [Fact] // cell + trigger are forwarded
    public void CellAndTrigger_AreForwarded()
    {
        var ev = new ShowTextEvent(new GridPoint(1, 2), EventTrigger.ActionButton, "x");
        Assert.Equal(new GridPoint(1, 2), ev.Cell);
        Assert.Equal(EventTrigger.ActionButton, ev.Trigger);
    }
}

/// <summary>Tests for the start map's placed events + the materialisation path.</summary>
public class StartMapEventTests
{
    [Fact] // Events() is the welcome sign placement
    public void Events_IsWelcomeSign()
    {
        EventData[] events = StartMap.Events();
        Assert.Equal(2, events.Length);

        EventData sign = Assert.Single(events, e => e.Id == "sign-welcome");
        Assert.Equal(6, sign.X);
        Assert.Equal(4, sign.Y);
        Assert.Equal("ActionButton", sign.Trigger);
        Assert.Equal("ShowText", sign.Kind);
        Assert.Equal("Welcome to monorpgmaker! Use the arrow keys to explore.", sign.Params["text"]);

        EventData warp = Assert.Single(events, e => e.Id == "to-town");
        Assert.Equal("Warp", warp.Kind);
        Assert.Equal("StepOn", warp.Trigger);
        Assert.Equal("town", warp.Params["map"]);
    }

    [Fact] // the happy path: LoadWorld materialises the events
    public void LoadWorld_MaterialisesEvents()
    {
        WorldSimResult result = StartMap.LoadWorld(MapSerializer.Serialize(StartMap.Build(), StartMap.Events()));
        Assert.True(result.Ok);
        Assert.Contains(new GridPoint(6, 4), result.Sim!.EventCells);
    }

    [Fact] // a placement that passes structural validation but FAILS materialisation → typed failure (kills the !Ok return)
    public void LoadWorld_MaterialisationFailure_IsTyped()
    {
        // Empty params passes the serializer's structural check (id/kind/trigger non-empty) but the registry's
        // ShowText factory rejects it (no "text") — the only way to reach CreateWorld's failure branch.
        var bad = new EventData { Id = "x", X = 1, Y = 1, Trigger = "ActionButton", Kind = "ShowText" };
        string json = MapSerializer.Serialize(new TileMap(5, 5), [bad]);

        WorldSimResult result = StartMap.LoadWorld(json);

        Assert.False(result.Ok);
        Assert.Null(result.Sim);
        Assert.Contains("ShowText requires", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // WorldSim.EventCells lists every placed event's cell
    public void EventCells_ListsAllPlacements()
    {
        var a = new EventData { Id = "a", X = 1, Y = 1, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = "A" } };
        var b = new EventData { Id = "b", X = 3, Y = 3, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = "B" } };
        string json = MapSerializer.Serialize(new TileMap(5, 5), [a, b]);

        WorldSimResult result = StartMap.LoadWorld(json);

        Assert.True(result.Ok);
        Assert.Equal(2, result.Sim!.EventCells.Count);
        Assert.Contains(new GridPoint(1, 1), result.Sim.EventCells);
        Assert.Contains(new GridPoint(3, 3), result.Sim.EventCells);
    }
}

/// <summary>The <see cref="HandlerRegistry"/> carries the ShowText oracle sample (gate:13 seam).</summary>
public class HandlerRegistryShowTextTests
{
    [Fact]
    public void ShowText_IsRegistered()
    {
        Assert.True(HandlerRegistry.TryGet("ShowText", out Func<IMapEvent>? factory));
        Assert.IsType<ShowTextEvent>(factory!());
    }
}
