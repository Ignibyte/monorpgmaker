using System;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Tracer;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class WorldSimMovementTests
{
    private static WorldSim Sim(TileMap map, Point start, IMapEvent[]? events = null) =>
        new(map, new Actor("Hero", start, maxHp: 10), events ?? Array.Empty<IMapEvent>(), Array.Empty<DoorRule>());

    [Fact] // T1 — REQ-001
    public void MovePlayer_IntoBlockedTile_DoesNotMove()
    {
        var map = new TileMap(3, 3);
        map.SetTile(new Point(1, 0), new Tile(TilesetId: 1, Blocking: true));
        var sim = Sim(map, new Point(1, 1));

        Assert.False(sim.MovePlayer(Direction.Up));
        Assert.Equal(new Point(1, 1), sim.Player.Cell);
    }

    [Fact] // T2 — REQ-001
    public void MovePlayer_IntoOpenTile_Advances()
    {
        var sim = Sim(new TileMap(3, 3), new Point(1, 1));

        Assert.True(sim.MovePlayer(Direction.Up));
        Assert.Equal(new Point(1, 0), sim.Player.Cell);
    }

    [Fact] // T8 — REQ-002 (dispatch precision)
    public void MovePlayer_FiresStepOnEvent_OnlyAtItsCell()
    {
        var sim = Sim(new TileMap(5, 3), new Point(1, 1), new IMapEvent[] { new LeverEvent(new Point(3, 1)) });

        Assert.True(sim.MovePlayer(Direction.Right));   // -> (2,1): not the event cell
        Assert.Null(sim.CurrentMessage);

        Assert.True(sim.MovePlayer(Direction.Right));   // -> (3,1): the event cell
        Assert.NotNull(sim.CurrentMessage);
    }
}

public class TracerLeverTests
{
    [Fact] // T3 — REQ-002
    public void SteppingOnLever_ShowsMessage_AndSetsSwitch()
    {
        var sim = TracerRoom.Build();

        Assert.True(sim.MovePlayer(Direction.Right));   // (2,4) -> (3,4)
        Assert.True(sim.MovePlayer(Direction.Right));   // (3,4) -> (4,4) lever

        var message = sim.CurrentMessage;
        Assert.NotNull(message);
        Assert.Contains("lever", message);
        Assert.True(sim.State.Get(LeverEvent.DoorSwitch));
    }

    [Fact] // T4 — REQ-002 (exactly once)
    public void SteppingOnLeverTwice_ShowsMessageOnlyOnce()
    {
        var sim = TracerRoom.Build();
        Assert.True(sim.MovePlayer(Direction.Right));
        Assert.True(sim.MovePlayer(Direction.Right));   // onto the lever: message shown
        Assert.NotNull(sim.CurrentMessage);

        Assert.True(sim.MovePlayer(Direction.Left));    // step off: message cleared
        Assert.Null(sim.CurrentMessage);

        Assert.True(sim.MovePlayer(Direction.Right));   // back onto the lever
        Assert.Null(sim.CurrentMessage);                // the guard fired once: no second message
    }

    [Fact] // T7 — REQ-002 (message lifecycle)
    public void MovePlayer_ClearsPreviousMessage()
    {
        var sim = TracerRoom.Build();
        Assert.True(sim.MovePlayer(Direction.Right));
        Assert.True(sim.MovePlayer(Direction.Right));   // message shown on the lever
        Assert.NotNull(sim.CurrentMessage);

        Assert.True(sim.MovePlayer(Direction.Up));      // a move that fires no event
        Assert.Null(sim.CurrentMessage);
    }
}

public class TracerDoorTests
{
    private static readonly Point Door = new(7, 4);

    [Fact] // T5 — REQ-003
    public void Door_IsImpassable_UntilLeverPulled_ThenPassable()
    {
        var sim = TracerRoom.Build();
        Assert.True(sim.Map.IsBlocked(Door));           // closed at start

        Assert.True(sim.MovePlayer(Direction.Right));
        Assert.True(sim.MovePlayer(Direction.Right));   // pull the lever -> door opens

        Assert.True(sim.State.Get(LeverEvent.DoorSwitch));
        Assert.False(sim.Map.IsBlocked(Door));          // now passable
    }

    [Fact] // T6 — REQ-003 (idempotent)
    public void SyncDoors_IsIdempotent_OnceOpen()
    {
        var sim = TracerRoom.Build();
        Assert.True(sim.MovePlayer(Direction.Right));
        Assert.True(sim.MovePlayer(Direction.Right));   // open

        sim.SyncDoors();
        sim.SyncDoors();

        Assert.False(sim.Map.IsBlocked(Door));          // still open
    }
}

public class SimGuardTests
{
    [Fact]
    public void WorldSim_Ctor_NullArgs_Throw()
    {
        var map = new TileMap(2, 2);
        var actor = new Actor("Hero", Point.Zero, maxHp: 1);
        var events = Array.Empty<IMapEvent>();
        var doors = Array.Empty<DoorRule>();

        Assert.Throws<ArgumentNullException>(() => new WorldSim(null!, actor, events, doors));
        Assert.Throws<ArgumentNullException>(() => new WorldSim(map, null!, events, doors));
        Assert.Throws<ArgumentNullException>(() => new WorldSim(map, actor, null!, doors));
        Assert.Throws<ArgumentNullException>(() => new WorldSim(map, actor, events, null!));
    }

    [Fact]
    public void EventContext_Ctor_NullArgs_Throw()
    {
        var state = new GameState();
        Assert.Throws<ArgumentNullException>(() => new EventContext(null!, _ => { }));
        Assert.Throws<ArgumentNullException>(() => new EventContext(state, null!));
    }

    [Fact]
    public void GameState_NullKey_Throws()
    {
        var state = new GameState();
        Assert.Throws<ArgumentNullException>(() => state.Get(null!));
        Assert.Throws<ArgumentNullException>(() => state.Set(null!, true));
    }

    [Fact]
    public void GameState_StoredValue_RoundTrips()
    {
        var state = new GameState();

        state.Set("k", false);
        Assert.False(state.Get("k"));   // a stored-false switch reads false

        state.Set("k", true);
        Assert.True(state.Get("k"));
    }
}
