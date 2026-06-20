using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// One committed tileset sheet a map may use: a stable <paramref name="Name"/> (the value stored in a map's
/// <c>$data</c> tileset reference), the <paramref name="ResourceFile"/> the hosts load (the Studio
/// <c>avares</c> asset and the Player embedded resource share this file name), and its square
/// <paramref name="TileSize"/> in pixels. Framework-neutral (no XNA / Avalonia) so the Studio host binds it
/// across the neutral boundary (D-0022).
/// </summary>
/// <param name="Name">The stable catalog name (e.g. <c>lpc-grass</c>) — the <c>$data</c> tileset reference.</param>
/// <param name="ResourceFile">The sheet's file name (e.g. <c>lpc-grass.png</c>), loaded by each host.</param>
/// <param name="TileSize">The square tile edge length in pixels.</param>
public readonly record struct TilesetInfo(string Name, string ResourceFile, int TileSize);

/// <summary>
/// The single source of the tileset sheets a map may use. The Studio picker, the <see cref="MapSerializer"/>
/// load-time validation, and the runtime sheet selection all read this one list, so the editor and runtime
/// cannot drift (the behaviour-registry single-descriptor pattern, D-0024). The four committed LPC sheets
/// (CC-BY-SA, <c>assets/tilesets/lpc/</c>) are all 32-pixel tiles; geometry (columns / count) stays derived
/// from each loaded sheet's real pixels by the hosts, so this carries identity only, not dimensions.
/// </summary>
public static class TilesetCatalog
{
    /// <summary>The square tile edge length shared by the committed LPC sheets, in pixels.</summary>
    public const int LpcTileSize = 32;

    /// <summary>The canonical default sheet — the value a map with no <c>$data</c> tileset reference falls back to.</summary>
    public const string DefaultName = "lpc-mountains";

    /// <summary>Every selectable tileset, the default listed first.</summary>
    public static IReadOnlyList<TilesetInfo> All { get; } =
    [
        new TilesetInfo(DefaultName, "lpc-mountains.png", LpcTileSize),
        new TilesetInfo("lpc-grass", "lpc-grass.png", LpcTileSize),
        new TilesetInfo("lpc-dirt", "lpc-dirt.png", LpcTileSize),
        new TilesetInfo("lpc-water", "lpc-water.png", LpcTileSize),
    ];

    /// <summary>Whether <paramref name="name"/> is a known catalog sheet.</summary>
    public static bool Contains(string name) => TryGet(name, out _);

    /// <summary>
    /// Look up the <see cref="TilesetInfo"/> for <paramref name="name"/>. Returns <see langword="false"/> (with a
    /// default <paramref name="info"/>) when the name is <see langword="null"/> or not in the catalog.
    /// </summary>
    public static bool TryGet(string name, out TilesetInfo info)
    {
        foreach (TilesetInfo candidate in All)
        {
            if (string.Equals(candidate.Name, name, StringComparison.Ordinal))
            {
                info = candidate;
                return true;
            }
        }

        info = default;
        return false;
    }

    /// <summary>The default sheet's <see cref="TilesetInfo"/> — the catalog's first entry (its <see cref="TilesetInfo.Name"/> is <see cref="DefaultName"/>; pinned by tests).</summary>
    public static TilesetInfo Default => All[0];

    /// <summary>
    /// Resolve <paramref name="name"/> to its sheet <see cref="TilesetInfo.ResourceFile"/>, falling back to the
    /// <see cref="Default"/> sheet's file when the name is not in the catalog. The single place a tileset name is
    /// turned into a sheet file — the editor host and the runtime both call it, so they cannot drift.
    /// </summary>
    public static string ResolveResourceFile(string name) =>
        TryGet(name, out TilesetInfo info) ? info.ResourceFile : Default.ResourceFile;
}
