namespace MonoRpgMaker.Engine.World;

/// <summary>The serializable <c>$data</c> shape of a <see cref="TileMap"/>: its dimensions and row-major cells.</summary>
public sealed class TileMapData
{
    /// <summary>Map width in tiles.</summary>
    public int Width { get; set; }

    /// <summary>Map height in tiles.</summary>
    public int Height { get; set; }

    /// <summary>The cells in row-major order (index = <c>(y * Width) + x</c>); length must be <c>Width * Height</c>.</summary>
    public TileData[] Tiles { get; set; } = [];
}

/// <summary>The serializable <c>$data</c> shape of a single <see cref="Tile"/>.</summary>
public sealed class TileData
{
    /// <summary>Which tileset image the cell draws.</summary>
    public int TilesetId { get; set; }

    /// <summary>Whether the cell blocks movement.</summary>
    public bool Blocking { get; set; }
}
