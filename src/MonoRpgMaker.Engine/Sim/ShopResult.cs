namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The result of a buy/sell attempt: either <see cref="Ok"/>, or a single <see cref="Error"/> reason
/// (unaffordable / nothing to sell / no such offer). A total result — a failed transaction never throws and never
/// mutates state.
/// </summary>
public sealed class ShopResult
{
    private ShopResult(bool ok, string? error)
    {
        Ok = ok;
        Error = error;
    }

    /// <summary>Whether the transaction succeeded.</summary>
    public bool Ok { get; }

    /// <summary>The failure reason when not <see cref="Ok"/>; otherwise <see langword="null"/>.</summary>
    public string? Error { get; }

    /// <summary>A successful transaction.</summary>
    public static ShopResult Success() => new(true, null);

    /// <summary>A failed transaction carrying the <paramref name="error"/> reason.</summary>
    public static ShopResult Failure(string error) => new(false, error);
}
