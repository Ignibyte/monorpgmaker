using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>The currently-open shop: the offers the player can buy/sell. (E2 — the host screen — will add a cursor + selection over these.)</summary>
public sealed class ShopState
{
    /// <summary>Create a shop over its <paramref name="offers"/>.</summary>
    public ShopState(IReadOnlyList<ShopOffer> offers)
    {
        ArgumentNullException.ThrowIfNull(offers);
        Offers = offers;
    }

    /// <summary>The lines the shop offers (each an item id + its buy/sell prices).</summary>
    public IReadOnlyList<ShopOffer> Offers { get; }
}
