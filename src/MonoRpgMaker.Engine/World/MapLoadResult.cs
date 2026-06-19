namespace MonoRpgMaker.Engine.World;

/// <summary>
/// The result of deserializing a map <c>$data</c> string: either a loaded <see cref="Map"/> or a single
/// <see cref="Error"/> reason. A total result — <see cref="MapSerializer.Deserialize"/> never throws on a
/// malformed input.
/// </summary>
public sealed class MapLoadResult
{
    private MapLoadResult(TileMap? map, string? error)
    {
        Map = map;
        Error = error;
    }

    /// <summary>The loaded map when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public TileMap? Map { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the load succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful load carrying <paramref name="map"/>.</summary>
    public static MapLoadResult Success(TileMap map) => new(map, null);

    /// <summary>A failed load carrying the <paramref name="error"/> reason.</summary>
    public static MapLoadResult Failure(string error) => new(null, error);
}
