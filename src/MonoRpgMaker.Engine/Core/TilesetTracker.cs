using System;

namespace MonoRpgMaker.Engine.Core;

/// <summary>
/// Tracks the tileset name the host has currently applied (loaded the sheet for), so the runtime reloads the
/// sheet only when the active map's tileset actually CHANGES — e.g. across a <c>Warp</c> to a map with a different
/// tileset (D-0022: the host owns the art; this is the gated change-detect seam the host polls each frame).
/// Deterministic — a name comparison, no clock or RNG.
/// </summary>
public sealed class TilesetTracker
{
    private string? _applied;

    /// <summary>
    /// Advance to <paramref name="current"/>: returns <see langword="true"/> (and records it as applied) when it
    /// differs from the last-applied name — the host should reload the sheet — or <see langword="false"/> when it
    /// is unchanged (no reload). The first call always returns <see langword="true"/> (nothing applied yet).
    /// </summary>
    public bool TryAdvance(string current)
    {
        ArgumentNullException.ThrowIfNull(current);

        if (string.Equals(_applied, current, StringComparison.Ordinal))
        {
            return false;
        }

        _applied = current;
        return true;
    }
}
