using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class HookDispatchTests
{
    // ---- HookSchedule.Build — ambiguity (REQ-005) ------------------------------------------------

    [Fact]
    public void Build_TwoEvents_SameCellTriggerOrder_IsAmbiguous()
    {
        IMapEvent[] events =
        {
            Event(2, 3, EventTrigger.StepOn, order: 0),
            Event(2, 3, EventTrigger.StepOn, order: 0),
        };

        ScheduleResult result = HookSchedule.Build(events);

        Assert.False(result.Ok);
        Assert.Contains("ambiguous", result.Error!, StringComparison.Ordinal);
    }

    [Fact] // control — same order, DIFFERENT cell (kills the Cell-equality conjunct)
    public void Build_SameOrder_DifferentCell_IsOk()
    {
        IMapEvent[] events = { Event(1, 1, EventTrigger.StepOn, 0), Event(2, 1, EventTrigger.StepOn, 0) };

        Assert.True(HookSchedule.Build(events).Ok);
    }

    [Fact] // control — same cell + order, DIFFERENT trigger (kills the Trigger-equality conjunct)
    public void Build_SameCellOrder_DifferentTrigger_IsOk()
    {
        IMapEvent[] events = { Event(1, 1, EventTrigger.StepOn, 0), Event(1, 1, EventTrigger.ActionButton, 0) };

        Assert.True(HookSchedule.Build(events).Ok);
    }

    [Fact] // control — same cell + trigger, DIFFERENT order (kills the Order-equality conjunct)
    public void Build_SameCellTrigger_DifferentOrder_IsOk()
    {
        IMapEvent[] events = { Event(1, 1, EventTrigger.StepOn, 0), Event(1, 1, EventTrigger.StepOn, 1) };

        Assert.True(HookSchedule.Build(events).Ok);
    }

    [Fact]
    public void Build_ThreeEvents_SameCellTriggerOrder_IsAmbiguous()
    {
        IMapEvent[] events =
        {
            Event(1, 1, EventTrigger.StepOn, 0),
            Event(1, 1, EventTrigger.StepOn, 0),
            Event(1, 1, EventTrigger.StepOn, 0),
        };

        Assert.False(HookSchedule.Build(events).Ok);
    }

    [Fact]
    public void Build_EmptySet_IsOk()
    {
        Assert.True(HookSchedule.Build(Array.Empty<IMapEvent>()).Ok);
    }

    [Fact]
    public void Build_Null_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => HookSchedule.Build(null!));
    }

    // ---- HookSchedule.EventsAt — ordering + filter (REQ-004) --------------------------------------

    [Fact]
    public void EventsAt_ReturnsAscendingOrder()
    {
        IMapEvent[] events =
        {
            Event(2, 1, EventTrigger.StepOn, 2),
            Event(2, 1, EventTrigger.StepOn, 0),
            Event(2, 1, EventTrigger.StepOn, 1),
        };
        HookSchedule schedule = HookSchedule.Build(events).Schedule!;

        IReadOnlyList<IMapEvent> at = schedule.EventsAt(new GridPoint(2, 1), EventTrigger.StepOn);

        Assert.Equal(3, at.Count);
        Assert.Equal(0, at[0].Order);
        Assert.Equal(1, at[1].Order);
        Assert.Equal(2, at[2].Order);
    }

    [Fact]
    public void EventsAt_UnknownCell_IsEmpty()
    {
        HookSchedule schedule = HookSchedule.Build(new IMapEvent[] { Event(1, 1, EventTrigger.StepOn, 0) }).Schedule!;

        Assert.Empty(schedule.EventsAt(new GridPoint(9, 9), EventTrigger.StepOn));
    }

    [Fact] // filters on cell AND trigger (kills both filter conjuncts)
    public void EventsAt_FiltersCellAndTrigger()
    {
        IMapEvent[] events =
        {
            Event(1, 1, EventTrigger.StepOn, 0),       // wanted
            Event(1, 1, EventTrigger.ActionButton, 0), // wrong trigger
            Event(2, 1, EventTrigger.StepOn, 0),       // wrong cell
        };
        HookSchedule schedule = HookSchedule.Build(events).Schedule!;

        IReadOnlyList<IMapEvent> at = schedule.EventsAt(new GridPoint(1, 1), EventTrigger.StepOn);

        Assert.Single(at);
        Assert.Equal(EventTrigger.StepOn, at[0].Trigger);
        Assert.Equal(new GridPoint(1, 1), at[0].Cell);
    }

    // ---- WorldSim — ordered dispatch + ambiguity (REQ-004/005) ------------------------------------

    [Fact]
    public void MovePlayer_DispatchesStepOnEvents_InAscendingOrder()
    {
        var log = new List<string>();
        IMapEvent[] events =
        {
            Event(2, 1, EventTrigger.StepOn, 2, log),
            Event(2, 1, EventTrigger.StepOn, 0, log),
            Event(2, 1, EventTrigger.StepOn, 1, log),
        };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events);

        sim.MovePlayer(Direction.Right);   // → (2,1)

        Assert.Equal(3, log.Count);
        Assert.Equal("0", log[0]);
        Assert.Equal("1", log[1]);
        Assert.Equal("2", log[2]);
    }

    [Fact]
    public void TryCreate_AmbiguousEvents_ReturnsFailure()
    {
        IMapEvent[] events = { Event(2, 1, EventTrigger.StepOn, 0), Event(2, 1, EventTrigger.StepOn, 0) };

        WorldSimResult result = WorldSim.TryCreate(
            new TileMap(3, 3), new Actor("Hero", new Point(1, 1), 10), events, Array.Empty<DoorRule>());

        Assert.False(result.Ok);
        Assert.Null(result.Sim);
        Assert.Contains("ambiguous", result.Error!, StringComparison.Ordinal);
    }

    // ---- ActionButton / PressAction (REQ-001/003) ------------------------------------------------

    [Fact] // (a) an ActionButton event at the faced cell fires
    public void PressAction_FiresActionButtonEvent_AtFacedCell()
    {
        var log = new List<string>();
        // player at (1,1) faces Down by default → faced cell (1,2)
        IMapEvent[] events = { Event(1, 2, EventTrigger.ActionButton, 0, log) };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events);

        bool fired = sim.PressAction();

        Assert.True(fired);
        Assert.Single(log);
    }

    [Fact] // (b) a StepOn event at the faced cell is NOT fired by PressAction
    public void PressAction_DoesNotFireStepOnEvent()
    {
        var log = new List<string>();
        IMapEvent[] events = { Event(1, 2, EventTrigger.StepOn, 0, log) };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events);

        bool fired = sim.PressAction();

        Assert.False(fired);
        Assert.Empty(log);
    }

    [Fact] // (c) a step does NOT fire an ActionButton event
    public void MovePlayer_DoesNotFireActionButtonEvent()
    {
        var log = new List<string>();
        IMapEvent[] events = { Event(2, 1, EventTrigger.ActionButton, 0, log) };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events);

        sim.MovePlayer(Direction.Right);   // steps onto (2,1)

        Assert.Empty(log);
    }

    [Fact] // (d) faced-cell offset — an ActionButton on the player's OWN cell does NOT fire (kills the + Facing.ToStep())
    public void PressAction_IgnoresEventOnPlayersOwnCell()
    {
        var log = new List<string>();
        IMapEvent[] events = { Event(1, 1, EventTrigger.ActionButton, 0, log) };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events);

        bool fired = sim.PressAction();

        Assert.False(fired);
        Assert.Empty(log);
    }

    // ---- Facing on a blocked move (REQ-002) ------------------------------------------------------

    [Fact]
    public void MovePlayer_BlockedMove_StillTurnsToFace()
    {
        var map = new TileMap(3, 3);
        map.SetTile(new Point(1, 0), new Tile(1, true));   // wall above (1,1)
        WorldSim sim = BuildSim(map, new Point(1, 1), Array.Empty<IMapEvent>());

        bool moved = sim.MovePlayer(Direction.Up);

        Assert.False(moved);
        Assert.Equal(Direction.Up, sim.Player.Facing);
    }

    // ---- PressAction re-syncs doors (REQ-003; kills the SyncDoors-in-PressAction mutant) ----------

    private static readonly Tile DoorClosed = new(3, Blocking: true);
    private static readonly Tile DoorOpen = new(4, Blocking: false);

    [Fact]
    public void PressAction_ActionButtonSetsSwitch_ReSyncsDoors()
    {
        var doorCell = new Point(0, 0);
        IMapEvent[] events =
        {
            // at the faced cell (1,2); opens the door switch when pressed
            Event(1, 2, EventTrigger.ActionButton, 0, log: null, new SetSwitch("door", true)),
        };
        DoorRule[] doors = { new(doorCell, "door", DoorClosed, DoorOpen) };
        WorldSim sim = BuildSim(new TileMap(3, 3), new Point(1, 1), events, doors);

        Assert.Equal(DoorClosed, sim.Map.GetTile(doorCell));   // control — closed before pressing

        sim.PressAction();

        Assert.Equal(DoorOpen, sim.Map.GetTile(doorCell));     // re-synced open after the action-button event
    }

    // ---- EventTrigger.ActionButton exists (REQ-001) ----------------------------------------------

    [Fact]
    public void EventTrigger_HasActionButton()
    {
        Assert.True(Enum.IsDefined(EventTrigger.ActionButton));
    }

    // ---- helpers ---------------------------------------------------------------------------------

    private static RecordingEvent Event(
        int x, int y, EventTrigger trigger, int order, List<string>? log = null, params Outcome[] outcomes) =>
        new(new GridPoint(x, y), trigger, order, order.ToString(System.Globalization.CultureInfo.InvariantCulture), log, outcomes);

    private static WorldSim BuildSim(TileMap map, Point start, IMapEvent[] events, DoorRule[]? doors = null)
    {
        WorldSimResult result = WorldSim.TryCreate(
            map, new Actor("Hero", start, maxHp: 10), events, doors ?? Array.Empty<DoorRule>());
        Assert.True(result.Ok, result.Error);
        return result.Sim!;
    }

    private sealed class RecordingEvent : IMapEvent
    {
        private readonly string _label;
        private readonly List<string>? _log;
        private readonly IReadOnlyList<Outcome> _outcomes;

        public RecordingEvent(GridPoint cell, EventTrigger trigger, int order, string label, List<string>? log, IReadOnlyList<Outcome> outcomes)
        {
            Cell = cell;
            Trigger = trigger;
            Order = order;
            _label = label;
            _log = log;
            _outcomes = outcomes;
        }

        public GridPoint Cell { get; }
        public EventTrigger Trigger { get; }
        public int Order { get; }

        public IReadOnlyList<Outcome> Run(IEventContext context)
        {
            _log?.Add(_label);
            return _outcomes;
        }
    }
}
