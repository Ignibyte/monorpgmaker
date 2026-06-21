using System;
using Microsoft.Xna.Framework;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Engine.Entities;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..004 — the gated shop-SCREEN seam: the `ShopState` cursor (clamped), the `WorldSim` movement-modal guard
/// + `MoveShopCursor`, and the bundled shopkeeper. The render/input/font are host code (E2 manual-smoke).
/// </summary>
public class ShopScreenTests
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
        sim.MovePlayer(Direction.Right); // step onto (2,0) → OpenShop → ActiveShop
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

    // --- REQ-001: the cursor clamps ---

    [Fact] // REQ-001 — Cursor starts 0 + MoveCursor clamps to [0, Offers.Count-1]
    public void Cursor_Clamps()
    {
        var shop = new ShopState([new ShopOffer("a", 1, 1), new ShopOffer("b", 1, 1)]);

        Assert.Equal(0, shop.Cursor);

        shop.MoveCursor(1);
        Assert.Equal(1, shop.Cursor);

        shop.MoveCursor(5);
        Assert.Equal(1, shop.Cursor); // clamped to the last offer

        shop.MoveCursor(-5);
        Assert.Equal(0, shop.Cursor); // clamped to the first offer
    }

    [Fact] // REQ-001 — MoveCursor on an empty shop is a no-op (no throw)
    public void Cursor_EmptyShop_NoOp()
    {
        var shop = new ShopState([]);

        shop.MoveCursor(1);

        Assert.Equal(0, shop.Cursor);
    }

    // --- REQ-002: movement is blocked while shopping ---

    [Fact] // REQ-002 — MovePlayer is a no-op (false, no cell change) while a shop is open
    public void MovePlayer_BlockedWhileShopping()
    {
        WorldSim sim = SimWithShop("potion:5:2");
        Point before = sim.Player.Cell;

        bool moved = sim.MovePlayer(Direction.Up);

        Assert.False(moved);
        Assert.Equal(before, sim.Player.Cell);
    }

    [Fact] // REQ-002 — PressAction is also a no-op while a shop is open (the faced event does NOT fire)
    public void PressAction_BlockedWhileShopping()
    {
        var map = new TileMap(5, 1);
        for (var x = 0; x < 5; x++)
        {
            map.SetTile(new Point(x, 0), new Tile(0, false));
        }

        var player = new Actor("Hero", new Point(1, 0), maxHp: 30);
        var shop = new ShopEvent(new GridPoint(2, 0), EventTrigger.StepOn, "potion:5:2");
        var sign = new ShowTextEvent(new GridPoint(3, 0), EventTrigger.ActionButton, "hello");
        WorldSim sim = WorldSim.TryCreate(map, player, [shop, sign], Array.Empty<DoorRule>()).Sim!;
        sim.MovePlayer(Direction.Right); // step onto (2,0) facing Right → the shop opens; the player now faces (3,0)

        Assert.False(sim.PressAction());  // guarded — does not fire the sign at the faced cell
        Assert.Null(sim.CurrentMessage);  // the faced sign did NOT show
    }

    // --- REQ-003: MoveShopCursor ---

    [Fact] // REQ-003 — MoveShopCursor moves the open shop's cursor
    public void MoveShopCursor_MovesOpenShop()
    {
        WorldSim sim = SimWithShop("potion:5:2,ether:20:8");

        sim.MoveShopCursor(1);

        Assert.Equal(1, sim.ActiveShop!.Cursor);
    }

    [Fact] // REQ-003 — MoveShopCursor with no shop open is a no-op (no throw)
    public void MoveShopCursor_NoShop_NoOp()
    {
        WorldSim sim = SimWithoutShop();

        sim.MoveShopCursor(1);

        Assert.Null(sim.ActiveShop);
    }

    // --- REQ-004: the bundled shopkeeper ---

    [Fact] // REQ-004 — the start map ships a Shop event (the shopkeeper)
    public void StartMap_HasShopkeeper()
    {
        EventData shop = Assert.Single(StartMap.Events(), e => e.Kind == "Shop");

        Assert.Equal("shopkeeper", shop.Id);
        Assert.Equal("potion:5:2,ether:20:8", shop.Params["items"]);
    }
}
