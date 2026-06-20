using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..005 — doors from <c>$data</c>: the round-trip, `GameSession` syncing a door tile per its switch, the
/// `Lever` built-in (give-once switch-setter), totality, and the bundled lever→door demo. Isolated asserts.
/// </summary>
public class DoorsTests
{
    private static IEventContext Context(GameState state) => new EventContext(state);

    private static TileMap Floor(int w, int h)
    {
        var map = new TileMap(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                map.SetTile(new Point(x, y), new Tile(0, false));
            }
        }

        return map;
    }

    private static DoorData Door(int x, int y, string sw) =>
        new()
        {
            X = x,
            Y = y,
            Switch = sw,
            ClosedTile = new TileData { TilesetId = 1, Blocking = true },
            OpenTile = new TileData { TilesetId = 2, Blocking = false },
        };

    private static GameSession SessionWithDoor()
    {
        string start = MapSerializer.Serialize(Floor(6, 6), Array.Empty<EventData>(), [Door(3, 3, "gate")]);
        return GameSession.Create(new Dictionary<string, string> { ["start"] = start }, "start", new GridPoint(1, 1)).Session!;
    }

    // --- REQ-001: $data round-trip ---

    [Fact] // REQ-001 — a door round-trips through serialize → deserialize
    public void Door_RoundTrips()
    {
        string json = MapSerializer.Serialize(Floor(5, 5), Array.Empty<EventData>(), [Door(3, 4, "gate")]);

        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.True(result.Ok);
        DoorData door = Assert.Single(result.Doors);
        Assert.Equal(3, door.X);
        Assert.Equal(4, door.Y);
        Assert.Equal("gate", door.Switch);
        Assert.True(door.ClosedTile.Blocking);
        Assert.False(door.OpenTile.Blocking);
    }

    // --- REQ-002: GameSession syncs the door tile per its switch ---

    [Fact] // REQ-002 — the door cell is the CLOSED (blocking) tile while the switch is unset
    public void GameSession_DoorClosed_WhenSwitchUnset()
    {
        GameSession s = SessionWithDoor();

        Assert.True(s.Active.Map.GetTile(new Point(3, 3)).Blocking);
    }

    [Fact] // REQ-002 — setting the switch + a step opens the door (the open, walkable tile)
    public void GameSession_DoorOpens_WhenSwitchSet()
    {
        GameSession s = SessionWithDoor();
        s.Active.State.Set("gate", true);

        s.MovePlayer(Direction.Right); // any step re-syncs the doors

        Assert.False(s.Active.Map.GetTile(new Point(3, 3)).Blocking);
    }

    // --- REQ-003: the Lever built-in ---

    [Fact] // REQ-003 — Lever (switch unset) sets the switch + shows a message
    public void Lever_Run_SetsSwitchAndMessage()
    {
        var ev = new SwitchEvent(GridPoint.Zero, EventTrigger.ActionButton, "gate", "open!");

        IReadOnlyList<Outcome> outcomes = ev.Run(Context(new GameState()));

        Assert.Equal(2, outcomes.Count);
        SetSwitch sw = Assert.IsType<SetSwitch>(outcomes[0]);
        Assert.Equal("gate", sw.Key);
        Assert.True(sw.Value);
    }

    [Fact] // REQ-003 — Lever does nothing once the switch is set (give-once)
    public void Lever_Run_GiveOnce()
    {
        var ev = new SwitchEvent(GridPoint.Zero, EventTrigger.ActionButton, "gate", "open!");
        var state = new GameState();
        state.Set("gate", true);

        Assert.Empty(ev.Run(Context(state)));
    }

    [Fact] // REQ-003 — a Lever placement materialises into a SwitchEvent
    public void Registry_MaterialisesLever()
    {
        var data = new EventData { Id = "l", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Lever", Params = { ["switch"] = "gate", ["message"] = "open" } };

        BehaviourResult result = BehaviourRegistry.TryMaterialize(data);

        Assert.True(result.Ok);
        Assert.IsType<SwitchEvent>(result.Event);
    }

    // --- REQ-004: totality ---

    [Fact] // REQ-004 — an off-map door → typed MapLoadResult.Failure
    public void Door_OffMap_Fails()
    {
        string json = MapSerializer.Serialize(Floor(5, 5), Array.Empty<EventData>(), [Door(9, 9, "gate")]);

        Assert.False(MapSerializer.Deserialize(json).Ok);
    }

    [Fact] // REQ-004 — a door with no switch → typed failure
    public void Door_NoSwitch_Fails()
    {
        string json = MapSerializer.Serialize(Floor(5, 5), Array.Empty<EventData>(), [Door(2, 2, "")]);

        Assert.False(MapSerializer.Deserialize(json).Ok);
    }

    [Fact] // REQ-004 — a Lever missing 'switch' → BehaviourResult.Failure
    public void Registry_Lever_MissingSwitch_Fails()
    {
        var data = new EventData { Id = "l", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Lever", Params = { ["message"] = "open" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Theory] // REQ-004 — an off-map door on ANY axis/edge → typed failure (kills the per-axis bound mutants)
    [InlineData(5, 2)]   // X == Width
    [InlineData(2, 5)]   // Y == Height
    [InlineData(-1, 2)]  // X < 0
    [InlineData(2, -1)]  // Y < 0
    public void Door_OffMapAxis_Fails(int x, int y)
    {
        string json = MapSerializer.Serialize(Floor(5, 5), Array.Empty<EventData>(), [Door(x, y, "gate")]);

        Assert.False(MapSerializer.Deserialize(json).Ok);
    }

    [Fact] // REQ-004 — a door with a null tile → typed failure
    public void Door_NullTile_Fails()
    {
        var door = new DoorData { X = 2, Y = 2, Switch = "gate", ClosedTile = null!, OpenTile = new TileData() };
        string json = MapSerializer.Serialize(Floor(5, 5), Array.Empty<EventData>(), [door]);

        Assert.False(MapSerializer.Deserialize(json).Ok);
    }

    [Fact] // REQ-004 — a Lever missing 'message' → BehaviourResult.Failure
    public void Registry_Lever_MissingMessage_Fails()
    {
        var data = new EventData { Id = "l", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Lever", Params = { ["switch"] = "gate" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-001 — a map $data with NO doors key loads with empty doors (back-compat; kills the `?? []` mutant)
    public void Deserialize_NoDoorsKey_EmptyDoors()
    {
        const string json = "{\"width\":2,\"height\":2,\"tiles\":[{\"tilesetId\":0,\"blocking\":false},{\"tilesetId\":0,\"blocking\":false},{\"tilesetId\":0,\"blocking\":false},{\"tilesetId\":0,\"blocking\":false}],\"events\":[],\"tileset\":\"lpc-mountains\"}";

        MapLoadResult result = MapSerializer.Deserialize(json);

        Assert.True(result.Ok);
        Assert.Empty(result.Doors);
    }

    // --- REQ-005: the bundled lever→door demo ---

    [Fact] // REQ-005 — the bundled start map carries a door, closed at boot
    public void BundledStart_HasClosedDoor()
    {
        GameSession s = StartMap.BuildSession();

        Assert.True(s.Active.Map.GetTile(new Point(10, 9)).Blocking);
    }

    [Fact] // REQ-005 — setting the bundled door's switch opens it (end-to-end through GameSession)
    public void BundledStart_DoorOpens()
    {
        GameSession s = StartMap.BuildSession();
        s.Active.State.Set("door_open", true);

        s.MovePlayer(Direction.Down); // re-sync

        Assert.False(s.Active.Map.GetTile(new Point(10, 9)).Blocking);
    }
}
