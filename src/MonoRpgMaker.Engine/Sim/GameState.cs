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
    private readonly Dictionary<string, bool> _switches = new();
    private readonly Dictionary<string, int> _counters = new();

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
}
