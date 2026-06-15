using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Data;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Tests;

public class TileTests
{
    [Fact]
    public void Empty_IsEmpty_AndNonBlocking()
    {
        var empty = Tile.Empty;
        Assert.True(empty.IsEmpty);
        Assert.False(empty.Blocking);
        Assert.True(empty.TilesetId < 0);
    }

    [Fact]
    public void Painted_Tile_IsNotEmpty_AndKeepsFields()
    {
        var t = new Tile(TilesetId: 7, Blocking: true);
        Assert.False(t.IsEmpty);
        Assert.Equal(7, t.TilesetId);
        Assert.True(t.Blocking);
    }

    [Fact]
    public void ZeroTilesetId_IsNotEmpty()
    {
        // Boundary: id 0 is a real tile; only a negative id is "empty".
        Assert.False(new Tile(0, false).IsEmpty);
    }
}

public class DirectionTests
{
    [Theory]
    [InlineData(Direction.Up, 0, -1)]
    [InlineData(Direction.Down, 0, 1)]
    [InlineData(Direction.Left, -1, 0)]
    [InlineData(Direction.Right, 1, 0)]
    public void ToStep_MapsEachFacing_ToItsUnitVector(Direction d, int dx, int dy)
    {
        Assert.Equal(new Point(dx, dy), d.ToStep());
    }
}

public class DatabaseExtraTests
{
    [Fact]
    public void TryGet_Miss_ReturnsFalse_AndDefault()
    {
        var db = new Database<ItemRecord>();
        Assert.False(db.TryGet(42, out var missing));
        Assert.Equal(default, missing);
    }

    [Fact]
    public void Add_Replaces_SameId_AndAllReflectsContents()
    {
        var db = new Database<ActorRecord>();
        db.Add(new ActorRecord(1, "Hero", 30));
        db.Add(new ActorRecord(1, "Hero+", 40));   // same id replaces
        db.Add(new ActorRecord(2, "Mage", 18));

        Assert.Equal(2, db.Count);
        Assert.Equal("Hero+", db.Get(1).Name);
        Assert.Equal(40, db.Get(1).MaxHp);

        var all = db.All;
        Assert.Equal(2, all.Count);
        Assert.Contains(all, r => r.Id == 2 && r.Name == "Mage");
    }
}
