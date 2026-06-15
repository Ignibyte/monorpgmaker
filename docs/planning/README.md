# Planning Workspace

This folder holds work planning, not permanent game design prose. Permanent
design docs stay directly under `docs/`.

## Directory Map

- `intake/` — pre-ticket work ideas. Rough candidates that may become forge tickets later.
- `tickets/open/` — forge-backed tickets that are real backlog items but not necessarily the active pipeline.
- `tickets/closed/` — local ticket documents after their forge ticket closes.
- `_templates/` — reusable planning templates (intake, ticket, EARS).
- `pipeline/active/` — the single active forge-backed work item. Never more than one spec here.
- `pipeline/completed/` — archived pipeline specs and notes after work closes.
- `pipeline/_templates/` — the spec/notes templates used when a forge ticket is minted.

## Ticket Document Rule

Every forge ticket created for monorpgmaker work must have a local ticket
document: `tickets/open/TICKET-<number>-<slug>.md`. The frontmatter `ticket:`
field is the canonical link to the forge ticket. Closed ticket docs move to
`tickets/closed/`.

When a ticket becomes active implementation work, `/pipeline:plan` also creates
the active pipeline document pair: `<slug>.spec.md` + `<slug>.notes.md`.

Intake docs capture possible work before a ticket exists. When promoted,
`/pipeline:plan` mints the forge ticket, creates/links the ticket document,
creates the spec/notes pair, and links the intake doc to the ticket/spec.

## Requirements Style

Pipeline acceptance criteria use EARS — concrete, testable, one behavior per row:

- Ubiquitous: `The <system> shall <response>.`
- Event-driven: `When <trigger>, the <system> shall <response>.`
- State-driven: `While <state>, the <system> shall <response>.`
- Unwanted behavior: `If <condition>, then the <system> shall <response>.`
- Optional/contextual: `Where <context>, the <system> shall <response>.`

Use `_templates/ears-requirements.md` when drafting new work.
