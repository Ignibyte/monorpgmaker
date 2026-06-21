using System;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>
/// The pure buy/sell economy over a <see cref="GameState"/>: gold is the <see cref="GoldCounter"/> counter, items
/// are <c>item.&lt;id&gt;</c> counters (the GiveItem/Chest convention). Bounds-checked + deterministic — a failed
/// transaction leaves state untouched (D-0017: state math, no rendering).
/// </summary>
public static class ShopModel
{
    /// <summary>The game-state counter key for the player's currency.</summary>
    public const string GoldCounter = "gold";

    /// <summary>Buy one of <paramref name="offer"/> against <paramref name="state"/>: if the player can afford it, deduct the buy price + grant the item; otherwise a typed failure (no state change).</summary>
    public static ShopResult Buy(GameState state, ShopOffer offer)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.GetCount(GoldCounter) < offer.BuyPrice)
        {
            return ShopResult.Failure("not enough gold");
        }

        state.Add(GoldCounter, -offer.BuyPrice);
        state.Add("item." + offer.ItemId, 1);
        return ShopResult.Success();
    }

    /// <summary>Sell one of <paramref name="offer"/> against <paramref name="state"/>: if the player owns at least one, remove it + grant the sell price; otherwise a typed failure (no state change).</summary>
    public static ShopResult Sell(GameState state, ShopOffer offer)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (state.GetCount("item." + offer.ItemId) < 1)
        {
            return ShopResult.Failure("nothing to sell");
        }

        state.Add("item." + offer.ItemId, -1);
        state.Add(GoldCounter, offer.SellPrice);
        return ShopResult.Success();
    }
}
