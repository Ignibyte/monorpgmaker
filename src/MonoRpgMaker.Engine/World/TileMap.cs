using System;
using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.World;

/// <summary>A rectangular grid of <see cref="Tile"/>s with a single collision layer.</summary>
public sealed class TileMap
{
    private readonly Tile[] _tiles;

    /// <summary>Create an all-empty map of the given tile dimensions.</summary>
    public TileMap(int width, int height)
    {
        if (width <= 0)
            throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0)
            throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        _tiles = new Tile[width * height];
        Array.Fill(_tiles, Tile.Empty);
    }

    /// <summary>Map width in tiles.</summary>
    public int Width { get; }

    /// <summary>Map height in tiles.</summary>
    public int Height { get; }

    /// <summary>True when <paramref name="cell"/> is inside the grid.</summary>
    public bool InBounds(Point cell) =>
        cell.X >= 0 && cell.Y >= 0 && cell.X < Width && cell.Y < Height;

    /// <summary>The tile at <paramref name="cell"/>; throws if out of bounds.</summary>
    public Tile GetTile(Point cell)
    {
        if (!InBounds(cell))
            throw new ArgumentOutOfRangeException(nameof(cell));
        return _tiles[Index(cell)];
    }

    /// <summary>Overwrite the tile at <paramref name="cell"/>; throws if out of bounds.</summary>
    public void SetTile(Point cell, Tile tile)
    {
        if (!InBounds(cell))
            throw new ArgumentOutOfRangeException(nameof(cell));
        _tiles[Index(cell)] = tile;
    }

    /// <summary>True if the cell is off-map or holds a blocking tile.</summary>
    public bool IsBlocked(Point cell) => !InBounds(cell) || GetTile(cell).Blocking;

    private int Index(Point cell) => (cell.Y * Width) + cell.X;
}
