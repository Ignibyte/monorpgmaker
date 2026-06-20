using System;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>Tests for the <see cref="Tileset"/> sheet-geometry model (the gated, framework-free editor logic).</summary>
public class TilesetTests
{
    // The LPC mountains sheet: 384×288 at 32px → 12 columns × 9 rows = 108 tiles.
    private static Tileset Sheet() => Tileset.FromSheet(384, 288, 32);

    [Fact] // ctor guard — tileSize
    public void Ctor_TileSizeZero_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tileset(0, 12, 108));

    [Fact] // ctor guard — columns (a 0-column tileset would divide-by-zero in TryGetSourceRect)
    public void Ctor_ColumnsZero_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tileset(32, 0, 108));

    [Fact] // ctor guard — tileCount
    public void Ctor_TileCountZero_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new Tileset(32, 12, 0));

    [Fact] // FromSheet geometry — pins both divisions and the columns*rows multiply
    public void FromSheet_Geometry_Exact()
    {
        Tileset sheet = Sheet();
        Assert.Equal(12, sheet.Columns);
        Assert.Equal(108, sheet.TileCount);
        Assert.Equal(32, sheet.TileSize);
    }

    [Fact] // FromSheet at the unit boundary
    public void FromSheet_OneTile()
    {
        Tileset sheet = Tileset.FromSheet(32, 32, 32);
        Assert.Equal(1, sheet.Columns);
        Assert.Equal(1, sheet.TileCount);
    }

    [Fact] // the Tile.Empty (-1) sentinel draws nothing
    public void TryGetSourceRect_NegativeId_FalseDefault()
    {
        Assert.False(Sheet().TryGetSourceRect(-1, out SourceRect rect));
        Assert.Equal(default, rect);
    }

    [Fact] // oob-high boundary: id == TileCount is rejected (kills >= → >)
    public void TryGetSourceRect_AtTileCount_False() =>
        Assert.False(Sheet().TryGetSourceRect(108, out _));

    [Fact] // last valid id is accepted (pins the boundary at exactly 108)
    public void TryGetSourceRect_LastValid_True() =>
        Assert.True(Sheet().TryGetSourceRect(107, out _));

    [Fact] // index 0 → top-left tile
    public void TryGetSourceRect_Index0()
    {
        Assert.True(Sheet().TryGetSourceRect(0, out SourceRect rect));
        Assert.Equal(new SourceRect(0, 0, 32, 32), rect);
    }

    [Fact] // index 12 wraps to the start of row 1 (kills %↔/ swap)
    public void TryGetSourceRect_RowWrap_Index12()
    {
        Assert.True(Sheet().TryGetSourceRect(12, out SourceRect rect));
        Assert.Equal(new SourceRect(0, 32, 32, 32), rect);
    }

    [Fact] // index 107 → col 11, row 8 (asymmetric, independently kills %/÷ swap + *TileSize)
    public void TryGetSourceRect_LastCell_Index107()
    {
        Assert.True(Sheet().TryGetSourceRect(107, out SourceRect rect));
        Assert.Equal(new SourceRect(352, 256, 32, 32), rect);
    }

    [Fact] // palette click math — negative position rejected
    public void TryGetTileIndex_Negative_False()
    {
        Assert.False(Sheet().TryGetTileIndex(-1, -1, out int index));
        Assert.Equal(0, index);
    }

    [Fact] // a column beyond the sheet (px = 12*32) is rejected (col >= Columns branch)
    public void TryGetTileIndex_ColumnBeyondSheet_False() =>
        Assert.False(Sheet().TryGetTileIndex(384, 0, out _));

    [Fact] // a row past the sheet (py = 9*32 → candidate 108) is rejected (candidate >= TileCount branch)
    public void TryGetTileIndex_RowPastSheet_False() =>
        Assert.False(Sheet().TryGetTileIndex(0, 288, out _));

    [Fact] // origin → index 0
    public void TryGetTileIndex_Origin_True()
    {
        Assert.True(Sheet().TryGetTileIndex(0, 0, out int index));
        Assert.Equal(0, index);
    }

    [Fact] // second column within row 0 → index 1
    public void TryGetTileIndex_SecondColumn()
    {
        Assert.True(Sheet().TryGetTileIndex(40, 5, out int index));
        Assert.Equal(1, index);
    }

    [Fact] // first column of row 1 → index 12 (row*Columns + col)
    public void TryGetTileIndex_SecondRow()
    {
        Assert.True(Sheet().TryGetTileIndex(5, 40, out int index));
        Assert.Equal(12, index);
    }
}

/// <summary>Tests for the <see cref="MapPaintSession"/> editor view-model (pointer math, paint, save/load totality).</summary>
public class MapPaintSessionTests
{
    private static MapPaintSession Session(int width = 20, int height = 15) => new(TilesetCatalog.DefaultName, width, height);

    [Fact] // a zero cell size is a precondition violation
    public void CellAt_NegativeCellSize_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => Session().CellAt(0, 0, 0));

    [Fact] // negative X → off-map (the first ||-operand)
    public void CellAt_NegativeX_Null() => Assert.Null(Session().CellAt(-1, 5, 32));

    [Fact] // negative Y → off-map (the second ||-operand)
    public void CellAt_NegativeY_Null() => Assert.Null(Session().CellAt(5, -1, 32));

    [Fact] // the right edge is exclusive: 640 == Width*cellSize → null (kills >= → >)
    public void CellAt_RightEdge_Null() => Assert.Null(Session().CellAt(640, 0, 32));

    [Fact] // the bottom edge is exclusive: 480 == Height*cellSize → null
    public void CellAt_BottomEdge_Null() => Assert.Null(Session().CellAt(0, 480, 32));

    [Fact] // origin maps to cell (0,0)
    public void CellAt_Origin() => Assert.Equal(new Cell(0, 0), Session().CellAt(0, 0, 32));

    [Fact] // the last in-bounds pixel maps to the last cell
    public void CellAt_LastCell() => Assert.Equal(new Cell(19, 14), Session().CellAt(639, 479, 32));

    [Fact] // a fractional pixel truncates to its cell
    public void CellAt_FractionFloors() => Assert.Equal(new Cell(1, 0), Session().CellAt(35.9, 1.0, 32));

    [Fact] // SelectTile sets both the id and the blocking flag
    public void SelectTile_SetsIdAndBlocking()
    {
        MapPaintSession session = Session();
        session.SelectTile(7, true);
        Assert.Equal(new Tile(7, true), session.Active);
    }

    [Fact] // the blocking default is false
    public void SelectTile_DefaultBlockingFalse()
    {
        MapPaintSession session = Session();
        session.SelectTile(7);
        Assert.False(session.Active.Blocking);
    }

    [Fact] // painting in-bounds writes the active tile via MapEditor
    public void Paint_InBounds_WritesActive()
    {
        MapPaintSession session = Session();
        session.SelectTile(7, true);
        Assert.True(session.Paint(new Cell(2, 3)));
        Assert.Equal(new Tile(7, true), session.TileAt(2, 3));
    }

    [Fact] // painting off-map is a no-op
    public void Paint_OffMap_False_NoChange()
    {
        MapPaintSession session = Session();
        session.SelectTile(7, true);
        Assert.False(session.Paint(new Cell(99, 99)));
        Assert.True(session.TileAt(5, 5).IsEmpty);
    }

    [Fact] // New resets to an all-empty map of the requested (asymmetric) size
    public void NewMap_ResizesAndClears()
    {
        MapPaintSession session = Session();
        session.SelectTile(3);
        session.Paint(new Cell(0, 0));

        session.NewMap(4, 3);

        Assert.Equal(4, session.Width);
        Assert.Equal(3, session.Height);
        for (var y = 0; y < session.Height; y++)
        {
            for (var x = 0; x < session.Width; x++)
            {
                Assert.True(session.TileAt(x, y).IsEmpty);
            }
        }
    }

    [Fact] // Save → Deserialize reproduces the painted map
    public void Save_RoundTripsThroughDeserialize()
    {
        MapPaintSession session = Session(5, 5);
        session.SelectTile(3, false);
        session.Paint(new Cell(1, 1));
        session.SelectTile(7, true);
        session.Paint(new Cell(2, 2));

        MapLoadResult result = MapSerializer.Deserialize(session.Save());

        Assert.True(result.Ok);
        Assert.Equal(new Tile(3, false), result.Map!.GetTile(new Point(1, 1)));
        Assert.Equal(new Tile(7, true), result.Map.GetTile(new Point(2, 2)));
    }

    [Fact] // a valid Load swaps the map under edit (kills the never-swap mutant)
    public void Load_Valid_SwapsMap()
    {
        MapPaintSession source = Session(6, 4);
        source.SelectTile(5, true);
        source.Paint(new Cell(3, 2));
        string saved = source.Save();

        var target = new MapPaintSession(TilesetCatalog.DefaultName, 1, 1);
        MapLoadResult result = target.Load(saved);

        Assert.True(result.Ok);
        Assert.Equal(6, target.Width);
        Assert.Equal(4, target.Height);
        Assert.Equal(new Tile(5, true), target.TileAt(3, 2));
    }

    [Theory] // a malformed Load never throws, never swaps the existing map (kills the always-swap mutant)
    [InlineData("{bad")]
    [InlineData("null")]
    [InlineData("[]")]
    [InlineData("{\"version\":1,\"width\":0,\"height\":1,\"tiles\":[]}")]
    [InlineData("")]
    public void Load_Invalid_DoesNotSwap_NonDestructive(string json)
    {
        MapPaintSession session = Session(5, 4);
        session.SelectTile(42, true);
        session.Paint(new Cell(1, 1));

        MapLoadResult? result = null;
        Assert.Null(Record.Exception(() => result = session.Load(json)));
        Assert.False(result!.Ok);
        Assert.NotNull(result.Error);
        Assert.Equal(5, session.Width);
        Assert.Equal(4, session.Height);
        Assert.Equal(new Tile(42, true), session.TileAt(1, 1));
    }
}
