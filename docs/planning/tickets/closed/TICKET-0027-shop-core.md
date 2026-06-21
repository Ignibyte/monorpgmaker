---
title: TICKET-0027-shop-core
status: closed
ticket: f2b43cea-2355-4a76-8e39-bc9426a8a129
ticket_number: 27
type: feature
created: 2026-06-21
intake:
pipeline_spec: docs/planning/pipeline/completed/WORK-shop-core.spec.md
---

# TICKET-0027-shop-core

## Summary

**Shop E1 — the gated buy/sell core.** The first of three shop slices (the user chose build-full-shop
autonomously): E1 ships the deterministic, fully-testable core the gate covers; **E2** (the host shop SCREEN —
render + modal input + the game's first text rendering) and **E3** (Studio authoring) follow as their own
pipelines. E1 adds an `OpenShop(string Offers)` outcome (the one new `Outcome` case — a shop is a host modal like a
message), a `ShopOffer` + total `Parse`, a `ShopState`, `WorldSim.ActiveShop` + `Buy`/`Sell`/`CloseShop`
(bounds-checked on the `gold` + `item.<id>` counters, typed results), and a `Shop` event kind (D-0024).

## Why

A shop is the last listed built-in. The buy/sell ECONOMY is pure, deterministic state math (gold ± price, item ± 1)
— the part the gate can fully cover — so it ships first, decoupled from the host screen (E2) which needs new
rendering/input the gate can't cover. Building the testable core first de-risks the UI slice.

## EARS Requirements

See the spec (`WORK-shop-core.spec.md`) — REQ-001 `OpenShop` → `ActiveShop`; REQ-002 Buy (afford / can't);
REQ-003 Sell (own / don't); REQ-004 the `Shop` built-in + totality; REQ-005 `CloseShop`; REQ-006 FULL gate + the
oracle reproduces `Shop`.

## Constraints

D-0017 (`OpenShop` is the one new `Outcome` case; Buy/Sell compose `Add`), D-0024 (the `Shop` built-in via the
single descriptor + `.expect` + handler), determinism (pure state math, no clock/RNG), totality (malformed offers /
bad index / unaffordable → a typed result, never a throw), MRM-clean, gate:13. The host screen (E2) + Studio (E3)
are deferred; no `start.json` change in E1.
