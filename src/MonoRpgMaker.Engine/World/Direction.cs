using Microsoft.Xna.Framework;

namespace MonoRpgMaker.Engine.World;

/// <summary>Cardinal facing for actors and the player, RPG Maker style.</summary>
public enum Direction
{
    Down,
    Left,
    Right,
    Up,
}

/// <summary>Helpers that turn a <see cref="Direction"/> into grid movement.</summary>
public static class DirectionExtensions
{
    /// <summary>The unit grid step for a facing, in tile coordinates.</summary>
    public static Point ToStep(this Direction direction) => direction switch
    {
        Direction.Down => new Point(0, 1),
        Direction.Left => new Point(-1, 0),
        Direction.Right => new Point(1, 0),
        Direction.Up => new Point(0, -1),
        _ => Point.Zero,
    };
}
