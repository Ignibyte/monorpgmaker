using System.Collections.Generic;
using MonoRpgMaker.Abstractions;
using MonoRpgMaker.Editor;
using MonoRpgMaker.Engine.Sim;
using MonoRpgMaker.Engine.Sim.Events;
using MonoRpgMaker.Engine.World;
using Xunit;

namespace MonoRpgMaker.Engine.Tests;

/// <summary>
/// REQ-001..005 — the placeable built-in library (GiveItem + Chest): the behaviours' returned outcomes, the
/// registry materialisation + totality, and the inspector's ParamKeys (single-source). Isolated exact-value asserts.
/// </summary>
public class BuiltinLibraryTests
{
    private static IEventContext Context(GameState state) => new EventContext(state);

    // --- REQ-001: GiveItem ---

    [Fact] // REQ-001 — GiveItem grants the item count (an item.<id> counter)
    public void GiveItem_Run_GrantsItemCounter()
    {
        var ev = new GiveItemEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 2, "msg");

        IReadOnlyList<Outcome> outcomes = ev.Run(Context(new GameState()));

        AddCounter add = Assert.IsType<AddCounter>(outcomes[0]);
        Assert.Equal("item.potion", add.Key);
        Assert.Equal(2, add.Amount);
    }

    [Fact] // REQ-001 — and shows the configured message
    public void GiveItem_Run_ShowsMessage()
    {
        var ev = new GiveItemEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 2, "You found a potion!");

        IReadOnlyList<Outcome> outcomes = ev.Run(Context(new GameState()));

        Assert.Equal("You found a potion!", Assert.IsType<ShowMessage>(outcomes[1]).Text);
    }

    // --- REQ-002: Chest give-once ---

    [Fact] // REQ-002 — a first open grants the item + sets the switch + shows the message (3 outcomes)
    public void Chest_FirstOpen_GrantsAndOpens()
    {
        var ev = new ContainerEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 1, "chest_opened", "You open the chest.");

        IReadOnlyList<Outcome> outcomes = ev.Run(Context(new GameState()));

        Assert.Equal(3, outcomes.Count);
        Assert.Equal("item.potion", Assert.IsType<AddCounter>(outcomes[0]).Key);
        SetSwitch sw = Assert.IsType<SetSwitch>(outcomes[1]);
        Assert.Equal("chest_opened", sw.Key);
        Assert.True(sw.Value);
    }

    [Fact] // REQ-002 — a re-open (the switch already set) shows only the empty message (give-once)
    public void Chest_Reopen_IsEmpty()
    {
        var ev = new ContainerEvent(GridPoint.Zero, EventTrigger.ActionButton, "potion", 1, "chest_opened", "You open the chest.");
        var state = new GameState();
        state.Set("chest_opened", true);

        IReadOnlyList<Outcome> outcomes = ev.Run(Context(state));

        Assert.Equal("The chest is empty.", Assert.IsType<ShowMessage>(Assert.Single(outcomes)).Text);
    }

    // --- REQ-003: registry materialisation ---

    [Fact] // REQ-003 — a GiveItem placement materialises into a GiveItemEvent
    public void Registry_MaterialisesGiveItem()
    {
        var data = new EventData { Id = "g", X = 1, Y = 1, Trigger = "ActionButton", Kind = "GiveItem", Params = { ["item"] = "potion", ["amount"] = "1", ["message"] = "hi" } };

        BehaviourResult result = BehaviourRegistry.TryMaterialize(data);

        Assert.True(result.Ok);
        Assert.IsType<GiveItemEvent>(result.Event);
    }

    [Fact] // REQ-003 — a Chest placement materialises into a ContainerEvent
    public void Registry_MaterialisesChest()
    {
        var data = new EventData { Id = "c", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Chest", Params = { ["item"] = "potion", ["amount"] = "1", ["switch"] = "chest_opened", ["message"] = "hi" } };

        BehaviourResult result = BehaviourRegistry.TryMaterialize(data);

        Assert.True(result.Ok);
        Assert.IsType<ContainerEvent>(result.Event);
    }

    // --- REQ-004: totality ---

    [Fact] // REQ-004 — GiveItem missing 'item' → typed failure
    public void Registry_GiveItem_MissingItem_Fails()
    {
        var data = new EventData { Id = "g", X = 1, Y = 1, Trigger = "ActionButton", Kind = "GiveItem", Params = { ["amount"] = "1", ["message"] = "hi" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — GiveItem non-integer 'amount' → typed failure
    public void Registry_GiveItem_NonIntAmount_Fails()
    {
        var data = new EventData { Id = "g", X = 1, Y = 1, Trigger = "ActionButton", Kind = "GiveItem", Params = { ["item"] = "potion", ["amount"] = "lots", ["message"] = "hi" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — Chest missing 'switch' → typed failure
    public void Registry_Chest_MissingSwitch_Fails()
    {
        var data = new EventData { Id = "c", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Chest", Params = { ["item"] = "potion", ["amount"] = "1", ["message"] = "hi" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    [Fact] // REQ-004 — Chest non-integer 'amount' → typed failure (kills the Chest TryParse mutant)
    public void Registry_Chest_NonIntAmount_Fails()
    {
        var data = new EventData { Id = "c", X = 1, Y = 1, Trigger = "ActionButton", Kind = "Chest", Params = { ["item"] = "potion", ["amount"] = "lots", ["switch"] = "chest_opened", ["message"] = "hi" } };

        Assert.False(BehaviourRegistry.TryMaterialize(data).Ok);
    }

    // --- REQ-005: the inspector single-source (ParamKeys) ---

    [Fact] // REQ-005 — GiveItem's param keys (the kind-aware inspector renders these)
    public void AvailableKinds_GiveItem_ParamKeys()
    {
        BehaviourKindInfo kind = Assert.Single(MapPaintSession.AvailableKinds, k => k.Name == "GiveItem");

        Assert.Equal(3, kind.ParamKeys.Count);
        Assert.Contains("item", kind.ParamKeys);
        Assert.Contains("amount", kind.ParamKeys);
        Assert.Contains("message", kind.ParamKeys);
    }

    [Fact] // REQ-005 — Chest's param keys
    public void AvailableKinds_Chest_ParamKeys()
    {
        BehaviourKindInfo kind = Assert.Single(MapPaintSession.AvailableKinds, k => k.Name == "Chest");

        Assert.Equal(4, kind.ParamKeys.Count);
        Assert.Contains("item", kind.ParamKeys);
        Assert.Contains("amount", kind.ParamKeys);
        Assert.Contains("switch", kind.ParamKeys);
        Assert.Contains("message", kind.ParamKeys);
    }
}
