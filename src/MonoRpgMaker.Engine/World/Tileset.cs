using System;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// An integer source rectangle in a tileset sheet (pixels). Avalonia-free so the editor logic stays
/// framework-agnostic and unit-testable; the Studio host maps it onto an Avalonia <c>Rect</c>.
/// </summary>
/// <param name="X">The left pixel.</param>
/// <param name="Y">The top pixel.</param>
/// <param name="Width">The width in pixels.</param>
/// <param name="Height">The height in pixels.</param>
public readonly record struct SourceRect(int X, int Y, int Width, int Height);

/// <summary>
/// A tileset sheet sliced into uniform square tiles: maps a <c>Tile.TilesetId</c> (a linear, row-major tile
/// index) to its <see cref="SourceRect"/> in the sheet. Pure integer math — no rendering dependency.
/// </summary>
public sealed class Tileset
{
    /// <summary>
    /// Create a tileset of <paramref name="tileCount"/> square tiles, <paramref name="tileSize"/> pixels on a
    /// side, laid out <paramref name="columns"/> tiles per row.
    /// </summary>
    public Tileset(int tileSize, int columns, int tileCount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(columns);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileCount);

        TileSize = tileSize;
        Columns = columns;
        TileCount = tileCount;
    }

    /// <summary>The square tile edge length, in pixels.</summary>
    public int TileSize { get; }

    /// <summary>How many tiles per row in the sheet.</summary>
    public int Columns { get; }

    /// <summary>The total number of tiles in the sheet.</summary>
    public int TileCount { get; }

    /// <summary>
    /// Build a tileset from a sheet's pixel dimensions: the sheet is sliced into <paramref name="tileSize"/>
    /// squares — <c>sheetWidth / tileSize</c> columns by <c>sheetHeight / tileSize</c> rows.
    /// </summary>
    public static Tileset FromSheet(int sheetWidth, int sheetHeight, int tileSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sheetWidth);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(sheetHeight);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(tileSize);

        var columns = sheetWidth / tileSize;
        var rows = sheetHeight / tileSize;
        return new Tileset(tileSize, columns, columns * rows);
    }

    /// <summary>
    /// Try to map a linear <paramref name="tilesetId"/> to its sheet <see cref="SourceRect"/>. Returns
    /// <see langword="false"/> (with a default rect) for a negative id — the <c>Tile.Empty</c> "draw nothing"
    /// sentinel — or an id at or beyond <see cref="TileCount"/>.
    /// </summary>
    public bool TryGetSourceRect(int tilesetId, out SourceRect rect)
    {
        if (tilesetId < 0 || tilesetId >= TileCount)
        {
            rect = default;
            return false;
        }

        var col = tilesetId % Columns;
        var row = tilesetId / Columns;
        rect = new SourceRect(col * TileSize, row * TileSize, TileSize, TileSize);
        return true;
    }

    /// <summary>
    /// Try to map a sheet-pixel position (e.g. a palette click) to its linear tile index. Returns
    /// <see langword="false"/> for a negative position, a column beyond the sheet, or a position past the
    /// last tile. Pure — the palette view delegates here so the math is tested, not hidden in the UI.
    /// </summary>
    public bool TryGetTileIndex(int pixelX, int pixelY, out int index)
    {
        index = 0;
        if (pixelX < 0 || pixelY < 0)
        {
            return false;
        }

        var col = pixelX / TileSize;
        var row = pixelY / TileSize;
        if (col >= Columns)
        {
            return false;
        }

        var candidate = (row * Columns) + col;
        if (candidate >= TileCount)
        {
            return false;
        }

        index = candidate;
        return true;
    }
}
