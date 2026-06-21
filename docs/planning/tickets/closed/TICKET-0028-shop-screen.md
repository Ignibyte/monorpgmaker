---
title: TICKET-0028-shop-screen
status: closed
ticket: b658b5af-4b7f-4316-8d49-b71fadfd7e50
ticket_number: 28
type: feature
created: 2026-06-21
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-shop-screen.spec.md
---

# TICKET-0028-shop-screen

## Summary

**Shop E2 — the host shop SCREEN.** The 2nd of three shop slices: E1 (#27) shipped the gated economy
(`WorldSim.ActiveShop` + `Buy/Sell` + `ShopModel` + the `Shop` event); E2 makes it VISIBLE + playable, and adds the
game's FIRST in-game text rendering via the MonoGame content pipeline (a `SpriteFont` — probe-confirmed compilable
headless). The gated seam (a cursor on `ShopState` + a `WorldSim` movement guard + the bundled `Shop` event) is
unit-tested; the screen, the modal input routing, and the font load are `[ExcludeFromCodeCoverage]` host code
(manual-smoke). E3 (Studio Shop authoring) follows.

## Why

E1 delivered the buy/sell economy but no way to SEE or drive it. E2 closes the loop: a player walks up to a
shopkeeper, opens a buy/sell menu, moves a cursor, buys/sells, and closes — and, as a bonus, the message box finally
renders TEXT (the game had none). It's the slice that makes the shop real.

## EARS Requirements

See the spec (`WORK-shop-screen.spec.md`) — REQ-001 the cursor clamp; REQ-002 the movement guard; REQ-003
`MoveShopCursor`; REQ-004 the bundled `Shop` event; REQ-005 (manual-smoke) the rendered screen + font + input + gold
seed; REQ-006 FULL gate.

## Constraints

D-0022 (the engine never loads content — the font is host-only), D-0023 (the content-pipeline-for-font is a noted
deviation from the embedded-resource art), D-0017/D-0024 (E1's economy stands — E2 only renders/drives it),
determinism (the gated cursor/guard is pure), MRM-clean, the gate stays green (the content build compiles + the
golden `start.json` regens). The demo gold seed is host-side; a real initial-state feature is deferred.
