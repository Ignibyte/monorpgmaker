using System;
using System.Collections.Generic;
using System.Text.Json;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Tracer;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

public class SaveSerializerTests
{
    private static GameState Populate()
    {
        var state = new GameState();
        state.Set("door_open", true);
        state.Set("chest_opened", false);
        state.Add("gold", 42);
        state.Add("potions", 3);
        return state;
    }

    [Fact] // T1 (REQ-001) — Serialize captures every field
    public void Serialize_CapturesAllFields()
    {
        string json = SaveSerializer.Serialize(Populate(), new Point(3, 4), Direction.Left, 0xDEADBEEFUL);

        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal(2, root.GetProperty("switches").GetArrayLength());
        Assert.Equal(2, root.GetProperty("counters").GetArrayLength());
        Assert.Equal(3, root.GetProperty("playerX").GetInt32());
        Assert.Equal(4, root.GetProperty("playerY").GetInt32());
        Assert.Equal((int)Direction.Left, root.GetProperty("facing").GetInt32());
        Assert.Equal(0xDEADBEEFUL, root.GetProperty("rngState").GetUInt64());
    }

    [Fact] // T2 (REQ-002) — output is insertion-order independent (deterministic)
    public void Serialize_IsInsertionOrderIndependent()
    {
        var a = new GameState();
        a.Set("zebra", true);
        a.Set("apple", false);
        a.Add("mango", 1);

        var b = new GameState();
        b.Add("mango", 1);
        b.Set("apple", false);
        b.Set("zebra", true);

        string ja = SaveSerializer.Serialize(a, Point.Zero, Direction.Down, 0UL);
        string jb = SaveSerializer.Serialize(b, Point.Zero, Direction.Down, 0UL);

        Assert.Equal(ja, jb);
    }

    [Fact] // T3 (REQ-003) — round-trip restores every value
    public void RoundTrip_RestoresEveryValue()
    {
        SaveLoadResult result = SaveSerializer.Deserialize(
            SaveSerializer.Serialize(Populate(), Point.Zero, Direction.Down, 0UL));

        Assert.True(result.Ok);
        GameState restored = GameState.Restore(Map(result.Save!.Switches), Map(result.Save.Counters));
        Assert.True(restored.Get("door_open"));
        Assert.False(restored.Get("chest_opened"));
        Assert.Equal(42, restored.GetCount("gold"));
        Assert.Equal(3, restored.GetCount("potions"));
        Assert.False(restored.Get("never_set"));        // absent switch default
        Assert.Equal(0, restored.GetCount("never_added")); // absent counter default
    }

    [Theory] // T4 (REQ-004) — malformed (incl. the inspect null-entry/key fix) → typed Failure, never a throw
    [InlineData("{bad")]
    [InlineData("null")]
    [InlineData("{\"version\":999,\"switches\":[],\"counters\":[]}")]
    [InlineData("{\"version\":1,\"switches\":null,\"counters\":[]}")]
    [InlineData("{\"version\":1,\"switches\":[],\"counters\":null}")]
    [InlineData("{\"version\":1,\"switches\":[null],\"counters\":[]}")]
    [InlineData("{\"version\":1,\"switches\":[{\"key\":null,\"value\":true}],\"counters\":[]}")]
    [InlineData("{\"version\":1,\"switches\":[],\"counters\":[null]}")]
    [InlineData("{\"version\":1,\"switches\":[],\"counters\":[{\"key\":null,\"value\":3}]}")]
    public void Deserialize_Malformed_FailsWithoutThrowing(string json)
    {
        SaveLoadResult? result = null;

        Assert.Null(Record.Exception(() => result = SaveSerializer.Deserialize(json)));
        Assert.False(result!.Ok);
        Assert.Null(result.Save);
    }

    [Fact] // T5 (REQ-005) — the saved RNG state reproduces the generator's continuation
    public void RngState_RoundTrips_ReproducesContinuation()
    {
        var original = new SplitMix64Random(123UL);
        original.NextBits();
        original.NextBits();
        ulong captured = original.State;

        SaveLoadResult result = SaveSerializer.Deserialize(
            SaveSerializer.Serialize(new GameState(), Point.Zero, Direction.Down, captured));
        Assert.True(result.Ok);
        var restored = new SplitMix64Random(result.Save!.RngState);

        Assert.Equal(original.NextBits(), restored.NextBits());
        Assert.Equal(original.NextBits(), restored.NextBits());
        Assert.Equal(original.NextBits(), restored.NextBits());
    }

    [Fact] // T6 (REQ-006) — save mid-sim, restore into a fresh sim → replay-equivalent (the door re-opens)
    public void Save_Restore_IntoFreshSim_IsReplayEquivalent()
    {
        WorldSim a = TracerRoom.Build();
        a.MovePlayer(Direction.Right);
        a.MovePlayer(Direction.Right);   // step onto the lever → DoorSwitch set, the door opens
        Assert.True(a.State.Get(LeverEvent.DoorSwitch));

        SaveLoadResult result = SaveSerializer.Deserialize(
            SaveSerializer.Serialize(a.State, Point.Zero, Direction.Down, 0UL));
        Assert.True(result.Ok);

        WorldSim b = TracerRoom.Build();   // fresh — its door is closed
        foreach (SwitchEntry entry in result.Save!.Switches)
        {
            b.State.Set(entry.Key, entry.Value);
        }

        b.SyncDoors();

        Assert.True(b.State.Get(LeverEvent.DoorSwitch));
        AssertMapsEqual(a.Map, b.Map);
    }

    [Fact] // T7 (REQ-002) — SwitchEntries are sorted (Ordinal) + can't be cast to a mutable List
    public void SwitchEntries_AreSorted_AndImmutable()
    {
        var state = new GameState();
        state.Set("z", true);
        state.Set("a", true);
        state.Set("m", true);

        IReadOnlyList<KeyValuePair<string, bool>> entries = state.SwitchEntries;

        Assert.Equal("a", entries[0].Key);
        Assert.Equal("m", entries[1].Key);
        Assert.Equal("z", entries[2].Key);
        Assert.Throws<InvalidCastException>(() => (List<KeyValuePair<string, bool>>)entries);
    }

    private static IReadOnlyList<KeyValuePair<string, bool>> Map(SwitchEntry[] entries)
    {
        var list = new List<KeyValuePair<string, bool>>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            list.Add(new KeyValuePair<string, bool>(entries[i].Key, entries[i].Value));
        }

        return list;
    }

    private static IReadOnlyList<KeyValuePair<string, int>> Map(CounterEntry[] entries)
    {
        var list = new List<KeyValuePair<string, int>>(entries.Length);
        for (var i = 0; i < entries.Length; i++)
        {
            list.Add(new KeyValuePair<string, int>(entries[i].Key, entries[i].Value));
        }

        return list;
    }

    private static void AssertMapsEqual(TileMap expected, TileMap actual)
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
