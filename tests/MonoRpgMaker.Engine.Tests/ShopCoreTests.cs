using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..005 — the gated shop core: the OpenShop outcome → ActiveShop, the pure ShopModel Buy/Sell economy,
/// the WorldSim Buy/Sell index wrapper + CloseShop, and the Shop built-in + ShopOffer.Parse totality. Isolated
/// asserts (the host screen + input are E2).
/// </summary>
public class ShopCoreTests
{
    private static WorldSim SimWithShop(string offers)
    {
        var map = new TileMap(5, 1);
        for (var x = 0; x < 5; x++)
        {
            map.SetTile(new Point(x, 0), new Tile(0, false));
        }

        var player = new Actor("Hero", new Point(1, 0), maxHp: 30);
        var shop = new ShopEvent(new GridPoint(2, 0), EventTrigger.StepOn, offers);
        WorldSim sim = WorldSim.TryCreate(map, player, [shop], Array.Empty<DoorRule>()).Sim!;
        sim.MovePlayer(Direction.Right); // step onto (2,0) → StepOn → OpenShop → ActiveShop
        return sim;
    }

    private static WorldSim SimWithoutShop()
    {
        var map = new TileMap(3, 1);
        for (var x = 0; x < 3; x++)
        {
            map.SetTile(new Point(x, 0), new Tile(0, false));
        }

        var player = new Actor("Hero", new Point(1, 0), maxHp: 30);
        return WorldSim.TryCreate(map, player, [], Array.Empty<DoorRule>()).Sim!;
    }

    // --- REQ-001: an applied OpenShop sets ActiveShop to the parsed offers ---

    [Fact] // REQ-001 — stepping onto a Shop event opens it with its parsed offers
    public void Shop_Opens_WithParsedOffers()
    {
        WorldSim sim = SimWithShop("potion:5:2,ether:20:8");

        Assert.NotNull(sim.ActiveShop);
        Assert.Equal(2, sim.ActiveShop!.Offers.Count);
        Assert.Equal("potion", sim.ActiveShop.Offers[0].ItemId);
        Assert.Equal(5, sim.ActiveShop.Offers[0].BuyPrice);
        Assert.Equal(2, sim.ActiveShop.Offers[0].SellPrice);
        Assert.Equal("ether", sim.ActiveShop.Offers[1].ItemId);
    }

    // --- REQ-002: ShopModel.Buy ---

    [Fact] // REQ-002 — affordable buy deducts gold + grants the item
    public void Buy_Affordable_DeductsGoldGrantsItem()
    {
        var state = new GameState();
        state.Add("gold", 10);

        ShopResult result = ShopModel.Buy(state, new ShopOffer("potion", 5, 2));

        Assert.True(result.Ok);
        Assert.Equal(5, state.GetCount("gold"));
        Assert.Equal(1, state.GetCount("item.potion"));
    }

    [Fact] // REQ-002 — an unaffordable buy leaves state unchanged + fails
    public void Buy_Unaffordable_NoChange()
    {
        var state = new GameState();
        state.Add("gold", 1);

        ShopResult result = ShopModel.Buy(state, new ShopOffer("potion", 5, 2));

        Assert.False(result.Ok);
        Assert.Equal(1, state.GetCount("gold"));
        Assert.Equal(0, state.GetCount("item.potion"));
    }

    // --- REQ-003: ShopModel.Sell ---

    [Fact] // REQ-003 — selling an owned item removes one + grants the sell price
    public void Sell_Owned_RemovesItemGrantsGold()
    {
        var state = new GameState();
        state.Add("item.potion", 1);

        ShopResult result = ShopModel.Sell(state, new ShopOffer("potion", 5, 2));

        Assert.True(result.Ok);
        Assert.Equal(0, state.GetCount("item.potion"));
        Assert.Equal(2, state.GetCount("gold"));
    }

    [Fact] // REQ-003 — selling what you don't own leaves state unchanged + fails
    public void Sell_NotOwned_NoChange()
    {
        var state = new GameState();

        ShopResult result = ShopModel.Sell(state, new ShopOffer("potion", 5, 2));

        Assert.False(result.Ok);
        Assert.Equal(0, state.GetCount("item.potion"));
        Assert.Equal(0, state.GetCount("gold"));
    }

    // --- REQ-004: the Shop built-in + Parse totality ---

    [Fact] // REQ-004 — ShopEvent.Run returns a single OpenShop carrying its offers
    public void ShopEvent_Run_ReturnsOpenShop()
    {
        var ev = new ShopEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion:5:2");

        Outcome outcome = Assert.Single(ev.Run(new EventContext(new GameState())));

        Assert.Equal("potion:5:2", Assert.IsType<OpenShop>(outcome).Offers);
    }

    [Fact] // REQ-004 — a Shop placement materialises into a ShopEvent
    public void Registry_MaterialisesShop()
    {
        var data = new EventData { Id = "s", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Shop", Params = { ["items"] = "potion:5:2" } };

        BehaviourResult result = BehaviourRegistry.TryMaterialize(data);

        Assert.True(result.Ok);
        Assert.IsType<ShopEvent>(result.Event);
    }

    [Fact] // REQ-004 — a Shop missing 'items' → typed failure
    public void Registry_Shop_MissingItems_Fails()
    {
        var data = new EventData { Id = "s", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Shop" };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — Parse is total: pure garbage yields no offers (no throw)
    public void Parse_Garbage_Empty()
    {
        Assert.Empty(ShopOffer.Parse("garbage"));
    }

    [Fact] // REQ-004 — Parse skips malformed entries, keeps the valid ones
    public void Parse_Mixed_KeepsValidOnly()
    {
        ShopOffer[] offers = ShopOffer.Parse("potion:5:2,bad,ether:x:8");

        ShopOffer only = Assert.Single(offers);
        Assert.Equal("potion", only.ItemId);
        Assert.Equal(5, only.BuyPrice);
        Assert.Equal(2, only.SellPrice);
    }

    [Fact] // REQ-004 — Parse rejects negative prices (a negative buy/sell would invert the economy)
    public void Parse_NegativePrice_Skipped()
    {
        Assert.Empty(ShopOffer.Parse("potion:-5:2"));
        Assert.Empty(ShopOffer.Parse("potion:5:-3"));
    }

    // --- REQ-002/003/005: the WorldSim Buy/Sell index wrapper + CloseShop ---

    [Fact] // REQ-002 — WorldSim.Buy delegates to the open shop's offer
    public void WorldSim_Buy_Delegates()
    {
        WorldSim sim = SimWithShop("potion:5:2");
        sim.State.Add("gold", 10);

        Assert.True(sim.Buy(0).Ok);
        Assert.Equal(5, sim.State.GetCount("gold"));
        Assert.Equal(1, sim.State.GetCount("item.potion"));
    }

    [Fact] // REQ-002 — an out-of-range offer index → typed failure
    public void WorldSim_Buy_BadIndex_Fails()
    {
        WorldSim sim = SimWithShop("potion:5:2");

        Assert.False(sim.Buy(5).Ok);
        Assert.False(sim.Buy(-1).Ok);
    }

    [Fact] // REQ-002 — Buy with no shop open → typed failure
    public void WorldSim_Buy_NoShop_Fails()
    {
        WorldSim sim = SimWithoutShop();

        Assert.Null(sim.ActiveShop);
        Assert.False(sim.Buy(0).Ok);
    }

    [Fact] // REQ-005 — CloseShop clears the open shop
    public void WorldSim_CloseShop_ClearsActiveShop()
    {
        WorldSim sim = SimWithShop("potion:5:2");
        Assert.NotNull(sim.ActiveShop);

        sim.CloseShop();

        Assert.Null(sim.ActiveShop);
    }
}
