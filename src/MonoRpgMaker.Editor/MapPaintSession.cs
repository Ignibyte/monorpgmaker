using System;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Editor;

/// <summary>A map grid cell coordinate (column <paramref name="X"/>, row <paramref name="Y"/>). Framework-neutral so a host needs no engine Point type.</summary>
/// <param name="X">The zero-based column.</param>
/// <param name="Y">The zero-based row.</param>
public readonly record struct Cell(int X, int Y);

/// <summary>
/// The Avalonia-free editing session behind the map painter: it holds the map under edit (via a
/// <see cref="MapEditor"/>), the active paint <see cref="Tile"/>, and the <see cref="Tileset"/>, and
/// orchestrates new / paint / save / load over <see cref="MapSerializer"/> strings. Pure logic — no UI and no
/// file IO — so the Studio host drives it and it stays fully unit-testable.
/// </summary>
public sealed class MapPaintSession
{
    private MapEditor _editor;
    private Tile _active = new(0, false);

    /// <summary>
    /// Start a session over a fresh <paramref name="width"/>×<paramref name="height"/> map that paints tiles
    /// from <paramref name="tileset"/>.
    /// </summary>
    public MapPaintSession(Tileset tileset, int width, int height)
    {
        ArgumentNullException.ThrowIfNull(tileset);

        Tileset = tileset;
        _editor = new MapEditor(new TileMap(width, height));
    }

    /// <summary>The tileset whose tiles this session paints.</summary>
    public Tileset Tileset { get; }

    /// <summary>
    /// The map currently under edit. Engine-internal — Avalonia hosts should read tiles via
    /// <see cref="Width"/>/<see cref="Height"/>/<see cref="TileAt"/>; this exposes the engine
    /// <c>TileMap</c> (whose members take a MonoGame <c>Point</c>), so binding it from a host would pull
    /// MonoGame into the host's compile graph.
    /// </summary>
    public TileMap Map => _editor.Map;

    /// <summary>The tile that <see cref="Paint"/> writes.</summary>
    public Tile Active => _active;

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

    /// <summary>Replace the map under edit with a fresh, all-empty <paramref name="width"/>×<paramref name="height"/> map.</summary>
    public void NewMap(int width, int height) => _editor = new MapEditor(new TileMap(width, height));

    /// <summary>Serialize the current map to a <c>$data</c> JSON string.</summary>
    public string Save() => MapSerializer.Serialize(Map);

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
        }

        return result;
    }
}
