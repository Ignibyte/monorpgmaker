---
pipeline_id: 77df7bad-6df3-4733-b367-3389649bb023
title: WORK-shop-screen
ticket: b658b5af-4b7f-4316-8d49-b71fadfd7e50
type: work
intake: none (direct /work under a standing /goal — Shop, slice E2 of 3; user chose the content-pipeline SpriteFont)
notes: WORK-shop-screen.notes.md
status: Phase 5 — Complete PASS
---

# WORK-shop-screen

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Shop E2 — the host shop SCREEN (the game's first text rendering + a buy/sell modal over E1's economy).
- **Context:** E1 (#27) shipped the gated shop economy (`WorldSim.ActiveShop` + `Buy/Sell/CloseShop` + `ShopModel` +
  the `Shop` event → `OpenShop`). E2 makes it VISIBLE + playable. E3 (Studio Shop authoring) follows. The user chose
  the **content-pipeline `SpriteFont`** for the text (probe-confirmed: mgcb compiles Arial headless → `font.xnb`).
- **Scope:**
  - *In (gated — the testable seam):*
    1. `ShopState` gains `int Cursor` + `MoveCursor(int delta)` — clamped to `[0, Offers.Count-1]`; a no-op when no
       offers (no over/underflow).
    2. `WorldSim.MovePlayer` returns `false` (no cell change) **while `ActiveShop != null`** — shopping freezes the
       map even if the host forgets to guard. `WorldSim.MoveShopCursor(int delta)` delegates to the active shop
       (no-op when no shop is open).
    3. A `Shop` event in `StartMap.Events()` (the bundled shopkeeper) — gated-verifiable via the golden `start.json`.
  - *In (host — `[ExcludeFromCodeCoverage]`, manual-smoke):*
    - The content-pipeline font: a `Content/…/font.spritefont` (Arial) + the `Content.mgcb` entry (the
      `MonoGame.Content.Builder.Task` + `Content.mgcb` ALREADY exist in `Player.csproj` — **no `packages.lock.json`
      regen**); `Game1.LoadContent` loads the `SpriteFont` + hands it to `RpgGame` (mirror `SetTileset`).
    - `RpgGame` modal INPUT routing (when `Sim.ActiveShop != null`: Up/Down → `MoveShopCursor`, Enter → `Buy(Cursor)`,
      S → `Sell(Cursor)`, Escape → `CloseShop` — instead of movement) + `DrawShopScreen` (offers + gold + a cursor)
      + finally RENDER the message-box TEXT (the font now enables it).
    - `Game1` seeds demo `gold` + a `potion` after boot (host-side — see decisions) so the shop is transactable.
  - *Out / deferred:* E3 (Studio Shop-event authoring); a real INITIAL-STATE / starting-inventory mechanism (none
    exists — `GameSession.Create` starts empty; the demo gold is host-side); shop polish (scrolling, icons,
    sell-confirm); a mid-shop save (already fine — `ActiveShop` isn't persisted).
- **Systems:** runtime/rendering (`RpgGame` Draw + the `SpriteFont`) · input (modal routing) · sim
  (`ShopState.Cursor` + the `WorldSim` movement guard) · content (the Player's content pipeline) · the bundled demo.

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | `ShopState.Cursor` shall start at 0, and `MoveCursor` shall clamp it to `[0, Offers.Count-1]` (no over/underflow; a no-op when there are no offers). | unit |
| REQ-002 | `WorldSim.MovePlayer` shall be a no-op (return `false`, no cell change) while `ActiveShop != null`. | unit |
| REQ-003 | `WorldSim.MoveShopCursor(delta)` shall move the open shop's cursor (and be a no-op when no shop is open). | unit |
| REQ-004 | The bundled start map's `$data` shall carry a `Shop` event (a shopkeeper), reproduced by the golden `start.json`. | unit + gate |
| REQ-005 | *(host / manual-smoke — NOT gate-verified)* The Player shall load a `SpriteFont`, render the shop screen (offers + gold + cursor) + the message text, route modal input (Up/Down/Enter/S/Escape), and seed demo gold — per a documented smoke checklist. | manual smoke |
| REQ-006 | The FULL `bin/gate.sh` shall be green — the content build compiles the font, coverage ≥80 (the gated cursor/guard tested), mutation ≥80, and the golden `start.json` matches. | gate |

## Locked-In Decisions
- **The content pipeline is added FOR THE FONT ONLY, host-side.** D-0022 holds — the **engine** never loads content;
  `Game1` (the host) loads the `SpriteFont`, mirroring how it loads embedded tilesets. The
  `MonoGame.Content.Builder.Task` + an (empty) `Content.mgcb` were ALREADY in `Player.csproj`, so this is wiring an
  asset into existing infrastructure — **no new package, no `packages.lock.json` regen**.
- **`OpenShop`/the font use the content pipeline (.xnb) vs the embedded-resource style of the art — a noted D-0023
  deviation** (capture it; the art stays embedded, only the font is pipeline-built).
- **The demo gold seed is HOST-SIDE (`Game1`).** No initial-state mechanism exists (`GameSession.Create` starts with
  an empty `GameState`); a real starting-state/inventory feature is deferred. The bundled `Shop` EVENT is gated
  (`$data`); only the gold/potion seed is host-side manual-smoke.
- **Most of E2 is `[ExcludeFromCodeCoverage]` host (manual-smoke):** the font load, `DrawShopScreen`, the input
  routing. The GATED seam is `ShopState.Cursor`/`MoveCursor` + the `WorldSim` movement guard + the `Shop` event —
  all unit-tested (Engine MSI is at 81.62, near the floor, so the new gated logic MUST be covered). Determinism;
  MRM-clean.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0017, D-0022, D-0023, D-0024), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0028-shop-screen.md`
- Forge ticket: b658b5af-4b7f-4316-8d49-b71fadfd7e50 (#28)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal; the host screen + content-pipeline font; gated cursor/guard + a Shop demo; E3 + initial-state deferred) |
| 2 — Design | PASS (gated cursor/guard + a Shop demo; host content-pipeline font + modal screen [excluded]; Escape guarded; smoke checklist) |
| 3 — Implement | PASS (cursor/guard gated + the host screen/font/input; content font builds in-gate; oracle 12/8; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic; MAJOR font-crash + MINOR held-Escape + NIT PressAction — all fixed; +1 test; BF+PR captured) |
| 4 — Validate | PASS (540 tests; cov 92.8%; MSI Engine 82.02/Abs 82.22/Analyzers 91.18/Editor 82.28; oracle 12/8; font builds in-gate; GATE GREEN [full]) |
| 5 — Complete | PASS (D-0027 + AD; docs; AAR closed; ticket #28 done; archived; E3 next + last) |
