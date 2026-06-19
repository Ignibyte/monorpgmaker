using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Sim.Tracer;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class MapSerializerTests
{
    [Fact] // T1 (REQ-001) — Serialize captures dims + each cell, row-major
    public void Serialize_CapturesDimensionsAndCells()
    {
        var map = new TileMap(2, 2);
        map.SetTile(new Point(0, 0), new Tile(1, true));
        map.SetTile(new Point(1, 0), new Tile(0, false));
        map.SetTile(new Point(0, 1), new Tile(2, false));
        map.SetTile(new Point(1, 1), new Tile(3, true));

        using JsonDocument doc = JsonDocument.Parse(MapSerializer.Serialize(map));
        JsonElement root = doc.RootElement;

        Assert.Equal(2, root.GetProperty("width").GetInt32());
        Assert.Equal(2, root.GetProperty("height").GetInt32());
        JsonElement tiles = root.GetProperty("tiles");
        Assert.Equal(4, tiles.GetArrayLength());
        Assert.Equal(1, tiles[0].GetProperty("tilesetId").GetInt32());
        Assert.True(tiles[0].GetProperty("blocking").GetBoolean());
        Assert.Equal(0, tiles[1].GetProperty("tilesetId").GetInt32());
        Assert.Equal(3, tiles[3].GetProperty("tilesetId").GetInt32());   // (1,1) at row-major index 3
    }

    [Fact] // T2 (REQ-002) — non-square round-trip is exact (kills the (y*Width)+x index mutants in both loops)
    public void RoundTrip_NonSquareMap_IsExact()
    {
        var map = new TileMap(4, 3);
        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 4; x++)
            {
                map.SetTile(new Point(x, y), new Tile((y * 4) + x, (x + y) % 2 == 0));
            }
        }

        map.SetTile(new Point(3, 2), Tile.Empty);

        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(map));

        Assert.True(result.Ok);
        AssertTileMapsEqual(map, result.Map!);
    }

    [Fact] // T3 (REQ-002/004) — a real authored map (the tracer room) round-trips exactly
    public void RoundTrip_TracerMap_IsExact()
    {
        TileMap tracer = TracerRoom.Build().Map;

        MapLoadResult result = MapSerializer.Deserialize(MapSerializer.Serialize(tracer));

        Assert.True(result.Ok);
        AssertTileMapsEqual(tracer, result.Map!);
    }

    [Fact] // T4 (REQ-003) — malformed JSON → typed failure, naming the cause
    public void Deserialize_MalformedJson_FailsWithReason()
    {
        MapLoadResult result = MapSerializer.Deserialize("{ not json");

        Assert.False(result.Ok);
        Assert.Contains("invalid JSON", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T4b (REQ-003) — totality: malformed input never throws
    public void Deserialize_MalformedJson_DoesNotThrow()
    {
        Assert.Null(Record.Exception(() => MapSerializer.Deserialize("{ not json")));
    }

    [Fact] // T5 (REQ-003) — an explicit JSON null document
    public void Deserialize_NullDocument_Fails()
    {
        MapLoadResult result = MapSerializer.Deserialize("null");

        Assert.False(result.Ok);
        Assert.Contains("null", result.Error!, StringComparison.Ordinal);
    }

    [Theory] // T6 (REQ-003) — non-positive width (0 kills the <=0 → <0 boundary; -1 covers negative)
    [InlineData(0)]
    [InlineData(-1)]
    public void Deserialize_NonPositiveWidth_Fails(int width)
    {
        MapLoadResult result = MapSerializer.Deserialize(
            "{\"width\":" + width + ",\"height\":2,\"tiles\":[]}");

        Assert.False(result.Ok);
        Assert.Contains("width must be positive", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T7 (REQ-003) — non-positive height
    public void Deserialize_NonPositiveHeight_Fails()
    {
        MapLoadResult result = MapSerializer.Deserialize("{\"width\":2,\"height\":0,\"tiles\":[]}");

        Assert.False(result.Ok);
        Assert.Contains("height must be positive", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T8 (REQ-003) — explicit null tiles (the data.Tiles is null branch)
    public void Deserialize_NullTiles_Fails()
    {
        MapLoadResult result = MapSerializer.Deserialize("{\"width\":2,\"height\":2,\"tiles\":null}");

        Assert.False(result.Ok);
        Assert.Contains("tiles are missing", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T9 (REQ-003) — a tile count that does not match width * height
    public void Deserialize_CountMismatch_Fails()
    {
        MapLoadResult result = MapSerializer.Deserialize(
            "{\"width\":2,\"height\":2,\"tiles\":[{\"tilesetId\":0,\"blocking\":false}]}");

        Assert.False(result.Ok);
        Assert.Contains("tile count", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T-OVERFLOW (REQ-003) — dims whose product overflows int must NOT throw (the long-product guard)
    public void Deserialize_OverflowingDimensions_FailsWithoutThrowing()
    {
        MapLoadResult? result = null;

        Assert.Null(Record.Exception(() =>
            result = MapSerializer.Deserialize("{\"width\":65536,\"height\":65536,\"tiles\":[]}")));
        Assert.False(result!.Ok);
        Assert.Contains("tile count", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // T10 (REQ-004) — the committed sample $data file loads to the expected map
    public void Deserialize_SampleFixture_LoadsExpectedMap()
    {
        string path = Path.Combine(AppContext.BaseDirectory, "Fixtures", "sample-map.json");
        string json = File.ReadAllText(path);

        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.True(result.Ok);
        TileMap map = result.Map!;
        Assert.Equal(3, map.Width);
        Assert.Equal(2, map.Height);
        Assert.Equal(new Tile(1, true), map.GetTile(new Point(0, 0)));
        Assert.Equal(new Tile(0, false), map.GetTile(new Point(1, 0)));
        Assert.Equal(new Tile(2, false), map.GetTile(new Point(1, 1)));
    }

    private static void AssertTileMapsEqual(TileMap expected, TileMap actual)
    {
        Assert.Equal(expected.Width, actual.Width);
        Assert.Equal(expected.Height, actual.Height);
        for (var y = 0; y < expected.Height; y++)
        {
            for (var x = 0; x < expected.Width; x++)
            {
                Assert.Equal(expected.GetTile(new Point(x, y)), actual.GetTile(new Point(x, y)));
            }
        }
    }
}
