using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..003 — <see cref="GameSession.TryRestore"/>: rebuild the session in place at the saved map with the
/// restored <see cref="GameState"/> + player position; total on a bad save (an unknown map id → false, no
/// corruption); and the full SaveSerializer round-trip. Isolated exact-value asserts.
/// </summary>
public class SaveLoadRestoreTests
{
    private static string FloorMap(int w, int h)
    {
        var map = new TileMap(w, h);
        for (var y = 0; y < h; y++)
        {
            for (var x = 0; x < w; x++)
            {
                map.SetTile(new Point(x, y), new Tile(0, false));
            }
        }

        return MapSerializer.Serialize(map, Array.Empty<EventData>());
    }

    private static GameSession TwoMapSession() =>
        GameSession.Create(
            new Dictionary<string, string> { ["start"] = FloorMap(8, 8), ["town"] = FloorMap(6, 6) },
            "start",
            new GridPoint(1, 1)).Session!;

    private static SaveState Save(string mapId, int x, int y) =>
        new() { Version = SaveSerializer.CurrentVersion, MapId = mapId, PlayerX = x, PlayerY = y };

    // --- REQ-001: restore the saved map + position + state ---

    [Fact] // REQ-001 — restore switches the active map to the saved id
    public void TryRestore_SwitchesToSavedMap()
    {
        GameSession s = TwoMapSession();

        Assert.True(s.TryRestore(Save("town", 3, 4)));
        Assert.Equal("town", s.ActiveMapId);
    }

    [Fact] // REQ-001 — and places the player at the saved cell
    public void TryRestore_PlacesPlayerAtSavedCell()
    {
        GameSession s = TwoMapSession();

        s.TryRestore(Save("town", 3, 4));

        Assert.Equal(new Point(3, 4), s.Active.Player.Cell);
    }

    [Fact] // REQ-001 — and restores a saved switch
    public void TryRestore_RestoresSwitch()
    {
        GameSession s = TwoMapSession();
        var save = new SaveState
        {
            Version = SaveSerializer.CurrentVersion,
            MapId = "town",
            PlayerX = 2,
            PlayerY = 2,
            Switches = [new SwitchEntry { Key = "door", Value = true }],
        };

        s.TryRestore(save);

        Assert.True(s.Active.State.Get("door"));
    }

    [Fact] // REQ-001 — and restores a saved counter
    public void TryRestore_RestoresCounter()
    {
        GameSession s = TwoMapSession();
        var save = new SaveState
        {
            Version = SaveSerializer.CurrentVersion,
            MapId = "town",
            PlayerX = 2,
            PlayerY = 2,
            Counters = [new CounterEntry { Key = "gold", Value = 7 }],
        };

        s.TryRestore(save);

        Assert.Equal(7, s.Active.State.GetCount("gold"));
    }

    // --- REQ-002: totality + no corruption on a bad save ---

    [Fact] // REQ-002 — an unknown saved map id → false
    public void TryRestore_UnknownMap_ReturnsFalse()
    {
        GameSession s = TwoMapSession();

        Assert.False(s.TryRestore(Save("ghost", 1, 1)));
    }

    [Fact] // REQ-002 — and leaves the active map unchanged
    public void TryRestore_UnknownMap_ActiveMapUnchanged()
    {
        GameSession s = TwoMapSession();

        s.TryRestore(Save("ghost", 1, 1));

        Assert.Equal("start", s.ActiveMapId);
    }

    [Fact] // REQ-002 — and does not corrupt the prior state (no mutation before the success check)
    public void TryRestore_Failed_PreservesPriorState()
    {
        GameSession s = TwoMapSession();
        s.Active.State.Set("flag", true);

        s.TryRestore(Save("ghost", 1, 1));

        Assert.True(s.Active.State.Get("flag"));
    }

    // --- REQ-003: the full SaveSerializer round-trip ---

    [Fact] // REQ-003 — Serialize → Deserialize → TryRestore restores the active map id
    public void RoundTrip_RestoresMap()
    {
        SaveState save = SavedTownState();

        GameSession restored = TwoMapSession();
        Assert.True(restored.TryRestore(save));
        Assert.Equal("town", restored.ActiveMapId);
    }

    [Fact] // REQ-003 — and the switch + counter survive the round-trip
    public void RoundTrip_RestoresState()
    {
        SaveState save = SavedTownState();

        GameSession restored = TwoMapSession();
        restored.TryRestore(save);

        Assert.True(restored.Active.State.Get("door"));
        Assert.Equal(5, restored.Active.State.GetCount("gold"));
    }

    [Fact] // restores MULTIPLE switches/counters by key→value (kills loop-bound + value-constant mutants)
    public void TryRestore_RestoresMultipleEntries()
    {
        GameSession s = TwoMapSession();
        var save = new SaveState
        {
            Version = SaveSerializer.CurrentVersion,
            MapId = "town",
            PlayerX = 2,
            PlayerY = 2,
            Switches =
            [
                new SwitchEntry { Key = "a", Value = true },
                new SwitchEntry { Key = "b", Value = false },
                new SwitchEntry { Key = "c", Value = true },
            ],
            Counters =
            [
                new CounterEntry { Key = "x", Value = 11 },
                new CounterEntry { Key = "y", Value = 22 },
                new CounterEntry { Key = "z", Value = 33 },
            ],
        };

        s.TryRestore(save);

        Assert.True(s.Active.State.Get("a"));
        Assert.False(s.Active.State.Get("b"));
        Assert.True(s.Active.State.Get("c"));
        Assert.Equal(11, s.Active.State.GetCount("x"));
        Assert.Equal(22, s.Active.State.GetCount("y"));
        Assert.Equal(33, s.Active.State.GetCount("z"));
    }

    [Fact] // a null save is a guarded programmer error
    public void TryRestore_Null_Throws()
    {
        GameSession s = TwoMapSession();

        Assert.Throws<ArgumentNullException>(() => s.TryRestore(null!));
    }

    // Build a "town" save by exercising the real serializer over a source session's state (Serialize → Deserialize).
    private static SaveState SavedTownState()
    {
        GameSession source = TwoMapSession();
        source.TryRestore(new SaveState
        {
            Version = SaveSerializer.CurrentVersion,
            MapId = "town",
            PlayerX = 2,
            PlayerY = 3,
            Switches = [new SwitchEntry { Key = "door", Value = true }],
            Counters = [new CounterEntry { Key = "gold", Value = 5 }],
        });

        string json = SaveSerializer.Serialize(
            source.Active.State, source.Active.Player.Cell, Direction.Down, 0UL, source.ActiveMapId);
        SaveLoadResult result = SaveSerializer.Deserialize(json);
        Assert.True(result.Ok);
        return result.Save!;
    }
}
