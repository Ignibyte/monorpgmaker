---
pipeline_id: e49e3723-d9f3-4a0d-9664-7942a734d9aa
title: WORK-shop-core
ticket: f2b43cea-2355-4a76-8e39-bc9426a8a129
type: work
intake: none (direct /work under a standing /goal — Shop, slice E1 of 3; user chose build-full-shop autonomously)
notes: WORK-shop-core.notes.md
status: Phase 5 — Complete PASS
---

# WORK-shop-core

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Shop E1 — the gated buy/sell core (an `OpenShop` outcome + a `ShopOffer`/Buy-Sell model + a `Shop`
  event kind + `WorldSim.ActiveShop`).
- **Context:** Shop is a buy/sell MENU subsystem — too big for one slice, so **three**: **E1 (this — the gated,
  fully-testable core the gate covers)**, **E2** (the host shop SCREEN — render + modal input + the game's FIRST
  text rendering; `[ExcludeFromCodeCoverage]`/manual-smoke), **E3** (Studio authoring). The user chose
  build-full-shop autonomously.
- **Scope:**
  - *In (the gated core):*
    1. **`OpenShop(string Offers)` outcome** (Abstractions) — the ONE new `Outcome` case, wired the 3 places
       (`Outcome.cs` + `OutcomeApplier.cs` + `ExpectationParser.cs`) **plus** the `WorldSim` applier callback.
    2. **`ShopOffer`** (Engine.Sim) = `readonly record struct ShopOffer(string ItemId, int BuyPrice, int
       SellPrice)` + `static ShopOffer[] Parse(string offers)` — the encoded inventory `"potion:5:2,ether:20:8"`
       (`id:buy:sell`, comma-separated); **total** (a malformed entry is skipped, never a throw).
    3. **`ShopState`** (Engine.Sim) = the open shop wrapping `IReadOnlyList<ShopOffer> Offers` (a class so E2 can
       add a cursor).
    4. **`WorldSim.ActiveShop`** (`ShopState?`, null when closed) + `Buy(int offerIndex)` / `Sell(int offerIndex)`
       / `CloseShop()` — Buy: bounds-check the index + `GetCount("gold") >= BuyPrice` → `Add("gold", -BuyPrice)` +
       `Add("item." + ItemId, +1)`, else a typed failure (no state change); Sell: `GetCount("item." + ItemId) >= 1`
       → `Add(item, -1)` + `Add("gold", +SellPrice)`, else a typed failure. Returns a typed result (Ok + reason).
    5. **A `Shop` event kind** (`ShopEvent`, params `["items"]`) → `Run => [new OpenShop(_items)]` + a
       `BehaviourRegistry` registration + `Shop.expect` (gate:13) + a `HandlerRegistry` entry.
    - The **`gold`** counter is the currency convention (a `GameState` counter, like items).
  - *Out / deferred:* the host SHOP SCREEN (rendering + modal input + a `SpriteFont` — **E2**; today the game
    renders NO text, the message box only sets the window title) · Studio authoring (**E3**) · a BUNDLED shop demo
    (E2 — an `OpenShop` sets `ActiveShop` invisibly without the screen, so **NO `start.json` change / golden regen**
    in E1) · save/restore of a mid-shop `ActiveShop` (the shop isn't open across a save — fine).
- **Systems:** events (the `Shop` built-in + the `OpenShop` outcome) · sim (`WorldSim.ActiveShop` + Buy/Sell over
  `GameState` counters) · the outcome vocabulary (`Outcome`/applier/parser).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | An applied `OpenShop` outcome shall set `WorldSim.ActiveShop` to a shop reflecting the parsed offers. | unit |
| REQ-002 | `WorldSim.Buy(index)` with `gold ≥ buyPrice` shall deduct the price + grant the item (`item.<id>` +1); with insufficient gold it shall leave state unchanged + return a typed failure. | unit |
| REQ-003 | `WorldSim.Sell(index)` with the item owned shall remove one + grant the sell price; without it it shall leave state unchanged + return a typed failure. | unit |
| REQ-004 | A `Shop` event (param `items`) shall materialise + on its trigger return `[OpenShop(items)]` (`.expect`-validated); a missing `items` → `BehaviourResult.Failure`; malformed offers → an empty `ActiveShop` (no throw). | unit + gate:13 |
| REQ-005 | `WorldSim.CloseShop()` shall clear `ActiveShop` (back to null). | unit |
| REQ-006 | The FULL `bin/gate.sh` shall be green (coverage + mutation + the oracle); the `.expect` oracle shall reproduce `Shop` (12 rows / 8 modules). | gate |

## Locked-In Decisions
- **`OpenShop` is the ONE new `Outcome` case (D-0017):** a shop is a host MODAL (like `ShowMessage`), not
  expressible as `SetSwitch`/`AddCounter`. It carries the offers as an **encoded string** (cleanest for the
  `.expect` oracle — a quoted arg like `ShowMessage`); the sim parses it into `ShopState` via `ShopOffer.Parse`.
- **The economy is counters:** `gold` (currency) + `item.<id>` (inventory) are `GameState` counters; Buy/Sell use
  `Add` (there is NO `Set` for counters — Buy = `Add("gold", -price)`). `ItemRecord` stays unwired (offers carry
  their own prices). Buy/Sell are **bounds-checked + return typed results** (unaffordable / nothing-to-sell / bad
  index → a typed failure, never a throw — totality).
- **The `Shop` built-in is contract-first (D-0024):** one `Registration` + `.expect` + handler.
- **The `OutcomeApplier` ctor gains a 4th callback** (`openShop`) — the one ripple across its construction
  (`WorldSim`) + its test call-sites.
- **Deferred:** the host screen / modal input / text rendering (E2); Studio authoring (E3); a bundled demo (E2 — no
  `start.json` change here). Determinism (Buy/Sell are pure state math); MRM-clean.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017, D-0023, D-0024), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0027-shop-core.md`
- Forge ticket: f2b43cea-2355-4a76-8e39-bc9426a8a129 (#27)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal; the gated shop core; host screen E2 + Studio E3 deferred) |
| 2 — Design | PASS (OpenShop outcome + pure ShopModel.Buy/Sell + WorldSim.ActiveShop + Shop event; OutcomeApplier 4th-callback ripple mapped) |
| 3 — Implement | PASS (OpenShop + ShopModel/Offer/State + WorldSim.ActiveShop + Shop event; build 0/0; oracle 12/8; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic; 1 MAJOR found+fixed [negative-price economy inversion]; +1 test; BF+PR captured) |
| 4 — Validate | PASS (533 tests; cov 92.7%; MSI Engine 81.62/Abs 82.22/Analyzers 91.18/Editor 82.28; oracle 12/8; GATE GREEN [full]) |
| 5 — Complete | PASS (docs touched; AAR closed; ticket #27 done; archived; E2 next) |
