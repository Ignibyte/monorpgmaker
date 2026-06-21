# WORK-shop-core — Notes

Per-phase working notes for the paired `.spec.md`. The spec is the contract;
this is the record of what happened.

## Phase 1 — Plan
- **Request:** Shop E1 — the gated buy/sell core (the first of 3 shop slices; the user chose build-full-shop
  autonomously). The deterministic core the gate covers; E2 (the host screen) + E3 (Studio) follow.
- **Intake source:** none (direct `/work` under a standing `/goal`).
- **Classification / tier:** work pipeline, MEDIUM slice. P2 events/economy.
- **Explore (the architecture — verified file:line):**
  - The **outcome 3-places** (add `OpenShop`): `Abstractions/Outcome.cs` (closed record hierarchy —
    SetSwitch/AddCounter/ShowMessage/Warp), `Engine/Sim/OutcomeApplier.cs:31-56` (a `switch` over cases + an
    injected `Action<T>` per side-effecting case — Warp uses `_requestWarp`), `Editor/Expectations/
    ExpectationParser.cs:142-187` (`ParseOutcome` — a quoted-string arm like `ShowMessage`). `WorldSim.cs:32`
    constructs the applier (`message => CurrentMessage = ..., warp => PendingWarp = ...`) — add `shop => ActiveShop
    = new ShopState(ShopOffer.Parse(shop.Offers))`.
  - **`GameState`:** `int GetCount(string)` (read) + `[StateMutator] void Add(string, int)` — **NO `Set` for
    counters** (Buy = `Add("gold", -price)`). `IEventContext` exposes `GetSwitch` + `GetCounter`. Items are counters
    keyed `item.<id>` (GiveItem/Chest). Currency = a new `gold` counter.
  - **The MESSAGE BOX is the modal PRECEDENT for E2:** `WorldSim.CurrentMessage` (set by the applier) → `RpgGame`
    `DrawMessageBanner` (a bare banner — **NO text; only the window title changes**) → input does NOT block
    (movement clears it). So E2 must add the game's FIRST text rendering (`SpriteFont`) + a modal input guard.
  - `ItemRecord(Id,Name,Price)` (Data/Database.cs) is UNWIRED — leave it (offers carry their own prices).
- **Ticket:** #27 — `f2b43cea-2355-4a76-8e39-bc9426a8a129`.
- **AAR id (from `aar-open`):** `ced8d2fa-2a7e-497d-9757-b661b0d77141`.
- **EARS reviewed:** REQ-001 OpenShop → ActiveShop · REQ-002 Buy (afford / can't) · REQ-003 Sell (own / don't) ·
  REQ-004 the Shop built-in + totality · REQ-005 CloseShop · REQ-006 FULL gate + oracle 12/8.
- **Design questions for Phase 2:** (1) the `OutcomeApplier` ctor's **4th callback** (`openShop`) — Explore ALL its
  construction + test call-sites (the #22 `requestWarp` add touched ~4 `OutcomeApplierTests` sites) + update them.
  (2) `OpenShop` carries the offers as an ENCODED STRING (recommend — `.expect`-parseable like `ShowMessage`). (3)
  `ShopOffer.Parse` totality (a malformed entry skipped — split on `,` then `:`; `int.TryParse` the prices). (4)
  Buy/Sell return a typed result (`ShopResult`? Ok + reason). (5) `ShopState`/`ShopOffer` in `Engine.Sim` (gated).

## Phase 2 — Design
- **Approach / architecture:** A shop is a host MODAL, so it gets the ONE new outcome `OpenShop(string Offers)`
  (Abstractions) — wired the 3 places + the `WorldSim` applier callback (`shop => ActiveShop =
  new ShopState(ShopOffer.Parse(shop.Offers))`). The buy/sell ECONOMY is split for testability: a **pure static
  `ShopModel.Buy/Sell(GameState, ShopOffer) → ShopResult`** (the bounds-checked state math — trivially testable
  with just a `GameState`), and a thin **`WorldSim.Buy/Sell(int index)`** wrapper (index-bounds + `ActiveShop` →
  `ShopModel`). `ShopOffer` (`ItemId,BuyPrice,SellPrice`) + a **total** `Parse` of the encoded `id:buy:sell,…`
  string; `ShopState` wraps the offers (a class so E2 adds a cursor). The `Shop` event (kind `Shop`, param `items`)
  → `Run => [OpenShop(items)]` (D-0024 — registration + `.expect` + handler). Currency = a `gold` counter; items =
  `item.<id>` counters (Add only — no Set). All `Engine.Sim` (gated).
- **File manifest (14):**
  | # | File | Change |
  |---|---|---|
  | 1 | MOD `Abstractions/Outcome.cs` | + `public sealed record OpenShop(string Offers) : Outcome;` (the one host-modal outcome beyond ShowMessage) |
  | 2 | MOD `Engine/Sim/OutcomeApplier.cs` | ctor 4th callback `Action<OpenShop> openShop` (+ null-guard) + `case OpenShop` |
  | 3 | NEW `Engine/Sim/ShopOffer.cs` | `readonly record struct ShopOffer(string ItemId, int BuyPrice, int SellPrice)` + `static ShopOffer[] Parse(string)` (total — skip malformed) |
  | 4 | NEW `Engine/Sim/ShopState.cs` | `sealed class ShopState { IReadOnlyList<ShopOffer> Offers }` |
  | 5 | NEW `Engine/Sim/ShopResult.cs` | typed result (`Ok` + `Error`), `Success()`/`Failure(reason)` (mirrors `BehaviourResult`) |
  | 6 | NEW `Engine/Sim/ShopModel.cs` | `static ShopResult Buy(GameState, ShopOffer)` + `Sell(...)` (pure, bounds-checked; `const GoldCounter = "gold"`) |
  | 7 | MOD `Engine/Sim/WorldSim.cs` | applier 4th callback; `ShopState? ActiveShop`; `Buy(int)`/`Sell(int)` (index-bounds → `ShopModel`); `CloseShop()` |
  | 8 | NEW `Engine/Sim/Events/ShopEvent.cs` | kind `Shop`; null-guard `items`; `Run => [new OpenShop(_items)]` |
  | 9 | MOD `Engine/Sim/BehaviourRegistry.cs` | + `new("Shop", ["items"], factory)` |
  | 10 | NEW `Engine/Sim/Events/Expectations/Shop.expect` | `=> OpenShop("potion:5:2")` |
  | 11 | MOD `Editor/Expectations/ExpectationParser.cs` | + an `OpenShop("...")` arm (quoted-string, like ShowMessage) |
  | 12 | MOD `Editor/Expectations/HandlerRegistry.cs` | `["Shop"]` matching `Shop.expect` |
  | 13 | MOD `tests/.../OutcomeApplierTests.cs` | the 4th-callback ripple (lines 15, 99–101, 103) + a 4th null-guard assert |
  | 14 | NEW `tests/.../ShopCoreTests.cs` | the matrix |
- ### Regression Test Plan
  | # | Test | Proves Requirement |
  |---|---|---|
  | T1 | a `StepOn` `ShopEvent` ("potion:5:2,ether:20:8") triggered in a `WorldSim` (player steps onto it) → `ActiveShop.Offers` has the 2 parsed offers (ids + prices) | REQ-001 |
  | T2 | `ShopModel.Buy`: `gold=10`, potion:5:2 → `gold 5` + `item.potion 1` + Ok; `gold=1` → unchanged + `!Ok` | REQ-002 |
  | T3 | `ShopModel.Sell`: `item.potion=1`, potion:5:2 → `item.potion 0` + `gold +2` + Ok; `item.potion=0` → unchanged + `!Ok` | REQ-003 |
  | T4 | `ShopEvent.Run → [OpenShop(items)]`; registry materialises `Shop`; missing `items` → `Failure`; `ShopOffer.Parse("garbage")` → empty (no throw), `Parse("potion:5:2,bad,ether:x:8")` → only the valid offer | REQ-004 |
  | T5 | `WorldSim.Buy/Sell(index)`: OOB index → `Failure`; valid index delegates (gold/item change); `CloseShop()` → `ActiveShop` null | REQ-002/003/005 |
  | T6 | FULL gate green; oracle reproduces `Shop` → **12 rows / 8 modules** | REQ-006 |
  - **No uncoverable paths** — all gated `Engine.Sim`/`Abstractions`/`Editor` logic (the screen + input are E2). The
    #18–#26 suites stay green; the `OutcomeApplier` ctor ripple is updated at every site (1 src + 5 test).
- **Risks / decisions:** (1) `ShopModel` (pure static) separates the buy/sell math from `WorldSim` — testable with
  just a `GameState`; `WorldSim.Buy/Sell(index)` is a thin bounds+delegate wrapper. (2) `OpenShop` carries the
  ENCODED string (`.expect`-parseable like `ShowMessage`); the sim parses it. (3) `ShopOffer.Parse` is TOTAL (split
  `,` then `:`; 3 parts + non-empty id + `int.TryParse` both prices, else SKIP the entry). (4) **no state mutation
  before the affordability/ownership check passes** (ShopModel checks first → no corruption on failure). (5) the
  `OutcomeApplier` ctor gains a 4th callback + a 4th null-guard — the one ripple (WorldSim + OutcomeApplierTests).
  (6) `gold` is a `ShopModel.GoldCounter` const; items inline `"item." + ItemId` (match GiveItem/Chest). (7) gate:13
  → 12 rows / 8 modules. (8) a test opens a shop by triggering a `ShopEvent` in a `WorldSim` (the realistic apply
  path — no new test seam).

## Phase 3 — Implement
- **Built:**
  - `Abstractions/Outcome.cs` — `+ OpenShop(string Offers)` (the one new outcome).
  - `Engine/Sim/OutcomeApplier.cs` — ctor 4th callback `Action<OpenShop> openShop` (+ null-guard) + `case OpenShop`.
  - `Engine/Sim/ShopOffer.cs` (NEW) — `record struct (ItemId,BuyPrice,SellPrice)` + total `Parse` (split `,`/`:`,
    skip malformed). `ShopState.cs` (NEW, wraps `Offers`). `ShopResult.cs` (NEW, typed Ok/Error). `ShopModel.cs`
    (NEW) — pure `Buy`/`Sell(GameState, ShopOffer)` (bounds-checked; `const GoldCounter = "gold"`; items
    `"item." + id`; no mutation before the guard).
  - `Engine/Sim/WorldSim.cs` — applier 4th callback (`shop => ActiveShop = new ShopState(ShopOffer.Parse(...))`);
    `+ ActiveShop`; `Buy(int)`/`Sell(int)` (index-bounds → `ShopModel`); `CloseShop()`.
  - `Engine/Sim/Events/ShopEvent.cs` (NEW, kind `Shop`) → `Run => [OpenShop(items)]`; `BehaviourRegistry` +
    `Shop.expect` + `HandlerRegistry` entry; `ExpectationParser` `OpenShop("…")` arm (mirrors ShowMessage).
  - Tests: `ShopCoreTests.cs` (NEW, 14); `OutcomeApplierTests` 4th-callback ripple (helper + 4 null-guards + a
    new 4th-arg null-guard assert).
- **Deviations from design (+ reason):** none material. `ShopModel` (pure static) split from `WorldSim` as
  designed; `WorldSim.Buy/Sell(index)` is the thin bounds+delegate wrapper. A test opens a shop by stepping a
  player onto a `StepOn` `ShopEvent` in a real `WorldSim` (the realistic apply path — confirmed: `ActiveShop` set).
- **Build / verify:** whole solution `-warnaserror` 0/0; **GATE GREEN [fast]**; gate:13 oracle **12 rows / 8
  modules** (Shop +1); 532 tests. The `OutcomeApplier` ctor ripple is updated at all sites; the #18–#26 suites green.
- **NOT yet (Phase 4):** the FULL gate (coverage + mutation — the `ShopModel`/`ShopOffer.Parse`/`WorldSim.Buy/Sell`
  + the registration + the `OpenShop` parser-arm mutants must die).

## Inspect (Phase 3.5)
- **Lenses run:** 1 adversarial correctness + data-integrity critic — found **1 MAJOR** (reproduced) + cleared all
  else CONCRETELY (ran `--filter Shop` / `--filter OutcomeApplier` + throwaway repros for the economy + Parse; ran
  the live oracle `12 rows / 8 modules`).
- **Findings:**
  | # | Severity | Finding | Verdict | Fix |
  |---|---|---|---|---|
  | 1 | **MAJOR** | `ShopOffer.Parse:24` accepted negative prices (`int.TryParse` allows a sign), so an authored `"potion:-5:2"` → `BuyPrice=-5` → `ShopModel.Buy` from gold≥0 passed the `< BuyPrice` guard + ran `Add("gold", +5)` — **buying ADDED gold + granted the item** (symmetric on sell). Repro: gold=0 → Buy → gold=5, item.potion=1. | **REAL data-integrity footgun** — Parse claims totality, so a negative price is a malformed entry it must reject; authored `$data` is a trust boundary. | **FIXED** — `&& buy >= 0 && sell >= 0` added to the Parse guard (negative-price entries now skipped) + XML-doc + a `Parse_NegativePrice_Skipped` test. Captured BF-claude-negative-price-inverts-economy-001 + PR-claude-validate-nonneg-numeric-at-parse-boundary-001. |
  | 2 | noted (not a defect) | `OutcomeFormat.Format` has no `OpenShop`/`Warp` arm (falls to `ToString()`). | cosmetic — the mismatch-REPORT text only; the oracle compares via `SequenceEqual` (payload-bearing), and it matches the existing `Warp` precedent. | none. |
- **Cleared (verified, not just unflagged):** the economy no-corruption (the guard is BEFORE any `Add` — a failed
  buy/sell mutates nothing; exact `BuyPrice`/`+1` / `SellPrice`/`-1`; NO Buy/Sell price swap; `"item."+id` on both;
  boundary `<` so `gold==price` affords); `Parse` totality (no throw on garbage/""/2-parts/empty-middle/4-parts/
  overflow); the new `OpenShop` case wired (a `case` BEFORE `default: throw` — no throw on apply) + the WorldSim
  callback parses via `ShopOffer.Parse`; `WorldSim.Buy/Sell` index bounds (both, `>=Count`) + `CloseShop`; the
  **`.expect`↔handler EXACT** round-trip (the parser slice no off-by-one; `ExpectationRunner` compares via
  `SequenceEqual`, payload-sensitive; oracle 12/8); the applier 4th-callback ripple (the new null-guard asserts
  `openShop`); MRM/§14 + determinism (no LINQ/float/Random/DateTime; XML-doc public).
- **Post-fix:** GATE GREEN [fast] (the fix re-ran clean; build 0/0; oracle 12/8; the negative-price test passes).
- **Capture:** `failure-record` BF-…-negative-price-inverts-economy-001 + `prevention-rule-record`
  PR-…-validate-nonneg-numeric-at-parse-boundary-001 (validate the RANGE of authored numeric data at the parse
  boundary, not just the format — `int.TryParse` accepts signs).
- **Phase-4 kill-list (covered by the 15 tests):** `ShopModel.Buy/Sell` guards (`<`/`<1` boundaries · the `-price`
  sign · the `+1`/`-1` · NO `BuyPrice`/`SellPrice` swap · the `"item."+id` key); `ShopOffer.Parse` (`==3` arity ·
  non-empty id · both `TryParse` · **the new `>= 0` non-negative guards**); `WorldSim.Buy/Sell` index bounds (`<0`,
  `>=Count`, the `ActiveShop is null`); the `.expect` slice arithmetic + the `length >= 0` guard; `ShopEvent.Run`
  the `_items` arg; the `Shop` registration `TryGetValue("items")`; `CloseShop = null`.

## Phase 4 — Validate
- **Tests (+15; 518 → 533):** `ShopCoreTests.cs` (15) — REQ-001 shop opens with parsed offers · REQ-002 Buy
  affordable/unaffordable + WorldSim delegate/bad-index/no-shop · REQ-003 Sell owned/not · REQ-004 ShopEvent.Run →
  OpenShop + registry materialises + missing-items fails + Parse garbage/mixed/**negative** (the inspect add) ·
  REQ-005 CloseShop. Plus the `OutcomeApplierTests` 4th-callback ripple (helper + 4 null-guards).
- **`dotnet test MonoRpgMaker.slnx`: 533 passed, 0 failed.**
- **`bin/gate.sh`: GATE GREEN [full] — 13/13** (first try). Coverage **92.7%** (floor 80). Mutation: Engine
  **81.62%** · Abstractions 82.22% · Analyzers 91.18% · Editor **82.28%** (floor 80 — the shop adds a lot of new
  gated code [ShopModel/Offer/State + WorldSim Buy/Sell] + the OpenShop parser arm; the kill-list tests kill them,
  MSI clears but Engine is now nearer the floor — watch for E2/E3). Oracle **12 rows / 8 modules** (Shop +1). Receipt
  written.
- **Pre-existing exclusions:** none — all gated `Engine.Sim`/`Abstractions`/`Editor` logic. Smoke (not blocking):
  the `Shop` event is placeable (`BehaviourRegistry.Kinds` offers it) + opens a `WorldSim.ActiveShop`; the buy/sell
  SCREEN + modal input + text rendering are **E2** (deferred), so there is no visible shop in the Player yet — the
  economy is gated + testable now, the UI is the next slice.

## Phase 5 — Complete
- **Docs updated:** `docs/roadmap.md` (the built-in-library paragraph += #27 — Shop's gated economy core; the
  screen E2 + Studio E3 remain) + `docs/product-phasing.md` (the Event-behavior row += Shop's economy core). No
  `decisions.md` entry — `OpenShop` extends the Outcome vocabulary ADDITIVELY (D-0017's documented "one registry,
  not two") + the Shop built-in is D-0024; both stand.
- **Forge capture:** `aar-submit` ced8d2fa (completed, effectiveness 4). `failure-record`
  BF-claude-negative-price-inverts-economy-001 + `prevention-rule-record`
  PR-claude-validate-nonneg-numeric-at-parse-boundary-001 ALREADY recorded in inspect (the critic's MAJOR catch —
  the adversarial-verify value). No AD.
- **Ticket closed:** #27 (`f2b43cea-…`) → done; `TICKET-0027-shop-core.md` moved open/ → closed/.
- **Archived:** spec + notes moved active/ → completed/; spec status `Phase 5 — Complete PASS`.
- **The shop is 3 slices:** **E1 (this — the gated economy core) DONE.** **E2 (the immediate NEXT slice) — the
  host shop SCREEN:** render `WorldSim.ActiveShop` + modal input routing (navigate / buy / sell / close) + the
  game's FIRST text rendering (`SpriteFont` — today only the window title shows text); `[ExcludeFromCodeCoverage]` /
  manual-smoke; a bundled shop demo (seed `gold` + a Shop placement). **E3 — Studio Shop-event authoring** (an
  offers editor). NB: Engine MSI is now **81.62** (nearer the 80 floor — the shop added a lot of gated code); E2/E3
  must add gated tests for any new gated logic so the floor holds.
