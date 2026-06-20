using System.Collections.Generic;

namespace MonoRpgMaker.Engine.World;

/// <summary>The serializable <c>$data</c> shape of a <see cref="TileMap"/>: its dimensions, row-major cells, and placed events.</summary>
public sealed class TileMapData
{
    /// <summary>Map width in tiles.</summary>
    public int Width { get; set; }

    /// <summary>Map height in tiles.</summary>
    public int Height { get; set; }

    /// <summary>The cells in row-major order (index = <c>(y * Width) + x</c>); length must be <c>Width * Height</c>.</summary>
    public TileData[] Tiles { get; set; } = [];

    /// <summary>The events placed on the map (optional; empty when none).</summary>
    public EventData[] Events { get; set; } = [];

    /// <summary>The catalog name of the tileset the cells draw from (empty when absent — the loader defaults it).</summary>
    public string Tileset { get; set; } = string.Empty;

    /// <summary>The doors placed on the map (optional; empty when none) — each binds a cell's tile to a switch.</summary>
    public DoorData[] Doors { get; set; } = [];
}

/// <summary>The serializable <c>$data</c> shape of a single <see cref="Tile"/>.</summary>
public sealed class TileData
{
    /// <summary>Which tileset image the cell draws.</summary>
    public int TilesetId { get; set; }

    /// <summary>Whether the cell blocks movement.</summary>
    public bool Blocking { get; set; }
}

/// <summary>
/// The serializable <c>$data</c> shape of one placed event: a stable id, its cell, its trigger, and the behaviour
/// (<see cref="Kind"/> + <see cref="Params"/>) bound to it. The runtime materialises this into an
/// <c>IMapEvent</c> through the behaviour registry (D-0024) — placement is data, behaviour is code, bound by kind.
/// </summary>
public sealed class EventData
{
    /// <summary>A stable identifier for this placement.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>The event cell column.</summary>
    public int X { get; set; }

    /// <summary>The event cell row.</summary>
    public int Y { get; set; }

    /// <summary>What activates the event (<c>StepOn</c> or <c>ActionButton</c>).</summary>
    public string Trigger { get; set; } = string.Empty;

    /// <summary>The behaviour kind — a registry key (e.g. <c>ShowText</c>).</summary>
    public string Kind { get; set; } = string.Empty;

    /// <summary>The behaviour's kind-specific parameters (e.g. <c>text</c> for <c>ShowText</c>).</summary>
    public Dictionary<string, string> Params { get; set; } = new();
}

/// <summary>
/// The serializable <c>$data</c> shape of a door: its cell, the switch that opens it, and its closed + open
/// tiles. The runtime binds these to a <c>DoorRule</c> whose tile reflects the switch (D-0026).
/// </summary>
public sealed class DoorData
{
    /// <summary>The door cell column.</summary>
    public int X { get; set; }

    /// <summary>The door cell row.</summary>
    public int Y { get; set; }

    /// <summary>The switch that opens the door — while false the cell shows the closed tile, once true the open tile.</summary>
    public string Switch { get; set; } = string.Empty;

    /// <summary>The tile shown while the door is closed (typically blocking).</summary>
    public TileData ClosedTile { get; set; } = new();

    /// <summary>The tile shown once the door is open (typically walkable).</summary>
    public TileData OpenTile { get; set; } = new();
}
