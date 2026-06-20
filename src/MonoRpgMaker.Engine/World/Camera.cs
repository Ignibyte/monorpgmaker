using System;
using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// Pure view-offset math for a player-following camera. Framework-thin (integer pixels only) so it is
/// deterministic and unit-testable; the host applies the offset as a render translation.
/// </summary>
public static class Camera
{
    /// <summary>
    /// The top-left view offset (in pixels) that centres <paramref name="playerPixel"/> in a
    /// <paramref name="viewportSize"/> view, clamped so the view never scrolls past a map of
    /// <paramref name="mapPixelSize"/>. When the map is no larger than the viewport on an axis, that axis stays
    /// at 0 (no scroll).
    /// </summary>
    public static Point ViewOffset(Point playerPixel, Point viewportSize, Point mapPixelSize)
    {
        var maxX = Math.Max(0, mapPixelSize.X - viewportSize.X);
        var maxY = Math.Max(0, mapPixelSize.Y - viewportSize.Y);
        var x = Math.Clamp(playerPixel.X - (viewportSize.X / 2), 0, maxX);
        var y = Math.Clamp(playerPixel.Y - (viewportSize.Y / 2), 0, maxY);
        return new Point(x, y);
    }
}
