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
/// <see cref="MapEditor"/>), the active paint <see cref="Tile"/>, and the active tileset name, and
/// orchestrates new / paint / save / load over <see cref="MapSerializer"/> strings. Pure logic — no UI and no
/// file IO — so the Studio host drives it and it stays fully unit-testable.
/// </summary>
public sealed class MapPaintSession
{
    private readonly List<EventData> _events = new();
    private MapEditor _editor;
    private Tile _active = new(0, false);
    private EventData? _selected;
    private int _eventSeq;

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

    /// <summary>Paint the active tile at <paramref name="cell"/>; returns <see langword="false"/> (a no-op) when off-map.</summary>
    public bool Paint(Cell cell) => _editor.Paint(new Point(cell.X, cell.Y), _active);

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

        _editor.Map.TilesetName = name;
        return true;
    }

    /// <summary>Replace the map under edit with a fresh, all-empty <paramref name="width"/>×<paramref name="height"/> map, keeping the active tileset (clears placed events).</summary>
    public void NewMap(int width, int height)
    {
        string tileset = _editor.Map.TilesetName;
        _editor = new MapEditor(new TileMap(width, height));
        _editor.Map.TilesetName = tileset;
        _events.Clear();
        _selected = null;
        _eventSeq = 0;
    }

    /// <summary>Serialize the current map and its placed events to a <c>$data</c> JSON string.</summary>
    public string Save() => MapSerializer.Serialize(Map, _events);

    /// <summary>
    /// Parse a <c>$data</c> JSON <paramref name="json"/> string; on success swap the map under edit to the
    /// loaded one, otherwise leave the current map untouched. Returns the typed <see cref="MapLoadResult"/> —
    /// never throws on malformed input.
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
    /// event per cell). Returns the selected event.
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
        return View(placed);
    }

    /// <summary>Select the event at <paramref name="cell"/> (or clear the selection when the cell has none); returns the selection.</summary>
    public EventView? SelectEvent(Cell cell)
    {
        _selected = _events.FirstOrDefault(e => e.X == cell.X && e.Y == cell.Y);
        return SelectedEvent;
    }

    /// <summary>Update the selected event's <paramref name="trigger"/>, <paramref name="kind"/>, and <paramref name="params"/>; a no-op when nothing is selected.</summary>
    public void UpdateSelected(string trigger, string kind, IReadOnlyDictionary<string, string> @params)
    {
        ArgumentNullException.ThrowIfNull(trigger);
        ArgumentNullException.ThrowIfNull(kind);
        ArgumentNullException.ThrowIfNull(@params);

        if (_selected is null)
        {
            return;
        }

        _selected.Trigger = trigger;
        _selected.Kind = kind;
        _selected.Params = @params.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);
    }

    /// <summary>Remove the selected event; returns <see langword="false"/> when nothing is selected.</summary>
    public bool RemoveSelected()
    {
        if (_selected is null)
        {
            return false;
        }

        _events.Remove(_selected);
        _selected = null;
        return true;
    }

    /// <summary>Remove the event at <paramref name="cell"/>; returns <see langword="false"/> when the cell has none.</summary>
    public bool RemoveEventAt(Cell cell)
    {
        EventData? found = _events.FirstOrDefault(e => e.X == cell.X && e.Y == cell.Y);
        if (found is null)
        {
            return false;
        }

        _events.Remove(found);
        if (ReferenceEquals(_selected, found))
        {
            _selected = null;
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
}
