using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Editor;

/// <summary>
/// Headless map-editing operations the editor UI will drive. Kept as pure logic for
/// now so it is unit-testable; a MonoGame/Avalonia front-end is layered on later.
/// </summary>
public sealed class MapEditor
{
    /// <summary>Edit the given map in place.</summary>
    public MapEditor(TileMap map) => Map = map;

    /// <summary>The map under edit.</summary>
    public TileMap Map { get; }

    /// <summary>Paint a tile, returning false if the cell is off-map.</summary>
    public bool Paint(Point cell, Tile tile)
    {
        if (!Map.InBounds(cell))
            return false;

        Map.SetTile(cell, tile);
        return true;
    }

    /// <summary>Reset every cell back to <see cref="Tile.Empty"/>.</summary>
    public void Clear()
    {
        for (var y = 0; y < Map.Height; y++)
        {
            for (var x = 0; x < Map.Width; x++)
                Map.SetTile(new Point(x, y), Tile.Empty);
        }
    }
}
