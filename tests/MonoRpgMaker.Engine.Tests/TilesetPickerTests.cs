using System.Linq;
using System.Text.Json;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// Tests for the per-map tileset picker (#20): the <c>$data</c> tileset name (serialize / default / validate),
/// the single-source <see cref="TilesetCatalog"/>, and the editor session's tileset selection.
/// </summary>
public class TilesetPickerTests
{
    private static readonly string[] ExpectedCatalogNames = ["lpc-mountains", "lpc-grass", "lpc-dirt", "lpc-water"];

    // ---- $data round-trip + default-on-absent + validation (MapSerializer / TileMap) ----

    [Fact] // T1 (REQ-001) — the tileset name is written to $data and round-trips through Serialize → Deserialize
    public void Serialize_WritesTilesetName_AndRoundTrips()
    {
        var map = new TileMap(2, 2) { TilesetName = "lpc-grass" };

        string json = MapSerializer.Serialize(map);

        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal("lpc-grass", doc.RootElement.GetProperty("tileset").GetString());

        MapLoadResult result = MapSerializer.Deserialize(json);
        Assert.True(result.Ok);
        Assert.Equal("lpc-grass", result.Map!.TilesetName);
    }

    [Fact] // T2 (REQ-002) — a $data document with NO tileset field defaults to the canonical sheet (back-compat)
    public void Deserialize_AbsentTileset_DefaultsToMountains()
    {
        const string json = "{\"width\":1,\"height\":1,\"tiles\":[{\"tilesetId\":0,\"blocking\":false}]}";

        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.True(result.Ok);
        Assert.Equal("lpc-mountains", result.Map!.TilesetName);
    }

    [Fact] // T3 (REQ-003) — an unknown tileset name is a typed failure (no throw; the test running proves no throw)
    public void Deserialize_UnknownTileset_FailsTyped()
    {
        const string json = "{\"width\":1,\"height\":1,\"tiles\":[{\"tilesetId\":0,\"blocking\":false}],\"tileset\":\"bogus\"}";

        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.False(result.Ok);
        Assert.Contains("bogus", result.Error!);
    }

    // ---- the single-source catalog (REQ-005 / REQ-006) ----

    [Fact] // T5a — the catalog is exactly the four committed LPC sheets, the default (mountains) first
    public void Catalog_All_IsTheFourLpcSheets()
    {
        string[] names = TilesetCatalog.All.Select(t => t.Name).ToArray();
        Assert.Equal(ExpectedCatalogNames, names);

        foreach (TilesetInfo info in TilesetCatalog.All)
        {
            Assert.Equal(info.Name + ".png", info.ResourceFile);
            Assert.Equal(32, info.TileSize);
        }
    }

    [Fact] // T5b — the editor's available tilesets ARE the catalog (one source; the picker cannot drift)
    public void AvailableTilesets_IsTheCatalog() =>
        Assert.Same(TilesetCatalog.All, MapPaintSession.AvailableTilesets);

    [Fact] // T6 — TryGet resolves a known sheet's file and rejects an unknown name
    public void Catalog_TryGet_KnownAndUnknown()
    {
        Assert.True(TilesetCatalog.TryGet("lpc-water", out TilesetInfo info));
        Assert.Equal("lpc-water.png", info.ResourceFile);
        Assert.False(TilesetCatalog.TryGet("bogus", out _));
    }

    [Fact] // T6b — ResolveResourceFile is the single name→file decision; an unknown name falls back to the default
    public void Catalog_ResolveResourceFile_KnownAndFallback()
    {
        Assert.Equal("lpc-dirt.png", TilesetCatalog.ResolveResourceFile("lpc-dirt"));
        Assert.Equal("lpc-mountains.png", TilesetCatalog.ResolveResourceFile("bogus"));
        Assert.Equal("lpc-mountains.png", TilesetCatalog.Default.ResourceFile);
        Assert.Equal(TilesetCatalog.DefaultName, TilesetCatalog.Default.Name);
    }

    // ---- the editor session's tileset selection (REQ-004) ----

    [Fact] // T4a — SelectTileset switches the active tileset and Save persists the new name
    public void SelectTileset_Known_SwitchesAndPersists()
    {
        var session = new MapPaintSession("lpc-mountains", 3, 3);

        Assert.True(session.SelectTileset("lpc-grass"));
        Assert.Equal("lpc-grass", session.ActiveTileset);

        using JsonDocument doc = JsonDocument.Parse(session.Save());
        Assert.Equal("lpc-grass", doc.RootElement.GetProperty("tileset").GetString());
    }

    [Fact] // T4b — an unknown SelectTileset is rejected and does NOT mutate the active tileset
    public void SelectTileset_Unknown_RejectedNoMutation()
    {
        var session = new MapPaintSession("lpc-grass", 3, 3);

        Assert.False(session.SelectTileset("bogus"));
        Assert.Equal("lpc-grass", session.ActiveTileset);
    }

    [Fact] // T7 — the tileset name round-trips through a session Save → Load into a fresh session
    public void Session_SaveLoad_PreservesTileset()
    {
        var source = new MapPaintSession("lpc-mountains", 4, 4);
        source.SelectTileset("lpc-grass");
        string saved = source.Save();

        var target = new MapPaintSession("lpc-mountains", 1, 1);
        MapLoadResult result = target.Load(saved);

        Assert.True(result.Ok);
        Assert.Equal("lpc-grass", target.ActiveTileset);
    }

    [Fact] // T8 — NewMap keeps the active tileset (only the grid resets)
    public void NewMap_PreservesActiveTileset()
    {
        var session = new MapPaintSession("lpc-mountains", 5, 5);
        Assert.True(session.SelectTileset("lpc-water"));

        session.NewMap(3, 3);

        Assert.Equal("lpc-water", session.ActiveTileset);
    }

    [Fact] // ctor with an unknown/garbage name defaults safely (no throw) — totality at the session boundary
    public void Ctor_UnknownName_DefaultsToMountains()
    {
        var session = new MapPaintSession("bogus", 2, 2);

        Assert.Equal("lpc-mountains", session.ActiveTileset);
    }

    // T9 (the golden start.json) is covered by RuntimePlayableTests.CommittedStartJson_MatchesBuild — not duplicated.
}
