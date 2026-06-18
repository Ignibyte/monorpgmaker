using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// Bridges the host's XNA <see cref="Point"/> and the pure <see cref="GridPoint"/> the
/// published seam uses. The Engine converts at the boundary so the World/Entities layers
/// keep using <see cref="Point"/> while the event seam speaks <see cref="GridPoint"/>.
/// </summary>
public static class GridPointExtensions
{
    /// <summary>The <see cref="GridPoint"/> with the same X/Y as <paramref name="p"/>.</summary>
    public static GridPoint ToGridPoint(this Point p) => new(p.X, p.Y);

    /// <summary>The XNA <see cref="Point"/> with the same X/Y as <paramref name="g"/>.</summary>
    public static Point ToXnaPoint(this GridPoint g) => new(g.X, g.Y);
}
