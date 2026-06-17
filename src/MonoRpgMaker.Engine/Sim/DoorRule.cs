using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// Binds a door cell's passability to a game-state switch: while the switch is
/// false the cell is <see cref="ClosedTile"/>; once true it becomes <see cref="OpenTile"/>.
/// </summary>
public readonly record struct DoorRule(Point Cell, string Switch, Tile ClosedTile, Tile OpenTile);
