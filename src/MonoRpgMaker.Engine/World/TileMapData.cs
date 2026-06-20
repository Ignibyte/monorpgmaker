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
