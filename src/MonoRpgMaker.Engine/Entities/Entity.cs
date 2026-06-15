using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Entities;

/// <summary>Base for everything that lives on the map grid.</summary>
public abstract class Entity : IMovable
{
    /// <summary>Place a named entity on a starting cell, facing down.</summary>
    protected Entity(string name, Point cell)
    {
        Name = name;
        Cell = cell;
        Facing = Direction.Down;
    }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <inheritdoc />
    public Point Cell { get; private set; }

    /// <inheritdoc />
    public Direction Facing { get; private set; }

    /// <inheritdoc />
    public bool TryStep(Direction direction, TileMap map)
    {
        Facing = direction;
        var target = Cell + direction.ToStep();
        if (map.IsBlocked(target))
            return false;

        Cell = target;
        return true;
    }
}
