using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// The serializable shape of a game's map set (<c>game.json</c>): the id of the map the runtime boots into and
/// every map id in the set (each id has its own <c>$data</c> file). The repo IS the game (D-0023) — the set is
/// bundled embedded content, addressed by stable id.
/// </summary>
public sealed class GameManifest
{
    /// <summary>The id of the map the runtime boots into.</summary>
    public string StartMap { get; set; } = string.Empty;

    /// <summary>Every map id in the set.</summary>
    public string[] MapIds { get; set; } = [];
}

/// <summary>
/// Serializes a <see cref="GameManifest"/> to / from JSON. Total — a malformed input yields <see langword="null"/>
/// (the caller treats it as a typed failure), never an exception on the parse path.
/// </summary>
public static class GameManifestSerializer
{
    /// <summary>Serialize <paramref name="manifest"/> to its JSON string.</summary>
    public static string Serialize(GameManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        return JsonSerializer.Serialize(manifest, GameManifestJsonContext.Default.GameManifest);
    }

    /// <summary>
    /// Parse a JSON <paramref name="json"/> manifest; returns <see langword="null"/> for malformed JSON, a null
    /// document, an empty start map, or a missing map list — never throws on the parse path.
    /// </summary>
    public static GameManifest? Deserialize(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        GameManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize(json, GameManifestJsonContext.Default.GameManifest);
        }
        catch (JsonException)
        {
            return null;
        }

        if (manifest is null || string.IsNullOrEmpty(manifest.StartMap) || manifest.MapIds is null)
        {
            return null;
        }

        return manifest;
    }
}

/// <summary>The System.Text.Json source-generation context for the game manifest (AOT-safe, no reflection).</summary>
[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(GameManifest))]
internal sealed partial class GameManifestJsonContext : JsonSerializerContext
{
}
