using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// Serializes the deterministic runtime (<c>$game</c>) state to / from a JSON save string. Pure +
/// framework-thin (the host owns file IO) and deterministic (sorted keys ⇒ byte-stable output).
/// <see cref="Deserialize"/> is total — a malformed input yields a typed <see cref="SaveLoadResult"/>.
/// </summary>
public static class SaveSerializer
{
    /// <summary>The current save-format version.</summary>
    public const int CurrentVersion = 1;

    /// <summary>Serialize the game state + player placement + RNG state to a JSON save string.</summary>
    public static string Serialize(GameState state, Point playerCell, Direction facing, ulong rngState)
    {
        ArgumentNullException.ThrowIfNull(state);

        IReadOnlyList<KeyValuePair<string, bool>> switchEntries = state.SwitchEntries;
        var switches = new SwitchEntry[switchEntries.Count];
        for (var i = 0; i < switches.Length; i++)
        {
            switches[i] = new SwitchEntry { Key = switchEntries[i].Key, Value = switchEntries[i].Value };
        }

        IReadOnlyList<KeyValuePair<string, int>> counterEntries = state.CounterEntries;
        var counters = new CounterEntry[counterEntries.Count];
        for (var i = 0; i < counters.Length; i++)
        {
            counters[i] = new CounterEntry { Key = counterEntries[i].Key, Value = counterEntries[i].Value };
        }

        var save = new SaveState
        {
            Version = CurrentVersion,
            Switches = switches,
            Counters = counters,
            PlayerX = playerCell.X,
            PlayerY = playerCell.Y,
            Facing = (int)facing,
            RngState = rngState,
        };

        return JsonSerializer.Serialize(save, SaveJsonContext.Default.SaveState);
    }

    /// <summary>
    /// Parse a JSON save <paramref name="json"/> string. Returns a typed <see cref="SaveLoadResult.Failure"/>
    /// for malformed JSON, an unsupported version, or missing entry arrays — it never throws on the parse path.
    /// </summary>
    public static SaveLoadResult Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        SaveState? save;
        try
        {
            save = JsonSerializer.Deserialize(json, SaveJsonContext.Default.SaveState);
        }
        catch (JsonException ex)
        {
            return SaveLoadResult.Failure("invalid JSON: " + ex.Message);
        }

        if (save is null)
        {
            return SaveLoadResult.Failure("the save document was null");
        }

        if (save.Version != CurrentVersion)
        {
            return SaveLoadResult.Failure("unsupported save version " + save.Version);
        }

        if (save.Switches is null)
        {
            return SaveLoadResult.Failure("switches are missing");
        }

        if (save.Counters is null)
        {
            return SaveLoadResult.Failure("counters are missing");
        }

        // The arrays exist, but an untrusted/hand-edited save can still hold a null element or a null key
        // (STJ honours an explicit JSON null over the non-null DTO default). Reject them here so the Ok result
        // is safe for the consumer to restore — otherwise the null would crash GameState.Restore (totality is
        // an end-to-end promise, not just the parse path).
        for (var i = 0; i < save.Switches.Length; i++)
        {
            if (save.Switches[i] is null || save.Switches[i].Key is null)
            {
                return SaveLoadResult.Failure("a switch entry was malformed");
            }
        }

        for (var i = 0; i < save.Counters.Length; i++)
        {
            if (save.Counters[i] is null || save.Counters[i].Key is null)
            {
                return SaveLoadResult.Failure("a counter entry was malformed");
            }
        }

        return SaveLoadResult.Success(save);
    }
}

/// <summary>The System.Text.Json source-generation context for the save DTOs (AOT-safe, no reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SaveState))]
internal sealed partial class SaveJsonContext : JsonSerializerContext
{
}
