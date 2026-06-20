using System;
using System.Collections.Generic;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-007 — the gated multi-map project (set create / select / start-pointer + the whole-set Save/Load round-trip
/// + Load totality) — and REQ-008 — the single-source param-key contract the Studio's kind-aware Warp inspector
/// renders from (so it can't drift from the registry).
/// </summary>
public class MapProjectTests
{
    [Fact] // REQ-007 — a new project has one map, the start + the active
    public void New_HasOneStartMap()
    {
        var project = new MapProject("start", 8, 6);

        Assert.Equal(1, project.MapCount);
        Assert.Equal("start", project.ActiveId);
        Assert.Equal("start", project.StartMapId);
    }

    [Fact] // REQ-007 — NewMap adds a map and switches the active to it
    public void NewMap_AddsAndSwitches()
    {
        var project = new MapProject("start", 8, 6);

        Assert.True(project.NewMap("town", 5, 5));
        Assert.Equal(2, project.MapCount);
        Assert.Equal("town", project.ActiveId);
    }

    [Fact] // REQ-007 — a duplicate id is rejected (no overwrite, count unchanged)
    public void NewMap_DuplicateId_Rejected()
    {
        var project = new MapProject("start", 8, 6);

        Assert.False(project.NewMap("start", 5, 5));
        Assert.Equal(1, project.MapCount);
    }

    [Fact] // REQ-007 — selecting an unknown id fails (active unchanged)
    public void SelectMap_Unknown_Fails()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);

        Assert.False(project.SelectMap("ghost"));
        Assert.Equal("town", project.ActiveId);
    }

    [Fact] // REQ-007 — selecting a known id switches the active
    public void SelectMap_Known_Switches()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);

        Assert.True(project.SelectMap("start"));
        Assert.Equal("start", project.ActiveId);
    }

    [Fact] // REQ-007 — SetStartMap rejects an unknown id
    public void SetStartMap_Unknown_Rejected()
    {
        var project = new MapProject("start", 8, 6);

        Assert.False(project.SetStartMap("ghost"));
        Assert.Equal("start", project.StartMapId);
    }

    [Fact] // REQ-007 — SetStartMap moves the pointer to a known id
    public void SetStartMap_Known_Moves()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);

        Assert.True(project.SetStartMap("town"));
        Assert.Equal("town", project.StartMapId);
    }

    [Fact] // REQ-007 — Save → Load round-trips the map ids
    public void SaveLoad_RoundTripsIds()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);
        SavedProject saved = project.Save();

        MapProject? loaded = MapProject.Load(saved.Manifest, saved.Maps);

        Assert.NotNull(loaded);
        Assert.Equal(2, loaded!.MapIds.Count);
        Assert.Contains("start", loaded.MapIds);
        Assert.Contains("town", loaded.MapIds);
    }

    [Fact] // REQ-007 — Save → Load round-trips the start-map pointer
    public void SaveLoad_RoundTripsStartMap()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);
        project.SetStartMap("town");
        SavedProject saved = project.Save();

        MapProject? loaded = MapProject.Load(saved.Manifest, saved.Maps);

        Assert.Equal("town", loaded!.StartMapId);
    }

    [Fact] // REQ-007 — Save → Load preserves each map's content (a painted tile survives)
    public void SaveLoad_PreservesMapContent()
    {
        var project = new MapProject("start", 8, 6);
        project.Active.SelectTile(5, blocking: true);
        project.Active.Paint(new Cell(2, 3));
        SavedProject saved = project.Save();

        MapProject? loaded = MapProject.Load(saved.Manifest, saved.Maps);

        Assert.Equal(5, loaded!.Active.TileAt(2, 3).TilesetId);
    }

    [Fact] // REQ-007 — a malformed manifest → null (never a throw)
    public void Load_MalformedManifest_Null()
    {
        Assert.Null(MapProject.Load("{ not valid json", new Dictionary<string, string>()));
    }

    [Fact] // REQ-007 — a manifest referencing a missing map → null
    public void Load_MissingMapData_Null()
    {
        var project = new MapProject("start", 8, 6);
        project.NewMap("town", 5, 5);
        SavedProject saved = project.Save();
        var incomplete = new Dictionary<string, string> { ["start"] = saved.Maps["start"] }; // 'town' omitted

        Assert.Null(MapProject.Load(saved.Manifest, incomplete));
    }

    [Fact] // REQ-007 — a start map not in the set → null
    public void Load_StartNotInSet_Null()
    {
        string startData = new MapProject("start", 4, 4).Save().Maps["start"];
        string manifest = GameManifestSerializer.Serialize(new GameManifest { StartMap = "ghost", MapIds = ["start"] });

        Assert.Null(MapProject.Load(manifest, new Dictionary<string, string> { ["start"] = startData }));
    }

    [Fact] // REQ-007 — a referenced map whose $data is invalid → null (never a throw)
    public void Load_InvalidMapData_Null()
    {
        SavedProject saved = new MapProject("start", 8, 6).Save();
        var corrupt = new Dictionary<string, string> { ["start"] = "{ not valid map json" };

        Assert.Null(MapProject.Load(saved.Manifest, corrupt));
    }

    [Fact] // REQ-007 — the ctor rejects an empty first id
    public void Ctor_EmptyId_Throws()
    {
        Assert.Throws<ArgumentException>(() => new MapProject(string.Empty, 4, 4));
    }

    // --- REQ-008: the inspector's field set IS the kind's ParamKeys (single source, can't drift) ---

    [Fact] // REQ-008 — the Warp kind's param keys are exactly [map, x, y]
    public void WarpKind_ParamKeys_AreMapXY()
    {
        BehaviourKindInfo warp = Assert.Single(MapPaintSession.AvailableKinds, k => k.Name == "Warp");

        Assert.Equal(3, warp.ParamKeys.Count);
        Assert.Contains("map", warp.ParamKeys);
        Assert.Contains("x", warp.ParamKeys);
        Assert.Contains("y", warp.ParamKeys);
    }

    [Fact] // REQ-008 — the ShowText kind's single param key is [text]
    public void ShowTextKind_ParamKeys_IsText()
    {
        BehaviourKindInfo showText = Assert.Single(MapPaintSession.AvailableKinds, k => k.Name == "ShowText");

        Assert.Equal("text", Assert.Single(showText.ParamKeys));
    }
}
