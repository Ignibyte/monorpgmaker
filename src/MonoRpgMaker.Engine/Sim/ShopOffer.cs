using System;
using System.Collections.Generic;

namespace MonoRpgMaker.Engine.Sim;

/// <summary>One line of a shop's inventory: an item id and its buy + sell prices (in <c>gold</c>).</summary>
public readonly record struct ShopOffer(string ItemId, int BuyPrice, int SellPrice)
{
    /// <summary>
    /// Parse the encoded inventory string <paramref name="offers"/> (<c>id:buy:sell</c> entries, comma-separated —
    /// e.g. <c>"potion:5:2,ether:20:8"</c>) into offers. Total: a malformed entry (wrong arity, empty id, or a
    /// non-integer or negative price) is skipped, never a throw — an empty or all-malformed input yields no offers.
    /// Negative prices are rejected at this boundary so a malformed offer can never invert the economy (a negative
    /// buy price would otherwise <em>add</em> gold).
    /// </summary>
    public static ShopOffer[] Parse(string offers)
    {
        ArgumentNullException.ThrowIfNull(offers);

        var result = new List<ShopOffer>();
        foreach (string entry in offers.Split(','))
        {
            string[] parts = entry.Split(':');
            if (parts.Length == 3
                && !string.IsNullOrEmpty(parts[0])
                && int.TryParse(parts[1], out int buy) && buy >= 0
                && int.TryParse(parts[2], out int sell) && sell >= 0)
            {
                result.Add(new ShopOffer(parts[0], buy, sell));
            }
        }

        return result.ToArray();
    }
}
