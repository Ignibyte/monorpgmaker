using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The mutable runtime game state for a play session — for M0, a named boolean
/// switch store (the embryonic <c>$game</c> state). Switches never set read false.
/// </summary>
public sealed class GameState
{
    private readonly Dictionary<string, bool> _switches = new();

    /// <summary>The value of switch <paramref name="key"/>, or false if never set.</summary>
    public bool Get(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return _switches.TryGetValue(key, out var value) && value;
    }

    /// <summary>Set switch <paramref name="key"/> to <paramref name="value"/>.</summary>
    public void Set(string key, bool value)
    {
        ArgumentNullException.ThrowIfNull(key);
        _switches[key] = value;
    }
}
