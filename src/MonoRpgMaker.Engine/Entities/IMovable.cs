using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>Anything that occupies a tile and can attempt grid movement.</summary>
public interface IMovable
{
    /// <summary>The tile the entity currently stands on.</summary>
    Point Cell { get; }

    /// <summary>The direction the entity is facing.</summary>
    Direction Facing { get; }

    /// <summary>Turn to face <paramref name="direction"/> and step one tile if unblocked.</summary>
    bool TryStep(Direction direction, TileMap map);
}
