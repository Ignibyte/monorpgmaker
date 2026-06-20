using System.Collections.Generic;

namespace MonoRpgMaker.Engine.World;

/// <summary>
/// The result of deserializing a map <c>$data</c> string: either a loaded <see cref="Map"/> (with its placed
/// <see cref="Events"/>) or a single <see cref="Error"/> reason. A total result —
/// <see cref="MapSerializer.Deserialize"/> never throws on a malformed input.
/// </summary>
public sealed class MapLoadResult
{
    private MapLoadResult(TileMap? map, IReadOnlyList<EventData> events, string? error)
    {
        Map = map;
        Events = events;
        Error = error;
    }

    /// <summary>The loaded map when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public TileMap? Map { get; }

    /// <summary>The loaded placed events when <see cref="Ok"/>; otherwise empty.</summary>
    public IReadOnlyList<EventData> Events { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the load succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful load carrying <paramref name="map"/> and its <paramref name="events"/>.</summary>
    public static MapLoadResult Success(TileMap map, IReadOnlyList<EventData> events) => new(map, events, null);

    /// <summary>A failed load carrying the <paramref name="error"/> reason.</summary>
    public static MapLoadResult Failure(string error) => new(null, [], error);
}
