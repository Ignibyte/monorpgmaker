using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Editor;

/// <summary>A map grid cell coordinate (column <paramref name="X"/>, row <paramref name="Y"/>). Framework-neutral so a host needs no engine Point type.</summary>
/// <param name="X">The zero-based column.</param>
/// <param name="Y">The zero-based row.</param>
public readonly record struct Cell(int X, int Y);

/// <summary>
/// A neutral read snapshot of a placed event for an editor host: its <paramref name="Cell"/>, stable
/// <paramref name="Id"/>, <paramref name="Trigger"/>, behaviour <paramref name="Kind"/>, and the kind's
/// <paramref name="Params"/>. Framework-neutral (a <see cref="Cell"/>, never an engine <c>Point</c>) so an
/// Avalonia host binds it without pulling MonoGame into its compile graph.
/// </summary>
/// <param name="Cell">The map cell the event occupies.</param>
/// <param name="Id">The placement's stable id.</param>
/// <param name="Trigger">The trigger name (e.g. <c>ActionButton</c>).</param>
/// <param name="Kind">The behaviour kind (e.g. <c>ShowText</c>).</param>
/// <param name="Params">The kind's parameters (e.g. <c>text</c>).</param>
public readonly record struct EventView(Cell Cell, string Id, string Trigger, string Kind, IReadOnlyDictionary<string, string> Params);

/// <summary>
/// The Avalonia-free editing session behind the map painter: it holds the map under edit (via a
/// <see cref="MapEditor"/>), the active paint <see cref="Tile"/>, the active tileset name, and an undo/redo
/// <see cref="EditHistory"/>, and orchestrates new / paint / save / load over <see cref="MapSerializer"/>
/// strings. Pure logic — no UI and no file IO — so the Studio host drives it and it stays fully unit-testable.
/// </summary>
public sealed class MapPaintSession
{
    private readonly List<EventData> _events = new();
    private readonly EditHistory _history = new();
    private MapEditor _editor;
    private Tile _active = new(0, false);
    private EventData? _selected;
    private int _eventSeq;
    private List<TileChange>? _openStroke;

    /// <summary>
    /// Start a session over a fresh <paramref name="width"/>×<paramref name="height"/> map that paints tiles
    /// from the catalog tileset named <paramref name="tilesetName"/> (an unknown name falls back to the catalog
    /// default).
    /// </summary>
    public MapPaintSession(string tilesetName, int width, int height)
    {
        _editor = new MapEditor(new TileMap(width, height));
        _editor.Map.TilesetName = TilesetCatalog.Contains(tilesetName) ? tilesetName : TilesetCatalog.DefaultName;
    }

    /// <summary>
    /// The map currently under edit. Engine-internal — Avalonia hosts should read tiles via
    /// <see cref="Width"/>/<see cref="Height"/>/<see cref="TileAt"/>; this exposes the engine
    /// <c>TileMap</c> (whose members take a MonoGame <c>Point</c>), so binding it from a host would pull
    /// MonoGame into the host's compile graph.
    /// </summary>
    public TileMap Map => _editor.Map;

    /// <summary>The tile that <see cref="Paint"/> writes.</summary>
    public Tile Active => _active;

    /// <summary>The catalog name of the active tileset — the sheet <see cref="Save"/> writes into the map's <c>$data</c>.</summary>
    public string ActiveTileset => _editor.Map.TilesetName;

    /// <summary>The tilesets a map may use — the single source shared with the runtime (the host's picker reads this).</summary>
    public static IReadOnlyList<TilesetInfo> AvailableTilesets => TilesetCatalog.All;

    /// <summary>The map width in tiles.</summary>
    public int Width => _editor.Map.Width;

    /// <summary>The map height in tiles.</summary>
    public int Height => _editor.Map.Height;

    /// <summary>Whether there is an edit to undo.</summary>
    public bool CanUndo => _history.CanUndo;

    /// <summary>Whether there is an undone edit to redo.</summary>
    public bool CanRedo => _history.CanRedo;

    /// <summary>Raised after any change to the undo/redo history, so a host can refresh button enablement + repaint.</summary>
    public event EventHandler? HistoryChanged
    {
        add => _history.Changed += value;
        remove => _history.Changed -= value;
    }

    /// <summary>The tile at column <paramref name="x"/>, row <paramref name="y"/>.</summary>
    public Tile TileAt(int x, int y) => _editor.Map.GetTile(new Point(x, y));

    /// <summary>
    /// Map a pointer pixel to its grid cell at <paramref name="cellSize"/> pixels per tile, or
    /// <see langword="null"/> when the pixel falls outside the map.
    /// </summary>
    public Cell? CellAt(double pixelX, double pixelY, int cellSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cellSize);

        if (pixelX < 0 || pixelY < 0)
        {
            return null;
        }

        var col = (int)(pixelX / cellSize);
        var row = (int)(pixelY / cellSize);
        if (col >= Width || row >= Height)
        {
            return null;
        }

        return new Cell(col, row);
    }

    /// <summary>Set the active paint tile to <paramref name="tilesetId"/> (optionally <paramref name="blocking"/>).</summary>
    public void SelectTile(int tilesetId, bool blocking = false) => _active = new Tile(tilesetId, blocking);

    /// <summary>
    /// Begin a paint stroke: subsequent <see cref="Paint"/> calls coalesce into ONE undo unit until
    /// <see cref="EndStroke"/>. Defensively commits any already-open stroke first.
    /// </summary>
    public void BeginStroke()
    {
        EndStroke();
        _openStroke = new List<TileChange>();
    }

    /// <summary>End the current paint stroke, recording it as a single undo unit (nothing is recorded for a stroke that changed no cell).</summary>
    public void EndStroke()
    {
        if (_openStroke is null)
        {
            return;
        }

        List<TileChange> changes = _openStroke;
        _openStroke = null;
        if (changes.Count > 0)
        {
            _history.Record(TileCommand(changes));
        }
    }

    /// <summary>Paint the active tile at <paramref name="cell"/>; returns <see langword="false"/> (a no-op) when off-map. A real change is recorded for undo (coalesced into the open stroke, or its own one-cell unit).</summary>
    public bool Paint(Cell cell)
    {
        var point = new Point(cell.X, cell.Y);
        if (!_editor.Map.InBounds(point))
        {
            return false;
        }

        Tile before = _editor.Map.GetTile(point);
        _editor.Paint(point, _active);
        if (!before.Equals(_active))
        {
            var change = new TileChange(cell, before, _active);
            if (_openStroke is not null)
            {
                _openStroke.Add(change);
            }
            else
            {
                _history.Record(TileCommand(new List<TileChange> { change }));
            }
        }

        return true;
    }

    /// <summary>
    /// Switch the active tileset to the catalog member <paramref name="name"/>; returns <see langword="false"/>
    /// (leaving the current tileset unchanged) when the name is not in the catalog. Painted indices are kept and
    /// re-interpreted against the new sheet — an index past its tile count simply renders empty.
    /// </summary>
    public bool SelectTileset(string name)
    {
        if (!TilesetCatalog.Contains(name))
        {
            return false;
        }

        string previous = _editor.Map.TilesetName;
        if (string.Equals(previous, name, StringComparison.Ordinal))
        {
            return true;
        }

        _editor.Map.TilesetName = name;
        _history.Record(new EditCommand(
            () => _editor.Map.TilesetName = name,
            () => _editor.Map.TilesetName = previous));
        return true;
    }

    /// <summary>Undo the most recent edit (clears the selection — selection is view state); returns <see langword="false"/> when there is nothing to undo.</summary>
    public bool Undo()
    {
        if (!_history.Undo())
        {
            return false;
        }

        _selected = null;
        return true;
    }

    /// <summary>Redo the most recently undone edit (clears the selection); returns <see langword="false"/> when there is nothing to redo.</summary>
    public bool Redo()
    {
        if (!_history.Redo())
        {
            return false;
        }

        _selected = null;
        return true;
    }

    /// <summary>Replace the map under edit with a fresh, all-empty <paramref name="width"/>×<paramref name="height"/> map, keeping the active tileset (clears placed events + the undo history).</summary>
    public void NewMap(int width, int height)
    {
        string tileset = _editor.Map.TilesetName;
        _editor = new MapEditor(new TileMap(width, height));
        _editor.Map.TilesetName = tileset;
        _events.Clear();
        _selected = null;
        _eventSeq = 0;
        _openStroke = null;
        _history.Clear();
    }

    /// <summary>Serialize the current map and its placed events to a <c>$data</c> JSON string.</summary>
    public string Save() => MapSerializer.Serialize(Map, _events);

    /// <summary>
    /// Parse a <c>$data</c> JSON <paramref name="json"/> string; on success swap the map under edit to the
    /// loaded one (and clear the undo history — no undo across a load), otherwise leave the current map
    /// untouched. Returns the typed <see cref="MapLoadResult"/> — never throws on malformed input.
    /// </summary>
    public MapLoadResult Load(string json)
    {
        MapLoadResult result = MapSerializer.Deserialize(json);
        if (result.Ok)
        {
            _editor = new MapEditor(result.Map!);
            _events.Clear();
            _events.AddRange(result.Events);
            _selected = null;
            _eventSeq = MaxEventSeq(_events);
            _openStroke = null;
            _history.Clear();
        }

        return result;
    }

    /// <summary>The behaviour kinds an event may use + their param keys — the single source shared with the runtime registry.</summary>
    public static IReadOnlyList<BehaviourKindInfo> AvailableKinds => BehaviourRegistry.Kinds;

    /// <summary>The cells that carry a placed event (for the host to mark).</summary>
    public IReadOnlyList<Cell> EventCells => _events.Select(e => new Cell(e.X, e.Y)).ToList();

    /// <summary>How many events are placed on the current map.</summary>
    public int EventCount => _events.Count;

    /// <summary>The currently selected event as a neutral snapshot, or <see langword="null"/> when none is selected.</summary>
    public EventView? SelectedEvent => _selected is null ? null : View(_selected);

    /// <summary>
    /// Place a new event at <paramref name="cell"/> (default kind <c>ShowText</c>, an action-button trigger, and a
    /// deterministic auto-id) and select it; if an event already occupies the cell, select that one instead (one
    /// event per cell). Returns the selected event. A newly placed event is recorded for undo (the place-or-select
    /// case is selection only, so it records nothing).
    /// </summary>
    public EventView AddEvent(Cell cell)
    {
        EventData? existing = _events.FirstOrDefault(e => e.X == cell.X && e.Y == cell.Y);
        if (existing is not null)
        {
            _selected = existing;
            return View(existing);
        }

        var placed = new EventData
        {
            Id = $"event-{++_eventSeq}",
            X = cell.X,
            Y = cell.Y,
            Trigger = "ActionButton",
            Kind = "ShowText",
        };
        _events.Add(placed);
        _selected = placed;
        _history.Record(new EditCommand(
            () => _events.Add(placed),
            () => _events.Remove(placed)));
        return View(placed);
    }

    /// <summary>Select the event at <paramref name="cell"/> (or clear the selection when the cell has none); returns the selection. Selection is view state — not undoable.</summary>
    public EventView? SelectEvent(Cell cell)
    {
        _selected = _events.FirstOrDefault(e => e.X == cell.X && e.Y == cell.Y);
        return SelectedEvent;
    }

    /// <summary>Update the selected event's <paramref name="trigger"/>, <paramref name="kind"/>, and <paramref name="params"/>; a no-op when nothing is selected or nothing changed. A real change is recorded for undo.</summary>
    public void UpdateSelected(string trigger, string kind, IReadOnlyDictionary<string, string> @params)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(@params);

        if (_selected is null)
        {
            return;
        }

        EventData target = _selected;
        string oldTrigger = target.Trigger;
        string oldKind = target.Kind;
        Dictionary<string, string> oldParams = target.Params;
        var newParams = @params.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

        if (string.Equals(oldTrigger, trigger, StringComparison.Ordinal)
            && string.Equals(oldKind, kind, StringComparison.Ordinal)
            && ParamsEqual(oldParams, newParams))
        {
            return;
        }

        target.Trigger = trigger;
        target.Kind = kind;
        target.Params = newParams;
        _history.Record(new EditCommand(
            () =>
            {
                target.Trigger = trigger;
                target.Kind = kind;
                target.Params = newParams;
            },
            () =>
            {
                target.Trigger = oldTrigger;
                target.Kind = oldKind;
                target.Params = oldParams;
            }));
    }

    /// <summary>Remove the selected event; returns <see langword="false"/> when nothing is selected. Recorded for undo (re-inserts at the same position).</summary>
    public bool RemoveSelected()
    {
        if (_selected is null)
        {
            return false;
        }

        RemoveEvent(_selected);
        return true;
    }

    /// <summary>Remove the event at <paramref name="cell"/>; returns <see langword="false"/> when the cell has none. Recorded for undo (re-inserts at the same position).</summary>
    public bool RemoveEventAt(Cell cell)
    {
        EventData? found = _events.FirstOrDefault(e => e.X == cell.X && e.Y == cell.Y);
        if (found is null)
        {
            return false;
        }

        RemoveEvent(found);
        return true;
    }

    private void RemoveEvent(EventData target)
    {
        int index = _events.IndexOf(target);
        _events.RemoveAt(index);
        if (ReferenceEquals(_selected, target))
        {
            _selected = null;
        }

        _history.Record(new EditCommand(
            () => _events.Remove(target),
            () => _events.Insert(index, target)));
    }

    private EditCommand TileCommand(IReadOnlyList<TileChange> changes) =>
        new(
            () => ApplyTiles(changes, redo: true),
            () => ApplyTiles(changes, redo: false));

    private void ApplyTiles(IReadOnlyList<TileChange> changes, bool redo)
    {
        if (redo)
        {
            foreach (TileChange change in changes)
            {
                _editor.Map.SetTile(new Point(change.Cell.X, change.Cell.Y), change.After);
            }

            return;
        }

        // Undo unwinds in REVERSE so a cell recorded more than once in one stroke restores its ORIGINAL tile,
        // not an intermediate (the last-recorded change is reverted first).
        for (int i = changes.Count - 1; i >= 0; i--)
        {
            TileChange change = changes[i];
            _editor.Map.SetTile(new Point(change.Cell.X, change.Cell.Y), change.Before);
        }
    }

    private static bool ParamsEqual(Dictionary<string, string> a, Dictionary<string, string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }

        foreach (KeyValuePair<string, string> entry in a)
        {
            if (!b.TryGetValue(entry.Key, out string? value) || !string.Equals(value, entry.Value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }

    private static EventView View(EventData e) =>
        new(new Cell(e.X, e.Y), e.Id, e.Trigger, e.Kind, e.Params);

    private static int MaxEventSeq(IEnumerable<EventData> events)
    {
        var max = 0;
        foreach (EventData e in events)
        {
            if (e.Id.StartsWith("event-", StringComparison.Ordinal)
                && int.TryParse(e.Id.AsSpan("event-".Length), out int n)
                && n > max)
            {
                max = n;
            }
        }

        return max;
    }

    /// <summary>A reversible single-cell tile edit: the cell, the tile <see cref="Before"/> the paint, and the tile <see cref="After"/>.</summary>
    private readonly record struct TileChange(Cell Cell, Tile Before, Tile After);
}
