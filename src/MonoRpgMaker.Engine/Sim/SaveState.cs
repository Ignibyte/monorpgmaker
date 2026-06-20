namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The serializable snapshot of the deterministic runtime (<c>$game</c>) state — switches + counters (in
/// sorted key order), the player position + facing, and the RNG state. Restoring it reproduces the saved
/// continuation (D-0016).
/// </summary>
public sealed class SaveState
{
    /// <summary>The save-format version (for future migrations).</summary>
    public int Version { get; set; }

    /// <summary>The switch entries, in sorted-key order.</summary>
    public SwitchEntry[] Switches { get; set; } = [];

    /// <summary>The counter entries, in sorted-key order.</summary>
    public CounterEntry[] Counters { get; set; } = [];

    /// <summary>The player's tile X.</summary>
    public int PlayerX { get; set; }

    /// <summary>The player's tile Y.</summary>
    public int PlayerY { get; set; }

    /// <summary>The player's facing (the <c>Direction</c> enum value).</summary>
    public int Facing { get; set; }

    /// <summary>The captured RNG generator state — restores an identical continuation.</summary>
    public ulong RngState { get; set; }

    /// <summary>The id of the map the player is on (empty = the start map; back-compat for pre-multi-map saves).</summary>
    public string MapId { get; set; } = string.Empty;
}

/// <summary>A saved switch: its key and boolean value.</summary>
public sealed class SwitchEntry
{
    /// <summary>The switch key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The switch value.</summary>
    public bool Value { get; set; }
}

/// <summary>A saved counter: its key and integer value.</summary>
public sealed class CounterEntry
{
    /// <summary>The counter key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>The counter value.</summary>
    public int Value { get; set; }
}
