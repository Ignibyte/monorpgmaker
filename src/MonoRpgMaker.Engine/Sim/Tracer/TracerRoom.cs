using Microsoft.Xna.Framework;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.World;

namespace MonoRpgMaker.Engine.Sim.Tracer;

/// <summary>
/// Builds the M0 tracer room: a walled room split by an interior wall with a
/// switch-driven door, a lever, and the player. The hand-authored content slice that
/// proves the authoring loop. (M1 relocates authored content like this to the Project.)
/// </summary>
public static class TracerRoom
{
    /// <summary>Walkable floor.</summary>
    public static readonly Tile Floor = new(TilesetId: 0, Blocking: false);

    /// <summary>Solid wall.</summary>
    public static readonly Tile Wall = new(TilesetId: 1, Blocking: true);

    /// <summary>The lever the player steps on.</summary>
    public static readonly Tile Lever = new(TilesetId: 2, Blocking: false);

    /// <summary>The closed (impassable) door.</summary>
    public static readonly Tile DoorClosed = new(TilesetId: 3, Blocking: true);

    /// <summary>The open (passable) door.</summary>
    public static readonly Tile DoorOpen = new(TilesetId: 4, Blocking: false);

    /// <summary>Assemble the configured simulation for the tracer slice.</summary>
    public static WorldSim Build()
    {
        const int width = 15;
        const int height = 9;
        const int wallX = 7;
        const int doorRow = 4;

        var map = new TileMap(width, height);
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var border = x == 0 || y == 0 || x == width - 1 || y == height - 1;
                map.SetTile(new Point(x, y), border ? Wall : Floor);
            }
        }

        // Interior wall down x = wallX, with a genuine gap at the door row. The door
        // cell is owned solely by the DoorRule below (SyncDoors sets it closed first).
        for (var y = 1; y < height - 1; y++)
        {
            if (y != doorRow)
                map.SetTile(new Point(wallX, y), Wall);
        }

        var leverCell = new Point(4, doorRow);
        var doorCell = new Point(wallX, doorRow);
        map.SetTile(leverCell, Lever);

        var player = new Actor("Hero", new Point(2, doorRow), maxHp: 30);
        var events = new IMapEvent[] { new LeverEvent(leverCell) };
        var doors = new[] { new DoorRule(doorCell, LeverEvent.DoorSwitch, DoorClosed, DoorOpen) };

        return new WorldSim(map, player, events, doors);
    }
}
