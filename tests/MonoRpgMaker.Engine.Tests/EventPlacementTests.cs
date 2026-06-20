using System;
using System.Collections.Generic;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>Tests for the map editor's event-placement model (the gated <see cref="MapPaintSession"/> event API).</summary>
public class MapPaintSessionEventTests
{
    private static MapPaintSession Session() => new(TilesetCatalog.DefaultName, 20, 15);

    [Fact] // REQ-001 — a fresh placement carries the defaults
    public void AddEvent_PlacesWithDefaults()
    {
        MapPaintSession s = Session();
        EventView placed = s.AddEvent(new Cell(2, 3));

        Assert.Equal(1, s.EventCount);
        Assert.Equal("event-1", placed.Id);
        Assert.Equal(new Cell(2, 3), placed.Cell);
        Assert.Equal("ShowText", placed.Kind);
        Assert.Equal("ActionButton", placed.Trigger);
    }

    [Fact] // REQ-001 — deterministic, monotonic ids
    public void AddEvent_SecondGetsEventTwo()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        EventView second = s.AddEvent(new Cell(5, 5));

        Assert.Equal("event-2", second.Id);
        Assert.Equal(2, s.EventCount);
    }

    [Fact] // REQ-001 — place-or-select: re-adding on an occupied cell selects it, never duplicates
    public void AddEvent_OnOccupiedCell_SelectsExisting()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.AddEvent(new Cell(5, 5));

        EventView reAdded = s.AddEvent(new Cell(2, 3));

        Assert.Equal(2, s.EventCount);       // no duplicate
        Assert.Equal("event-1", reAdded.Id); // the existing event
    }

    [Fact] // REQ-002 — selecting an empty cell clears the selection
    public void SelectEvent_EmptyCell_NoSelection()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));

        EventView? selected = s.SelectEvent(new Cell(9, 9));

        Assert.Null(selected);
        Assert.Null(s.SelectedEvent);
    }

    [Fact] // REQ-002 — selecting an occupied cell
    public void SelectEvent_OccupiedCell_Selects()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.SelectEvent(new Cell(9, 9)); // clear first

        EventView? selected = s.SelectEvent(new Cell(2, 3));

        Assert.NotNull(selected);
        Assert.Equal("event-1", selected!.Value.Id);
    }

    [Fact] // REQ-002 — update writes each field
    public void UpdateSelected_WritesFields()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));

        s.UpdateSelected("StepOn", "ShowText", new Dictionary<string, string> { ["text"] = "Hi" });

        EventView sel = s.SelectedEvent!.Value;
        Assert.Equal("StepOn", sel.Trigger);
        Assert.Equal("Hi", sel.Params["text"]);
    }

    [Fact] // REQ-002 — the stored params are a COPY, not an alias of the caller's dictionary
    public void UpdateSelected_CopiesParams_NotAlias()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        var caller = new Dictionary<string, string> { ["text"] = "original" };

        s.UpdateSelected("ActionButton", "ShowText", caller);
        caller["text"] = "mutated"; // mutate the source AFTER the call

        Assert.Equal("original", s.SelectedEvent!.Value.Params["text"]);
    }

    [Fact] // REQ-002 — update with no selection is a no-op (no throw, no state change)
    public void UpdateSelected_NoSelection_NoOp()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.SelectEvent(new Cell(9, 9)); // nothing selected now

        Exception? ex = Record.Exception(() =>
            s.UpdateSelected("StepOn", "ShowText", new Dictionary<string, string> { ["text"] = "x" }));

        Assert.Null(ex);
        Assert.Equal("ActionButton", s.SelectEvent(new Cell(2, 3))!.Value.Trigger); // untouched
    }

    [Theory] // REQ-002 — null-argument guards
    [InlineData(null, "ShowText")]
    [InlineData("ActionButton", null)]
    public void UpdateSelected_NullTriggerOrKind_Throws(string? trigger, string? kind)
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        Assert.Throws<ArgumentNullException>(() =>
            s.UpdateSelected(trigger!, kind!, new Dictionary<string, string>()));
    }

    [Fact] // REQ-002 — null params guard
    public void UpdateSelected_NullParams_Throws()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        Assert.Throws<ArgumentNullException>(() => s.UpdateSelected("ActionButton", "ShowText", null!));
    }

    [Fact] // REQ-002 — remove the selected event
    public void RemoveSelected()
    {
        MapPaintSession s = Session();
        Assert.False(s.RemoveSelected()); // nothing selected

        s.AddEvent(new Cell(2, 3));
        Assert.True(s.RemoveSelected());
        Assert.Equal(0, s.EventCount);
        Assert.Null(s.SelectedEvent);
    }

    [Fact] // REQ-002 — remove by cell
    public void RemoveEventAt()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));

        Assert.False(s.RemoveEventAt(new Cell(8, 8))); // absent
        Assert.True(s.RemoveEventAt(new Cell(2, 3)));  // present
        Assert.Equal(0, s.EventCount);
    }

    [Fact] // REQ-002 — RemoveEventAt clears the selection ONLY when it removed the selected event
    public void RemoveEventAt_ClearsSelectionOnlyForSelected()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3)); // event-1
        s.AddEvent(new Cell(5, 5)); // event-2

        s.SelectEvent(new Cell(5, 5));     // event-2 selected
        s.RemoveEventAt(new Cell(2, 3));   // remove a DIFFERENT event
        Assert.Equal("event-2", s.SelectedEvent!.Value.Id); // selection unchanged

        s.RemoveEventAt(new Cell(5, 5));   // remove the SELECTED event
        Assert.Null(s.SelectedEvent);
    }

    [Fact] // REQ-001 — EventCells lists the placed cells
    public void EventCells_ListsPlacements()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.AddEvent(new Cell(5, 5));

        Assert.Equal(2, s.EventCount);
        Assert.Contains(new Cell(2, 3), s.EventCells);
        Assert.Contains(new Cell(5, 5), s.EventCells);
    }

    [Fact] // REQ-003 — Save carries the events into the $data
    public void Save_IncludesEvents()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.UpdateSelected("ActionButton", "ShowText", new Dictionary<string, string> { ["text"] = "Hello" });

        MapLoadResult round = MapSerializer.Deserialize(s.Save());

        Assert.True(round.Ok);
        EventData ev = Assert.Single(round.Events);
        Assert.Equal("event-1", ev.Id);
        Assert.Equal("Hello", ev.Params["text"]);
    }

    [Fact] // REQ-003 — Save → Load round-trip on a fresh session
    public void SaveLoad_RoundTrip()
    {
        MapPaintSession source = Session();
        source.AddEvent(new Cell(2, 3));
        source.UpdateSelected("ActionButton", "ShowText", new Dictionary<string, string> { ["text"] = "Hello" });
        string json = source.Save();

        MapPaintSession loaded = Session();
        MapLoadResult result = loaded.Load(json);

        Assert.True(result.Ok);
        Assert.Equal(1, loaded.EventCount);
        Assert.Null(loaded.SelectedEvent); // load clears the selection
        EventView ev = loaded.SelectEvent(new Cell(2, 3))!.Value;
        Assert.Equal("event-1", ev.Id);
        Assert.Equal("ActionButton", ev.Trigger);
        Assert.Equal("ShowText", ev.Kind);
        Assert.Equal("Hello", ev.Params["text"]);
    }

    [Fact] // REQ-001 — Load advances the id counter to the MAX loaded id (not the last, not zero)
    public void Load_AdvancesIdCounter_ToMax()
    {
        // "event-5" is placed AFTER "event-2", so the last id is not the max
        var map = new TileMap(10, 10);
        EventData[] events =
        [
            new() { Id = "event-2", X = 1, Y = 1, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = "a" } },
            new() { Id = "event-5", X = 2, Y = 2, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = "b" } },
        ];

        MapPaintSession s = Session();
        s.Load(MapSerializer.Serialize(map, events));
        EventView next = s.AddEvent(new Cell(3, 3));

        Assert.Equal("event-6", next.Id); // max(2,5)+1 — not "event-3"
    }

    [Fact] // REQ-001 — a non-"event-" id is ignored by the counter (no throw; the next id is event-1)
    public void Load_NonMatchingId_CounterStartsAtOne()
    {
        var map = new TileMap(10, 10);
        EventData[] events =
        [
            new() { Id = "sign-a", X = 1, Y = 1, Trigger = "ActionButton", Kind = "ShowText", Params = { ["text"] = "a" } },
        ];

        MapPaintSession s = Session();
        Exception? ex = Record.Exception(() => s.Load(MapSerializer.Serialize(map, events)));

        Assert.Null(ex);
        Assert.Equal("event-1", s.AddEvent(new Cell(3, 3)).Id);
    }

    [Fact] // REQ-001 — NewMap clears events + resets the id counter
    public void NewMap_ClearsEvents()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.AddEvent(new Cell(5, 5));

        s.NewMap(10, 10);

        Assert.Equal(0, s.EventCount);
        Assert.Null(s.SelectedEvent);
        Assert.Equal("event-1", s.AddEvent(new Cell(1, 1)).Id); // counter reset
    }

    [Fact] // REQ-005 — the editor's output materialises into a real runtime event
    public void EditorOutput_IsRuntimeValid()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 3));
        s.UpdateSelected("ActionButton", "ShowText", new Dictionary<string, string> { ["text"] = "Hi" });

        MapLoadResult round = MapSerializer.Deserialize(s.Save());
        EventData ev = Assert.Single(round.Events);
        BehaviourResult materialised = BehaviourRegistry.TryMaterialize(ev);

        Assert.True(materialised.Ok);
        Assert.IsType<ShowTextEvent>(materialised.Event);
    }

    [Fact] // REQ-004 — the editor reads the SAME kind source the runtime materialises from
    public void AvailableKinds_IsTheRegistrySource()
    {
        Assert.Same(BehaviourRegistry.Kinds, MapPaintSession.AvailableKinds);
        BehaviourKindInfo kind = Assert.Single(MapPaintSession.AvailableKinds);
        Assert.Equal("ShowText", kind.Name);
        Assert.Equal("text", Assert.Single(kind.ParamKeys));
    }
}

/// <summary>The registry's kind descriptor — the single source the editor reads (the <c>BuildKinds</c> mapping).</summary>
public class BehaviourKindsTests
{
    [Fact] // the #18 suite asserts Factories (via TryMaterialize); this kills the BuildKinds map mutant
    public void Kinds_ShowText_WithTextParam()
    {
        BehaviourKindInfo kind = Assert.Single(BehaviourRegistry.Kinds);
        Assert.Equal("ShowText", kind.Name);
        Assert.Equal("text", Assert.Single(kind.ParamKeys));
    }
}
