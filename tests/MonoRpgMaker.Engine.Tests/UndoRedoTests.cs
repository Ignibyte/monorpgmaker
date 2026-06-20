using System.Collections.Generic;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Tests for the map editor's undo/redo command-history spine (#21): the gated <see cref="MapPaintSession"/>
/// undo/redo over paint strokes, the event ops, and the tileset switch.
/// </summary>
public class UndoRedoTests
{
    private static MapPaintSession Session() => new(TilesetCatalog.DefaultName, 5, 5);

    private static MapPaintSession Painted(int tilesetId, Cell cell)
    {
        MapPaintSession s = Session();
        s.SelectTile(tilesetId);
        s.BeginStroke();
        s.Paint(cell);
        s.EndStroke();
        return s;
    }

    [Fact] // T1 (REQ-001) — undo restores the cell to its original (empty) tile
    public void Undo_RestoresPaintedCell()
    {
        MapPaintSession s = Painted(4, new Cell(2, 2));
        Assert.True(s.CanUndo);

        Assert.True(s.Undo());
        Assert.True(s.TileAt(2, 2).IsEmpty);
    }

    [Fact] // T2 (REQ-002) — redo re-applies the undone paint
    public void Redo_ReappliesPaint()
    {
        MapPaintSession s = Painted(4, new Cell(2, 2));
        s.Undo();

        Assert.True(s.CanRedo);
        Assert.True(s.Redo());
        Assert.Equal(4, s.TileAt(2, 2).TilesetId);
    }

    [Fact] // T3 (REQ-003) — a new edit clears the redo stack
    public void NewEdit_ClearsRedo()
    {
        MapPaintSession s = Session();
        s.SelectTile(1);
        s.Paint(new Cell(0, 0));
        s.SelectTile(2);
        s.Paint(new Cell(1, 1));
        s.Undo();
        Assert.True(s.CanRedo);

        s.SelectTile(3);
        s.Paint(new Cell(2, 2));
        Assert.False(s.CanRedo);
    }

    [Fact] // T4a (REQ-004) — a stroke of three cells is one undo unit
    public void Stroke_ThreeCells_IsOneUndo()
    {
        MapPaintSession s = Session();
        s.SelectTile(4);
        s.BeginStroke();
        s.Paint(new Cell(0, 0));
        s.Paint(new Cell(1, 0));
        s.Paint(new Cell(2, 0));
        s.EndStroke();

        Assert.True(s.Undo());
        Assert.True(s.TileAt(0, 0).IsEmpty);
        Assert.True(s.TileAt(1, 0).IsEmpty);
        Assert.True(s.TileAt(2, 0).IsEmpty);
        Assert.False(s.CanUndo);
    }

    [Fact] // T4b (REQ-004) — a no-change stroke (and an empty stroke) push nothing
    public void NoChangeStroke_PushesNothing()
    {
        MapPaintSession s = Session();

        s.BeginStroke();
        s.EndStroke();
        Assert.False(s.CanUndo);

        s.SelectTile(-1); // Tile.Empty's id — painting an already-empty cell is a no-op change
        s.BeginStroke();
        s.Paint(new Cell(0, 0));
        s.EndStroke();
        Assert.False(s.CanUndo);
    }

    [Fact] // T-EDGE (the inspect regression) — a cell painted twice in one stroke undoes to the ORIGINAL, not the intermediate
    public void SameCellTwiceInStroke_Undo_RestoresOriginal()
    {
        MapPaintSession s = Session();
        Tile original = s.TileAt(2, 2);

        s.BeginStroke();
        s.SelectTile(5, false);
        s.Paint(new Cell(2, 2)); // original -> 5
        s.SelectTile(7, true);
        s.Paint(new Cell(2, 2)); // 5 -> 7
        s.EndStroke();
        Assert.Equal(new Tile(7, true), s.TileAt(2, 2));

        Assert.True(s.Undo());
        Assert.Equal(original, s.TileAt(2, 2));
    }

    [Fact] // T5a (REQ-005) — the tileset switch is undoable + redoable
    public void SelectTileset_UndoRedo()
    {
        MapPaintSession s = Session();
        Assert.Equal("lpc-mountains", s.ActiveTileset);
        Assert.True(s.SelectTileset("lpc-grass"));

        Assert.True(s.Undo());
        Assert.Equal("lpc-mountains", s.ActiveTileset);
        Assert.True(s.Redo());
        Assert.Equal("lpc-grass", s.ActiveTileset);
    }

    [Fact] // T5b (REQ-005) — AddEvent undoes (clearing selection) and redoes with the SAME id
    public void AddEvent_UndoRedo_SameId()
    {
        MapPaintSession s = Session();
        string id = s.AddEvent(new Cell(2, 2)).Id;
        Assert.Equal(1, s.EventCount);

        Assert.True(s.Undo());
        Assert.Equal(0, s.EventCount);
        Assert.Null(s.SelectedEvent);

        Assert.True(s.Redo());
        Assert.Equal(1, s.EventCount);
        Assert.Equal(id, s.SelectEvent(new Cell(2, 2))!.Value.Id);
    }

    [Fact] // T5c (REQ-005) — UpdateSelected undoes to the old params and redoes the new
    public void UpdateSelected_UndoRedo()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 2));
        s.UpdateSelected("ActionButton", "ShowText", new Dictionary<string, string> { ["text"] = "X" });
        Assert.Equal("X", s.SelectedEvent!.Value.Params["text"]);

        Assert.True(s.Undo());
        Assert.False(s.SelectEvent(new Cell(2, 2))!.Value.Params.ContainsKey("text"));

        Assert.True(s.Redo());
        Assert.Equal("X", s.SelectEvent(new Cell(2, 2))!.Value.Params["text"]);
    }

    [Fact] // T5c (REQ-004/005) — an UpdateSelected that changes nothing records nothing
    public void UpdateSelected_Unchanged_RecordsNothing()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(2, 2));
        var @params = new Dictionary<string, string> { ["text"] = "X" };
        s.UpdateSelected("StepOn", "ShowText", @params);
        s.UpdateSelected("StepOn", "ShowText", @params); // identical — no command

        Assert.True(s.Undo());  // undo the (single) update
        Assert.True(s.Undo());  // undo the add
        Assert.False(s.Undo()); // nothing more — the second update pushed nothing
    }

    [Fact] // T5d (REQ-005) — a removed event re-inserts at its original index on undo
    public void RemoveEvent_UndoRestoresOrder()
    {
        MapPaintSession s = Session();
        s.AddEvent(new Cell(0, 0));
        s.AddEvent(new Cell(1, 1));
        Assert.True(s.RemoveEventAt(new Cell(0, 0)));
        Assert.Equal(new Cell(1, 1), s.EventCells[0]);

        Assert.True(s.Undo());
        Assert.Equal(2, s.EventCount);
        Assert.Equal(new Cell(0, 0), s.EventCells[0]);
        Assert.Equal(new Cell(1, 1), s.EventCells[1]);

        Assert.True(s.Redo());
        Assert.Equal(1, s.EventCount);
        Assert.Equal(new Cell(1, 1), s.EventCells[0]);
    }

    [Fact] // T6 (REQ-006) — NewMap clears the history
    public void NewMap_ClearsHistory()
    {
        MapPaintSession s = Painted(4, new Cell(0, 0));
        s.Undo();
        Assert.True(s.CanRedo);

        s.NewMap(3, 3);
        Assert.False(s.CanUndo);
        Assert.False(s.CanRedo);
    }

    [Fact] // T6 (REQ-006) — a successful Load clears the history
    public void Load_ClearsHistory()
    {
        string json = new MapPaintSession(TilesetCatalog.DefaultName, 3, 3).Save();
        MapPaintSession s = Painted(4, new Cell(0, 0));
        Assert.True(s.CanUndo);

        MapLoadResult result = s.Load(json);
        Assert.True(result.Ok);
        Assert.False(s.CanUndo);
        Assert.False(s.CanRedo);
    }

    [Fact] // T7 (REQ-007) — undo/redo on an empty history are no-ops (never throw)
    public void EmptyHistory_NoOp()
    {
        MapPaintSession s = Session();
        Assert.False(s.CanUndo);
        Assert.False(s.CanRedo);
        Assert.False(s.Undo());
        Assert.False(s.Redo());
    }

    [Fact] // T8 (REQ-001/004) — a lone Paint (no BeginStroke) is its own one-cell undo unit
    public void LonePaint_AutoWrapsAsOneUndo()
    {
        MapPaintSession s = Session();
        s.SelectTile(3);
        Assert.True(s.Paint(new Cell(0, 0)));
        Assert.True(s.CanUndo);

        Assert.True(s.Undo());
        Assert.True(s.TileAt(0, 0).IsEmpty);
    }

    [Fact] // T9 — HistoryChanged fires on record, undo, redo, and NewMap's clear
    public void HistoryChanged_FiresOnEveryMutation()
    {
        MapPaintSession s = Session();
        var raised = 0;
        s.HistoryChanged += (_, _) => raised++;

        s.SelectTile(3);
        s.Paint(new Cell(0, 0)); // record
        Assert.Equal(1, raised);
        s.Undo();
        Assert.Equal(2, raised);
        s.Redo();
        Assert.Equal(3, raised);
        s.NewMap(3, 3); // clear
        Assert.Equal(4, raised);
    }
}
