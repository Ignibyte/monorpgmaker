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

    /// <summary>The selected offer index (the buy/sell cursor), 0-based; starts at the first offer.</summary>
    public int Cursor { get; private set; }

    /// <summary>Move the cursor by <paramref name="delta"/>, clamped to the offer range; a no-op when the shop has no offers.</summary>
    public void MoveCursor(int delta)
    {
        if (Offers.Count == 0)
        {
            return;
        }

        Cursor = Math.Clamp(Cursor + delta, 0, Offers.Count - 1);
    }
}
