namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The result of deserializing a save string: either a parsed <see cref="Save"/> or a single
/// <see cref="Error"/> reason. A total result — <see cref="SaveSerializer.Deserialize"/> never throws on a
/// malformed input.
/// </summary>
public sealed class SaveLoadResult
{
    private SaveLoadResult(SaveState? save, string? error)
    {
        Save = save;
        Error = error;
    }

    /// <summary>The parsed save when <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public SaveState? Save { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>Whether the load succeeded.</summary>
    public bool Ok => Error is null;

    /// <summary>A successful load carrying <paramref name="save"/>.</summary>
    public static SaveLoadResult Success(SaveState save) => new(save, null);

    /// <summary>A failed load carrying the <paramref name="error"/> reason.</summary>
    public static SaveLoadResult Failure(string error) => new(null, error);
}
