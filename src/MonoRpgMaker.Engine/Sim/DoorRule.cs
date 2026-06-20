using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// Binds a door cell's passability to a game-state switch: while the switch is
/// false the cell is <see cref="ClosedTile"/>; once true it becomes <see cref="OpenTile"/>.
/// </summary>
public readonly record struct DoorRule(Point Cell, string Switch, Tile ClosedTile, Tile OpenTile)
{
    /// <summary>Convert serializable <see cref="DoorData"/> placements (from a map's <c>$data</c>) into door rules — the loader→sim bridge (D-0026). The caller has already validated the doors (the deserializer rejects off-map / switchless ones).</summary>
    public static DoorRule[] FromData(IReadOnlyList<DoorData> doors)
    {
        ArgumentNullException.ThrowIfNull(doors);

        var rules = new DoorRule[doors.Count];
        for (var i = 0; i < doors.Count; i++)
        {
            DoorData d = doors[i];
            rules[i] = new DoorRule(
                new Point(d.X, d.Y),
                d.Switch,
                new Tile(d.ClosedTile.TilesetId, d.ClosedTile.Blocking),
                new Tile(d.OpenTile.TilesetId, d.OpenTile.Blocking));
        }

        return rules;
    }
}
