namespace MonoRpgMaker.Engine.World;

/// <summary>A single map cell: which tileset image it draws and whether it blocks movement.</summary>
public readonly record struct Tile(int TilesetId, bool Blocking)
{
    /// <summary>The sentinel "nothing here" tile.</summary>
    public static Tile Empty => new(-1, false);

    /// <summary>True when this cell draws nothing.</summary>
    public bool IsEmpty => TilesetId < 0;
}
