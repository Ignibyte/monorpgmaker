---
pipeline_id: f284d897-5512-4fd1-ad09-bbe44bcb01aa
title: WORK-tileset-reskin-warp
ticket: 765e62a0-a48d-4f6a-b9b5-a50d840381bf
type: work
intake: none (direct /work request under a standing /goal — slice A of 3)
notes: WORK-tileset-reskin-warp.notes.md
status: Phase 5 — Complete PASS
---

# WORK-tileset-reskin-warp

> Pipeline spec (always-loaded contract). Detailed per-phase work lives in the
> paired `.notes.md`. Phase advance = updating the `status:` frontmatter above.

## Work Spec
- **Title:** Cross-map tileset re-skin on warp — the runtime host reloads the active map's tileset sheet when a
  Warp switches to a map with a different tileset (the #22 deferred item).
- **Scope:**
  - *In* — (1) when the active map's tileset name changes (a `GameSession` warp to a map with a different sheet),
    the runtime host reloads + applies that map's sheet, resolved through the single-source `TilesetCatalog`
    (D-0025), so the new map renders with its own tileset. (2) The re-skin fires only on an ACTUAL change — a step
    that doesn't change the tileset name does not reload (no per-frame churn); `RpgGame` (Engine) tracks the
    last-applied name and signals the host, which performs the reload (D-0022 — `RpgGame` never loads embedded
    resources). (3) The change-detect decision is a small gated/pure seam (unit-coverable); the texture reload
    stays host (`[ExcludeFromCodeCoverage]`, manual smoke). (4) The bundled TOWN map uses a different sheet
    (`lpc-grass`) so the start⇄town warp visibly re-skins — regen `content/maps/town.json` (golden stays green).
  - *Out* — per-layer tilesets; animated tiles; a fade/transition on the switch; any change to the sim/`Warp`
    behaviour itself (the switch already works, #22).
- **Systems:** runtime/player (`RpgGame`/`Game1` host) · rendering (the tileset sheet) · map/tilemap (the bundled
  town `$data`) · engine (the gated change-detect seam).

## Acceptance Criteria (EARS)

| ID | EARS Requirement | Verification |
|---|---|---|
| REQ-001 | When the active map changes to one whose tileset name differs from the currently-applied sheet, the runtime host shall reload + apply that map's tileset sheet so the map renders with its own tileset. | unit (the change-detect seam) + manual smoke |
| REQ-002 | The host shall resolve the reloaded sheet through the single-source `TilesetCatalog` (D-0025) — it cannot display a sheet the catalog cannot resolve. | unit + review |
| REQ-003 | When the active map's tileset name is unchanged across a step, the host shall NOT reload the sheet (the re-skin fires only on an actual change — no per-frame churn). | unit (the change-detect seam) |
| REQ-004 | The bundled town map shall use a different tileset than the start map (`lpc-grass`), so the start⇄town warp visibly re-skins. | unit (the town tileset name) + manual smoke |
| REQ-005 | The FULL `bin/gate.sh` shall be green (coverage + mutation + the `.expect` oracle), and the committed `content/maps/town.json` shall match its programmatic source (golden). | gate |

## Locked-In Decisions
- **D-0022 (neutral thin host) — the re-skin SEAM:** `RpgGame` (Engine) tracks the last-applied tileset name and
  SIGNALS a tileset change; the Player host `Game1` performs the reload (it owns the embedded sheet resources +
  the GPU). `RpgGame` never loads embedded resources. This mirrors the #20 host-resolution-single-source seam.
- **D-0025 (single-source catalog):** the reload resolves the sheet through `TilesetCatalog` (the same path as the
  initial load) — the host can't show a sheet the catalog can't resolve.
- **D-0026 (the GameSession switch):** the active map changes via the existing warp switch; the re-skin is a render
  consequence, not a sim change. No change to `WorldSim`/`GameSession`/`Warp`.
- **A gated change-detect seam:** the "did the tileset change since last applied?" decision is extracted as a pure,
  unit-testable seam; only the texture `Texture2D.FromStream` reload stays host (`[ExcludeFromCodeCoverage]`).
- **Determinism:** the re-skin is driven by the active map's `$data` (its tileset name), never by time/RNG.

## Linked Artifacts
- Design docs: `docs/decisions.md` (D-0022, D-0025, D-0026), `docs/roadmap.md`.
- Intake doc: none (direct `/work` under a standing `/goal`).
- Ticket doc: `docs/planning/tickets/open/TICKET-0023-tileset-reskin-warp.md`
- Forge ticket: 765e62a0-a48d-4f6a-b9b5-a50d840381bf (#23)

## Phase Plan
| Phase | Status |
|---|---|
| 1 — Plan | PASS (autonomous — standing /goal) |
| 2 — Design | PASS (TilesetTracker gated seam + host OnTilesetChanged hook + town→lpc-grass) |
| 3 — Implement | PASS (TilesetTracker + RpgGame/Game1 hook + town→grass; build 0/0; GATE GREEN [fast]) |
| 3.5 — Inspect | PASS (1 critic, verified concretely; 2 doc-accuracy fixes; no code defect) |
| 4 — Validate | PASS (477 tests; cov 93.0%; MSI Engine 85.68/Abs 82.22/Analyzers 91.18/Editor 85.45; GATE GREEN [full]) |
| 5 — Complete | PASS (docs touched; AAR closed; ticket #23 done; archived) |
