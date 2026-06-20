using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>Tests for the player-following <see cref="Camera"/> (pure integer view-offset math).</summary>
public class CameraTests
{
    [Fact] // a map smaller than the viewport on both axes → no scroll (the Math.Max(0, …) floor)
    public void SmallMap_NoScroll() =>
        Assert.Equal(new Point(0, 0), Camera.ViewOffset(new Point(150, 150), new Point(640, 480), new Point(300, 300)));

    [Fact] // X scrolls to its max while Y is floored to 0 — the axes are independent
    public void AsymmetricMap_XScrollsYFloored() =>
        Assert.Equal(new Point(1360, 0), Camera.ViewOffset(new Point(5000, 5000), new Point(640, 480), new Point(2000, 288)));

    [Fact] // player at the origin of a big map → clamped up to (0,0)
    public void ClampLeftTop() =>
        Assert.Equal(new Point(0, 0), Camera.ViewOffset(new Point(0, 0), new Point(640, 480), new Point(2000, 2000)));

    [Fact] // player far right → x clamped to mapW - viewW
    public void ClampRight() =>
        Assert.Equal(1360, Camera.ViewOffset(new Point(1_000_000, 0), new Point(640, 480), new Point(2000, 2000)).X);

    [Fact] // player far down → y clamped to mapH - viewH (2000 - 480 = 1520)
    public void ClampBottom() =>
        Assert.Equal(1520, Camera.ViewOffset(new Point(0, 1_000_000), new Point(640, 480), new Point(2000, 2000)).Y);

    [Fact] // centred: player − viewport/2 (pins the /2 and the subtraction; 320 ≠ 240 catches an axis swap)
    public void Centre() =>
        Assert.Equal(new Point(680, 760), Camera.ViewOffset(new Point(1000, 1000), new Point(640, 480), new Point(2000, 2000)));
}

/// <summary>Tests for the bundled <see cref="StartMap"/> (the map, the no-events world, and the data-load path).</summary>
public class StartMapTests
{
    [Fact] // dimensions (asymmetric — a Width/Height swap is caught)
    public void Build_Dimensions()
    {
        TileMap map = StartMap.Build();
        Assert.Equal(28, map.Width);
        Assert.Equal(18, map.Height);
    }

    [Fact] // the border is the blocking Wall tile; the interior + PlayerStart are the walkable Floor tile
    public void Build_BorderBlocking_InteriorFloor()
    {
        TileMap map = StartMap.Build();
        var wall = new Tile(1, true);
        var floor = new Tile(66, false);
        Assert.Equal(wall, map.GetTile(new Point(0, 0)));    // top-left corner (x==0, y==0)
        Assert.Equal(wall, map.GetTile(new Point(27, 0)));   // x == Width-1
        Assert.Equal(wall, map.GetTile(new Point(0, 17)));   // y == Height-1
        Assert.Equal(wall, map.GetTile(new Point(9, 17)));   // bottom edge
        Assert.Equal(wall, map.GetTile(new Point(27, 5)));   // right edge
        Assert.Equal(floor, map.GetTile(new Point(2, 2)));   // PlayerStart
        Assert.Equal(floor, map.GetTile(new Point(14, 9)));  // interior
    }

    [Fact] // PlayerStart is a known, walkable floor cell
    public void PlayerStart_IsFloor()
    {
        Assert.Equal(new Point(2, 2), StartMap.PlayerStart);
        Assert.False(StartMap.Build().GetTile(StartMap.PlayerStart).Blocking);
    }

    [Fact] // BuildWorld places the player at PlayerStart over the built map
    public void BuildWorld_PlayerAtStart()
    {
        WorldSim sim = StartMap.BuildWorld();
        Assert.Equal(new Point(2, 2), sim.Player.Cell);
        Assert.Equal(28, sim.Map.Width);
    }

    [Fact] // LoadWorld round-trips a serialized build (the good-data branch)
    public void LoadWorld_RoundTrip()
    {
        WorldSimResult result = StartMap.LoadWorld(MapSerializer.Serialize(StartMap.Build()));

        Assert.True(result.Ok);
        Assert.Equal(new Point(2, 2), result.Sim!.Player.Cell);
        TileMap expected = StartMap.Build();
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                Assert.Equal(expected.GetTile(new Point(x, y)), result.Sim.Map.GetTile(new Point(x, y)));
            }
        }
    }

    [Theory] // malformed input → typed failure, never a throw (the bad-data branch)
    [InlineData("{bad")]
    [InlineData("null")]
    [InlineData("")]
    [InlineData("[]")]
    public void LoadWorld_Malformed_FailsWithoutThrowing(string json)
    {
        WorldSimResult? result = null;

        Assert.Null(Record.Exception(() => result = StartMap.LoadWorld(json)));
        Assert.False(result!.Ok);
        Assert.NotNull(result.Error);
        Assert.Null(result.Sim);
    }

    [Fact] // the player walks the loaded map and is blocked by the wall border
    public void Player_Walks_BlockedByWall()
    {
        WorldSim sim = StartMap.BuildWorld();
        Assert.True(sim.MovePlayer(Direction.Right));
        Assert.Equal(new Point(3, 2), sim.Player.Cell);

        WorldSim fresh = StartMap.BuildWorld();
        Assert.True(fresh.MovePlayer(Direction.Left));     // (2,2) → (1,2) floor
        Assert.Equal(new Point(1, 2), fresh.Player.Cell);
        Assert.False(fresh.MovePlayer(Direction.Left));    // (1,2) → (0,2) is the wall border
        Assert.Equal(new Point(1, 2), fresh.Player.Cell);  // unchanged
    }

    [Fact] // the committed $data matches the builder — catches a stale content/maps/start.json
    public void CommittedStartJson_MatchesBuild()
    {
        string committed = File.ReadAllText(CommittedStartJsonPath()).Replace("\r\n", "\n", StringComparison.Ordinal);
        string fromBuild = MapSerializer.Serialize(StartMap.Build(), StartMap.Events(), StartMap.Doors()).Replace("\r\n", "\n", StringComparison.Ordinal);
        Assert.Equal(fromBuild, committed);
    }

    private static string CommittedStartJsonPath([CallerFilePath] string testFile = "")
    {
        string testsDir = Path.GetDirectoryName(testFile)!;                    // <repo>/tests/MonoRpgMaker.Engine.Tests
        string repoRoot = Path.GetFullPath(Path.Combine(testsDir, "..", "..")); // <repo>
        return Path.Combine(repoRoot, "content", "maps", "start.json");
    }
}
