using System;
using System.Collections.Generic;
using System.Linq;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Editor;

/// <summary>
/// A gated multi-map editing project: an ordered set of maps (each a <see cref="MapPaintSession"/>) addressed by
/// a stable id, with a start-map pointer and an active map. Reuses the single-map session (#16/#19/#20/#21) — the
/// project just adds the set + the manifest. Framework-neutral (string ids / <see cref="Cell"/>), so the Studio
/// host drives it without pulling MonoGame/Avalonia into its compile graph. <see cref="Save"/>/<see cref="Load"/>
/// round-trip the manifest + every map's <c>$data</c> — the exact shape the runtime registry reads.
/// </summary>
public sealed class MapProject
{
    private readonly List<string> _ids = new();
    private readonly Dictionary<string, MapPaintSession> _maps = new(StringComparer.Ordinal);

    /// <summary>Start a project with one map <paramref name="firstId"/> over a default-tileset session — also the start map.</summary>
    public MapProject(string firstId, int width, int height)
    {
        ArgumentException.ThrowIfNullOrEmpty(firstId);
        AddSession(firstId, new MapPaintSession(TilesetCatalog.DefaultName, width, height));
        ActiveId = firstId;
        StartMapId = firstId;
    }

    private MapProject()
    {
        ActiveId = string.Empty;
        StartMapId = string.Empty;
    }

    /// <summary>The ids of every map in the set, in insertion order.</summary>
    public IReadOnlyList<string> MapIds => _ids;

    /// <summary>The id of the map currently being edited.</summary>
    public string ActiveId { get; private set; }

    /// <summary>The id of the start map the runtime boots into.</summary>
    public string StartMapId { get; private set; }

    /// <summary>The session for the map currently being edited.</summary>
    public MapPaintSession Active => _maps[ActiveId];

    /// <summary>How many maps are in the set.</summary>
    public int MapCount => _ids.Count;

    /// <summary>
    /// Add a new empty map with id <paramref name="id"/> (a fresh default session) and select it; returns
    /// <see langword="false"/> when the id is empty or already in the set (no duplicate ids).
    /// </summary>
    public bool NewMap(string id, int width, int height)
    {
        if (string.IsNullOrEmpty(id) || _maps.ContainsKey(id))
        {
            return false;
        }

        AddSession(id, new MapPaintSession(TilesetCatalog.DefaultName, width, height));
        ActiveId = id;
        return true;
    }

    /// <summary>Select the map <paramref name="id"/> as active; returns <see langword="false"/> when the id is not in the set.</summary>
    public bool SelectMap(string id)
    {
        if (id is null || !_maps.ContainsKey(id))
        {
            return false;
        }

        ActiveId = id;
        return true;
    }

    /// <summary>Set the start map to <paramref name="id"/>; returns <see langword="false"/> when the id is not in the set.</summary>
    public bool SetStartMap(string id)
    {
        if (id is null || !_maps.ContainsKey(id))
        {
            return false;
        }

        StartMapId = id;
        return true;
    }

    /// <summary>Serialize the whole set: the manifest JSON + each map id → its <c>$data</c> JSON (what the runtime registry reads).</summary>
    public SavedProject Save()
    {
        var maps = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (string id in _ids)
        {
            maps[id] = _maps[id].Save();
        }

        var manifest = new GameManifest { StartMap = StartMapId, MapIds = _ids.ToArray() };
        return new SavedProject(GameManifestSerializer.Serialize(manifest), maps);
    }

    /// <summary>
    /// Load a project from a manifest JSON + each map id → its <c>$data</c> JSON. Returns <see langword="null"/>
    /// when the manifest is malformed, a referenced map's <c>$data</c> is missing/invalid, or the start map is not
    /// in the set — never throws.
    /// </summary>
    public static MapProject? Load(string manifestJson, IReadOnlyDictionary<string, string> maps)
    {
        ArgumentNullException.ThrowIfNull(manifestJson);
        ArgumentNullException.ThrowIfNull(maps);

        GameManifest? manifest = GameManifestSerializer.Deserialize(manifestJson);
        if (manifest is null)
        {
            return null;
        }

        var project = new MapProject();
        foreach (string id in manifest.MapIds)
        {
            if (string.IsNullOrEmpty(id) || project._maps.ContainsKey(id) || !maps.TryGetValue(id, out string? json))
            {
                return null;
            }

            var session = new MapPaintSession(TilesetCatalog.DefaultName, 1, 1);
            if (!session.Load(json).Ok)
            {
                return null;
            }

            project.AddSession(id, session);
        }

        if (project._ids.Count == 0 || !project._maps.ContainsKey(manifest.StartMap))
        {
            return null;
        }

        project.StartMapId = manifest.StartMap;
        project.ActiveId = manifest.StartMap;
        return project;
    }

    private void AddSession(string id, MapPaintSession session)
    {
        _ids.Add(id);
        _maps[id] = session;
    }
}

/// <summary>
/// A serialized <see cref="MapProject"/>: the manifest JSON + each map id → its <c>$data</c> JSON. The host writes
/// these as <c>game.json</c> + per-map files (the runtime then reads them).
/// </summary>
/// <param name="Manifest">The game manifest JSON (start map + map ids).</param>
/// <param name="Maps">Each map id mapped to its <c>$data</c> JSON.</param>
public readonly record struct SavedProject(string Manifest, IReadOnlyDictionary<string, string> Maps);
