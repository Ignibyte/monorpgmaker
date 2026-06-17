using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Data;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Tests;

public class TileMapTests
{
    [Fact]
    public void NewMap_IsAllEmpty_AndUnblocked()
    {
        var map = new TileMap(4, 3);

        Assert.Equal(4, map.Width);
        Assert.Equal(3, map.Height);
        Assert.True(map.GetTile(new Point(0, 0)).IsEmpty);
        Assert.False(map.IsBlocked(new Point(1, 1)));
    }

    [Fact]
    public void IsBlocked_True_ForBlockingTile_AndOffMap()
    {
        var map = new TileMap(2, 2);
        map.SetTile(new Point(1, 1), new Tile(TilesetId: 5, Blocking: true));

        Assert.True(map.IsBlocked(new Point(1, 1)));
        Assert.True(map.IsBlocked(new Point(-1, 0)));
        Assert.True(map.IsBlocked(new Point(2, 2)));
    }

    [Fact]
    public void Ctor_NonPositiveDimensions_Throw()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new TileMap(0, 5));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => new TileMap(5, 0));
    }

    [Fact]
    public void GetTile_And_SetTile_OffMap_Throw()
    {
        var map = new TileMap(2, 2);
        Assert.Throws<System.ArgumentOutOfRangeException>(() => map.GetTile(new Point(2, 0)));
        Assert.Throws<System.ArgumentOutOfRangeException>(() => map.SetTile(new Point(2, 0), Tile.Empty));
    }

    [Fact]
    public void IsBlocked_AtEachBoundary_IsOffMap()
    {
        var map = new TileMap(2, 2);
        Assert.True(map.IsBlocked(new Point(2, 0)));   // X == Width, Y in range
        Assert.True(map.IsBlocked(new Point(0, 2)));   // Y == Height, X in range
    }
}

public class EntityMovementTests
{
    [Fact]
    public void TryStep_MovesIntoOpenTile_AndUpdatesFacing()
    {
        var map = new TileMap(3, 3);
        var actor = new Actor("Hero", new Point(1, 1), maxHp: 10);

        Assert.True(actor.TryStep(Direction.Up, map));
        Assert.Equal(new Point(1, 0), actor.Cell);
        Assert.Equal(Direction.Up, actor.Facing);
    }

    [Fact]
    public void TryStep_Blocked_DoesNotMove_ButStillTurns()
    {
        var map = new TileMap(3, 3);
        map.SetTile(new Point(1, 0), new Tile(TilesetId: 1, Blocking: true));
        var actor = new Actor("Hero", new Point(1, 1), maxHp: 10);

        Assert.False(actor.TryStep(Direction.Up, map));
        Assert.Equal(new Point(1, 1), actor.Cell);
        Assert.Equal(Direction.Up, actor.Facing);
    }
}

public class ActorStatsTests
{
    [Fact]
    public void Damage_And_Heal_ClampToRange()
    {
        var actor = new Actor("Hero", Point.Zero, maxHp: 20);

        actor.TakeDamage(5);
        Assert.Equal(15, actor.Hp);

        actor.Heal(100);
        Assert.Equal(20, actor.Hp);

        actor.TakeDamage(999);
        Assert.True(actor.IsDefeated);
    }
}

public class DatabaseTests
{
    [Fact]
    public void Add_Then_Get_RoundTrips()
    {
        var db = new Database<ItemRecord>();
        db.Add(new ItemRecord(1, "Potion", 50));

        Assert.Equal(1, db.Count);
        Assert.Equal("Potion", db.Get(1).Name);
        Assert.True(db.TryGet(1, out var found));
        Assert.Equal(50, found.Price);
        Assert.False(db.TryGet(99, out _));
    }
}
