using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Editor.Expectations;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..006 + REQ-009 — the runtime multi-map registry + the built-in <c>Warp</c> switch: the switch carries
/// game state (both triggers), totality on every bad-warp path (no throw), behaviour purity (D-0017), the save
/// map id, and the editor-produced set is runtime-valid (cross-layer). The kill-list from the inspect ledger.
/// </summary>
public class MultiMapWarpTests
{
    private static IEventContext Context() => new EventContext(new GameState());

    private static string FloorMap(int w, int h, params EventData[] events)
    {
        var map = new TileMap(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                map.SetTile(new Point(x, y), new Tile(0, false));
            }
        }

        return MapSerializer.Serialize(map, events);
    }

    private static string MapWithBlockingAt(int w, int h, int bx, int by)
    {
        var map = new TileMap(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                map.SetTile(new Point(x, y), new Tile(0, false));
            }
        }

        map.SetTile(new Point(bx, by), new Tile(1, true));
        return MapSerializer.Serialize(map, Array.Empty<EventData>());
    }

    private static EventData WarpData(int x, int y, string trigger, string map, int tx, int ty) =>
        new()
        {
            Id = "w",
            X = x,
            Y = y,
            Trigger = trigger,
            Kind = "Warp",
            Params =
            {
                ["map"] = map,
                ["x"] = tx.ToString(CultureInfo.InvariantCulture),
                ["y"] = ty.ToString(CultureInfo.InvariantCulture),
            },
        };

    // A start map (player at (2,2)) with a StepOn warp at (3,2) → town (2,2); one MovePlayer(Right) fires it.
    private static GameSession StartTownSession() =>
        GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 2, 2)),
                ["town"] = FloorMap(5, 5),
            },
            "start",
            new GridPoint(2, 2)).Session!;

    // --- REQ-001 / REQ-005: load by id; totality on a bad start ---

    [Fact] // REQ-001 — Create boots the start map
    public void Create_LoadsStartMap()
    {
        GameSessionResult result = GameSession.Create(
            new Dictionary<string, string> { ["start"] = FloorMap(6, 6), ["town"] = FloorMap(5, 5) },
            "start",
            new GridPoint(2, 2));

        Assert.True(result.Ok);
        Assert.Equal("start", result.Session!.ActiveMapId);
    }

    [Fact] // REQ-001/005 — an unknown start id → typed failure, never a throw
    public void Create_UnknownStartId_Fails()
    {
        GameSessionResult result = GameSession.Create(
            new Dictionary<string, string> { ["start"] = FloorMap(4, 4) }, "missing", new GridPoint(1, 1));

        Assert.False(result.Ok);
        Assert.Null(result.Session);
    }

    [Fact] // REQ-001/005 — a malformed start $data → typed failure (not only an unknown id)
    public void Create_MalformedStartData_Fails()
    {
        GameSessionResult result = GameSession.Create(
            new Dictionary<string, string> { ["start"] = "{ this is not valid map json" }, "start", new GridPoint(1, 1));

        Assert.False(result.Ok);
        Assert.Null(result.Session);
    }

    // --- REQ-002 + D-0017: the behaviour RETURNS a Warp outcome (purity) ---

    [Fact] // REQ-002 — WarpEvent.Run returns one Warp outcome carrying the target map
    public void WarpEvent_Run_ReturnsWarpMapId()
    {
        var ev = new WarpEvent(new GridPoint(3, 2), EventTrigger.StepOn, "town", new GridPoint(4, 5));

        Warp warp = Assert.IsType<Warp>(Assert.Single(ev.Run(Context())));

        Assert.Equal("town", warp.MapId);
    }

    [Fact] // REQ-002 — and the target cell (isolated from the map-id assert)
    public void WarpEvent_Run_ReturnsTargetCell()
    {
        var ev = new WarpEvent(new GridPoint(3, 2), EventTrigger.StepOn, "town", new GridPoint(4, 5));

        Warp warp = Assert.IsType<Warp>(Assert.Single(ev.Run(Context())));

        Assert.Equal(new GridPoint(4, 5), warp.Cell);
    }

    // --- REQ-004: the registry materialises Warp; missing/bad params → typed failure ---

    [Fact] // REQ-004 — a Warp placement materialises into a WarpEvent
    public void Registry_MaterialisesWarp()
    {
        BehaviourResult result = BehaviourRegistry.TryMaterialize(WarpData(1, 1, "StepOn", "town", 2, 3));

        Assert.True(result.Ok);
        Assert.IsType<WarpEvent>(result.Event);
    }

    [Fact] // REQ-004 — a missing 'map' param → typed failure
    public void Registry_Warp_MissingMap_Fails()
    {
        var data = new EventData { Id = "w", X = 1, Y = 1, Trigger = "StepOn", Kind = "Warp", Params = { ["x"] = "2", ["y"] = "3" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — a non-integer 'x' → typed failure
    public void Registry_Warp_NonIntX_Fails()
    {
        var data = new EventData { Id = "w", X = 1, Y = 1, Trigger = "StepOn", Kind = "Warp", Params = { ["map"] = "town", ["x"] = "NaN", ["y"] = "3" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — a non-integer 'y' → typed failure
    public void Registry_Warp_NonIntY_Fails()
    {
        var data = new EventData { Id = "w", X = 1, Y = 1, Trigger = "StepOn", Kind = "Warp", Params = { ["map"] = "town", ["x"] = "2", ["y"] = "yes" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    // --- REQ-003: the switch carries state, both triggers ---

    [Fact] // REQ-003 — a StepOn warp switches the active map
    public void StepOnWarp_SwitchesActiveMap()
    {
        GameSession s = StartTownSession();

        s.MovePlayer(Direction.Right);

        Assert.Equal("town", s.ActiveMapId);
    }

    [Fact] // REQ-003 — and places the player at the target cell
    public void StepOnWarp_PlacesPlayerAtTarget()
    {
        GameSession s = StartTownSession();

        s.MovePlayer(Direction.Right);

        Assert.Equal(new Point(2, 2), s.Active.Player.Cell);
    }

    [Fact] // REQ-003 — a switch set before the warp persists across it
    public void StepOnWarp_CarriesSwitch()
    {
        GameSession s = StartTownSession();
        s.Active.State.Set("door", true);

        s.MovePlayer(Direction.Right);

        Assert.True(s.Active.State.Get("door"));
    }

    [Fact] // REQ-003 — a counter added before the warp persists across it
    public void StepOnWarp_CarriesCounter()
    {
        GameSession s = StartTownSession();
        s.Active.State.Add("gold", 7);

        s.MovePlayer(Direction.Right);

        Assert.Equal(7, s.Active.State.GetCount("gold"));
    }

    [Fact] // REQ-003 — an ActionButton warp also switches the map (the player steps adjacent, then acts)
    public void ActionButtonWarp_SwitchesActiveMap()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(4, 2, "ActionButton", "town", 1, 1)),
                ["town"] = FloorMap(5, 5),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right); // (2,2) → (3,2), now facing the warp tile (4,2)
        Assert.Equal("start", s.ActiveMapId); // not warped yet (the warp is ActionButton, not StepOn)

        s.PressAction(); // faces (4,2) → the ActionButton warp fires

        Assert.Equal("town", s.ActiveMapId);
    }

    [Fact] // REQ-003 — and an ActionButton warp carries state too
    public void ActionButtonWarp_CarriesCounter()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(4, 2, "ActionButton", "town", 1, 1)),
                ["town"] = FloorMap(5, 5),
            },
            "start",
            new GridPoint(2, 2)).Session!;
        s.Active.State.Add("gold", 3);

        s.MovePlayer(Direction.Right);
        s.PressAction();

        Assert.Equal(3, s.Active.State.GetCount("gold"));
    }

    [Fact] // the switch applies exactly once — landing on another warp tile does NOT chain in one step
    public void Warp_DoesNotChain_InOneStep()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 2, 2)),
                ["town"] = FloorMap(5, 5, WarpData(2, 2, "StepOn", "third", 1, 1)),
                ["third"] = FloorMap(4, 4),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right); // start → town@(2,2); placing the player does NOT re-fire town's StepOn

        Assert.Equal("town", s.ActiveMapId); // not "third"
    }

    // --- REQ-005: the totality matrix (each: no throw + the documented no-op/clamp) ---

    [Fact] // REQ-005 — a warp to an unknown map id is a safe no-op (active map unchanged)
    public void Warp_UnknownTargetMap_IsNoOp()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string> { ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "nowhere", 1, 1)) },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal("start", s.ActiveMapId);
    }

    [Fact] // REQ-005 — an off-map target cell (incl. a 1×1 target) clamps the player in-bounds
    public void Warp_OffMapTargetCell_ClampsInBounds()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 99, 99)),
                ["town"] = FloorMap(1, 1),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal("town", s.ActiveMapId);
        Assert.True(s.Active.Map.InBounds(s.Active.Player.Cell));
    }

    [Fact] // REQ-005 — a malformed target $data is a safe no-op
    public void Warp_MalformedTargetData_IsNoOp()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 1, 1)),
                ["town"] = "{ not valid json",
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal("start", s.ActiveMapId);
    }

    [Fact] // REQ-005 — a target whose events fail to materialise (unknown kind) is a safe no-op
    public void Warp_TargetEventsUnmaterialisable_IsNoOp()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 1, 1)),
                ["town"] = FloorMap(5, 5, new EventData { Id = "x", X = 1, Y = 1, Trigger = "StepOn", Kind = "Bogus" }),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal("start", s.ActiveMapId);
    }

    [Fact] // pins the CURRENT behaviour — a warp to a blocking target cell still places the player there (in-bounds)
    public void Warp_BlockingTargetCell_PlacesPlayerThere()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 2, 2)),
                ["town"] = MapWithBlockingAt(5, 5, 2, 2),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal("town", s.ActiveMapId);
        Assert.Equal(new Point(2, 2), s.Active.Player.Cell);
    }

    // --- REQ-006: the save serializer records the map id (round-trip + back-compat) ---

    [Fact] // REQ-006 — Serialize with a map id round-trips it
    public void Save_RecordsMapId()
    {
        string json = SaveSerializer.Serialize(new GameState(), new Point(1, 1), Direction.Down, 0UL, "town");

        SaveLoadResult result = SaveSerializer.Deserialize(json);

        Assert.True(result.Ok);
        Assert.Equal("town", result.Save!.MapId);
    }

    [Fact] // REQ-006 — the default (4-arg) call records the empty map id (back-compat = the start map)
    public void Save_DefaultMapId_IsEmpty()
    {
        string json = SaveSerializer.Serialize(new GameState(), new Point(1, 1), Direction.Down, 0UL);

        SaveLoadResult result = SaveSerializer.Deserialize(json);

        Assert.Equal(string.Empty, result.Save!.MapId);
    }

    [Fact] // REQ-006 — a save after a warp records the now-active map id
    public void Save_AfterWarp_RecordsActiveMapId()
    {
        GameSession s = StartTownSession();
        s.MovePlayer(Direction.Right); // → town

        string json = SaveSerializer.Serialize(s.Active.State, s.Active.Player.Cell, Direction.Down, 0UL, s.ActiveMapId);

        Assert.Equal("town", SaveSerializer.Deserialize(json).Save!.MapId);
    }

    // --- REQ-009: the editor-produced set is runtime-valid (cross-layer) ---

    [Fact] // REQ-009 — a MapProject saved set feeds GameSession.Create and the warp resolves end-to-end
    public void EditorSavedSet_IsRuntimeValid()
    {
        var project = new MapProject("start", 6, 6);
        project.Active.AddEvent(new Cell(3, 2));
        project.Active.UpdateSelected("StepOn", "Warp", new Dictionary<string, string> { ["map"] = "town", ["x"] = "2", ["y"] = "2" });
        project.NewMap("town", 5, 5);
        project.SelectMap("start");

        SavedProject saved = project.Save();
        GameManifest? manifest = GameManifestSerializer.Deserialize(saved.Manifest);
        Assert.NotNull(manifest);

        GameSession s = GameSession.Create(saved.Maps, manifest!.StartMap, new GridPoint(2, 2)).Session!;
        s.MovePlayer(Direction.Right); // (2,2) → the authored StepOn warp at (3,2) → town

        Assert.Equal("town", s.ActiveMapId);
    }

    [Fact] // REQ-005 — an off-map target on a larger map clamps to the (1,1) interior cell (pins the fallback)
    public void Warp_OffMapTargetCell_FallsBackToInterior()
    {
        GameSession s = GameSession.Create(
            new Dictionary<string, string>
            {
                ["start"] = FloorMap(6, 6, WarpData(3, 2, "StepOn", "town", 99, 99)),
                ["town"] = FloorMap(5, 5),
            },
            "start",
            new GridPoint(2, 2)).Session!;

        s.MovePlayer(Direction.Right);

        Assert.Equal(new Point(1, 1), s.Active.Player.Cell);
    }

    [Fact] // a null target map id is a guarded programmer error (not bad data)
    public void WarpEvent_NullMapId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new WarpEvent(GridPoint.Zero, EventTrigger.StepOn, null!, GridPoint.Zero));
    }

    [Fact] // REQ-006 — a null map id to the serializer is a guarded programmer error
    public void Save_NullMapId_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SaveSerializer.Serialize(new GameState(), Point.Zero, Direction.Down, 0UL, null!));
    }

    // --- GameManifest (the multi-map registry manifest) round-trip + totality ---

    [Fact] // REQ-001 — the manifest round-trips the start map + the ids
    public void GameManifest_RoundTrips()
    {
        string json = GameManifestSerializer.Serialize(new GameManifest { StartMap = "start", MapIds = ["start", "town"] });

        GameManifest? back = GameManifestSerializer.Deserialize(json);

        Assert.NotNull(back);
        Assert.Equal("start", back!.StartMap);
        Assert.Equal(2, back.MapIds.Length);
    }

    [Fact] // REQ-001/005 — a manifest with an empty start map → null
    public void GameManifest_EmptyStart_Null()
    {
        string json = GameManifestSerializer.Serialize(new GameManifest { StartMap = string.Empty, MapIds = ["a"] });

        Assert.Null(GameManifestSerializer.Deserialize(json));
    }

    [Fact] // REQ-001/005 — a manifest with null mapIds → null
    public void GameManifest_NullMapIds_Null()
    {
        Assert.Null(GameManifestSerializer.Deserialize("{\"startMap\":\"a\",\"mapIds\":null}"));
    }

    [Fact] // REQ-001/005 — malformed manifest JSON → null, never a throw
    public void GameManifest_Malformed_Null()
    {
        Assert.Null(GameManifestSerializer.Deserialize("{ not valid json"));
    }

    [Fact] // GameSession.Create null-guards its args (a programmer error, not bad data)
    public void Create_NullArgs_Throw()
    {
        Assert.Throws<ArgumentNullException>(() => GameSession.Create(null!, "start", GridPoint.Zero));
        Assert.Throws<ArgumentNullException>(() => GameSession.Create(new Dictionary<string, string>(), null!, GridPoint.Zero));
    }

    [Fact] // REQ-006 — Serialize null-guards the state
    public void Save_NullState_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => SaveSerializer.Serialize(null!, Point.Zero, Direction.Down, 0UL, "m"));
    }

    // --- StartMap's bundled two-map bootstrap (the programmatic fallback the Player boots; covers TownBuild / TownEvents / BuildSession) ---

    [Fact] // the town map is a 16×12 room
    public void StartMap_TownBuild_HasTownDimensions()
    {
        TileMap town = StartMap.TownBuild();

        Assert.Equal(16, town.Width);
        Assert.Equal(12, town.Height);
    }

    [Fact] // the town border is blocking; the interior is floor (pins the build loop)
    public void StartMap_TownBuild_BorderBlocking_InteriorFloor()
    {
        TileMap town = StartMap.TownBuild();

        Assert.True(town.GetTile(new Point(0, 0)).Blocking);
        Assert.False(town.GetTile(new Point(1, 1)).Blocking);
    }

    [Fact] // the far corner is also a blocking border cell (pins the width-1 / height-1 bound)
    public void StartMap_TownBuild_FarCornerBlocking()
    {
        TileMap town = StartMap.TownBuild();

        Assert.True(town.GetTile(new Point(15, 11)).Blocking);
    }

    [Fact] // the town carries one Warp back to the start map
    public void StartMap_TownEvents_WarpsBackToStart()
    {
        EventData warp = Assert.Single(StartMap.TownEvents());

        Assert.Equal("Warp", warp.Kind);
        Assert.Equal("start", warp.Params["map"]);
    }

    [Fact] // the bundled two-map session boots into the start map (covers BuildSession end-to-end)
    public void StartMap_BuildSession_BootsStart()
    {
        GameSession session = StartMap.BuildSession();

        Assert.Equal("start", session.ActiveMapId);
    }

    // --- the .expect parser's Warp arm (gate:13 covers reproduction; this pins the grammar + totality) ---

    [Fact] // the flat Warp("map", x, y) form parses to a Warp outcome
    public void Parser_ParsesFlatWarp()
    {
        ParseResult result = ExpectationParser.Parse("=> Warp(\"town\", 3, 4)");

        Warp warp = Assert.IsType<Warp>(Assert.Single(Assert.Single(result.Rows!).Expected));
        Assert.Equal("town", warp.MapId);
        Assert.Equal(new GridPoint(3, 4), warp.Cell);
    }

    [Theory] // a malformed Warp is a typed ParseError (Rows null), never a throw
    [InlineData("=> Warp(\"town\", (3, 4))")] // the tuple form (the deviation: flat-only)
    [InlineData("=> Warp(town, 3, 4)")]        // an unquoted map id
    [InlineData("=> Warp(\"town\", x, 4)")]    // a non-integer coord
    [InlineData("=> Warp(\"town\", 3)")]        // the wrong arity
    public void Parser_MalformedWarp_IsTypedError(string line)
    {
        ParseResult result = ExpectationParser.Parse(line); // must not throw

        Assert.Null(result.Rows);
        Assert.NotNull(result.Error);
    }
}
