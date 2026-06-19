using System;
using System.Collections.Generic;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The mutable runtime game state for a play session — for M0, a named boolean
/// switch store plus a named integer counter store (the embryonic <c>$game</c>
/// switches + variables). Switches never set read false; counters never added to
/// read zero.
/// </summary>
public sealed class GameState
{
    // Sorted (Ordinal) so enumeration order is deterministic — the basis for a byte-stable save snapshot, and
    // analyzer-clean (the determinism analyzer permits SortedDictionary, unlike a plain Dictionary).
    private readonly SortedDictionary<string, bool> _switches = new(StringComparer.Ordinal);
    private readonly SortedDictionary<string, int> _counters = new(StringComparer.Ordinal);

    /// <summary>The value of switch <paramref name="key"/>, or false if never set.</summary>
    public bool Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _switches.TryGetValue(key, out var value) && value;
    }

    /// <summary>Set switch <paramref name="key"/> to <paramref name="value"/>.</summary>
    [StateMutator]
    public void Set(string key, bool value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _switches[key] = value;
    }

    /// <summary>The value of counter <paramref name="key"/>, or zero if never added to.</summary>
    public int GetCount(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _counters.TryGetValue(key, out var value) ? value : 0;
    }

    /// <summary>Add <paramref name="amount"/> to counter <paramref name="key"/>.</summary>
    [StateMutator]
    public void Add(string key, int amount)
    {
        ArgumentNullException.ThrowIfNull(key);
        _counters[key] = GetCount(key) + amount;
    }

    /// <summary>The switches in deterministic sorted-key order — the read accessor a save snapshot consumes.</summary>
    public IReadOnlyList<KeyValuePair<string, bool>> SwitchEntries =>
        new List<KeyValuePair<string, bool>>(_switches).AsReadOnly();

    /// <summary>The counters in deterministic sorted-key order — the read accessor a save snapshot consumes.</summary>
    public IReadOnlyList<KeyValuePair<string, int>> CounterEntries =>
        new List<KeyValuePair<string, int>>(_counters).AsReadOnly();

    /// <summary>
    /// Rebuild a fresh <see cref="GameState"/> from saved <paramref name="switches"/> and
    /// <paramref name="counters"/> (the load path). Each counter is added onto a zero baseline, so it reads
    /// back its saved value.
    /// </summary>
    public static GameState Restore(
        IReadOnlyList<KeyValuePair<string, bool>> switches, IReadOnlyList<KeyValuePair<string, int>> counters)
    {
        ArgumentNullException.ThrowIfNull(switches);
        ArgumentNullException.ThrowIfNull(counters);

        var state = new GameState();
        for (var i = 0; i < switches.Count; i++)
        {
            state.Set(switches[i].Key, switches[i].Value);
        }

        for (var i = 0; i < counters.Count; i++)
        {
            state.Add(counters[i].Key, counters[i].Value);
        }

        return state;
    }
}
