using Microsoft.Xna.Framework;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class MapEditorTests
{
    [Fact] // Paint on an in-bounds cell sets the tile and returns true
    public void Paint_InBounds_SetsTile_ReturnsTrue()
    {
        var map = new TileMap(3, 3);
        var editor = new MapEditor(map);
        var tile = new Tile(TilesetId: 7, Blocking: true);

        Assert.True(editor.Paint(new Point(1, 1), tile));
        Assert.Equal(tile, map.GetTile(new Point(1, 1)));
    }

    [Fact] // Paint off-map returns false and leaves the map unchanged
    public void Paint_OffMap_ReturnsFalse_LeavesMapUnchanged()
    {
        var map = new TileMap(2, 2);
        var editor = new MapEditor(map);
        Tile before = map.GetTile(new Point(0, 0));

        Assert.False(editor.Paint(new Point(5, 5), new Tile(TilesetId: 9, Blocking: true)));
        Assert.Equal(before, map.GetTile(new Point(0, 0)));
    }

    [Fact] // Clear resets every cell to Tile.Empty
    public void Clear_ResetsEveryCell_ToEmpty()
    {
        var map = new TileMap(2, 2);
        var editor = new MapEditor(map);
        editor.Paint(new Point(0, 0), new Tile(TilesetId: 4, Blocking: false));
        editor.Paint(new Point(1, 1), new Tile(TilesetId: 4, Blocking: false));

        editor.Clear();

        for (var y = 0; y < 2; y++)
        {
            for (var x = 0; x < 2; x++)
            {
                Assert.Equal(Tile.Empty, map.GetTile(new Point(x, y)));
            }
        }
    }
}
