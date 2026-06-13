---
title: "plan: Party stations — distinct game verbs + per-station toy art"
type: plan
status: proposal (awaiting approval — no build yet)
date: 2026-06-13
origin: /design-review feedback (2026-06-13)
related: docs/plans/2026-06-12-party-campus-pack-implementation-plan.md
---

# Plan: distinct game verbs (#3) + per-station toy art (#4)

This plan covers the two larger design-review items the user deferred for
approval. **Nothing here is built yet.** #1 (campus zoom/follow) and #2 (toy
clarity: task-halo + faded optional pokes) already shipped on `main`
(`a11d4c5`, `71778c0`).

## Problem (from the review)

- **#3 — "the games all seem the same, move the toys to a place."** Confirmed
  in code: the seven `ToyPatternId`s are distinct *rule sets* (`Complete`
  gating, target kinds, order) but every one reduces to the same player **verb**
  — drag a token onto a labeled pad. Variety that lives only in validation
  rules is invisible to a 7-year-old. The one exception is `BalanceMeters`,
  which already has a different verb (tap-to-tune dial) — that is the model to
  follow.
- **#4 — "we need different toy designs within the game."** Every toy renders
  as a tinted `CampusWorldSprites.Circle` placeholder
  (`PartyStationRenderer.DecoratePlayfield`). `AssetCatalog` carries intentional
  `prop.party.*` placeholder keys awaiting final art; `IsPlaceholderToySprite`
  is the seam that flips a toy from "tinted circle" to "real sprite."

## Goal

By the end: a child walking between two adjacent stations does something
**mechanically different** at each (not just "drag again"), and the toys look
like what they are (a battery, a wheel, a soup pot) — not colored dots.

---

## Part A — Distinct game verbs (#3)

### Approach: verb families, not 10 bespoke engines

Keep the proven spine (KTD5: toy patterns are shared systems). Add a small set
of **distinct interaction verbs** to `ToyInteractionKit`, each a
`ToyPatternController` input handler + `ToyPatternRules` variant + renderer
affordance — the same way `BalanceMeters` added the tap-dial. Then assign
stations so **no two adjacent stations share a verb**.

Proposed verb families (pointer-first, classroom-safe, no harsh fail — all
honor the existing host-validated submit seam so 2P + tests come free):

| Verb family | What the player does | Candidate stations | Reuses |
|---|---|---|---|
| **Place** (existing `DragToSlot`/`SortToBin`/trio/care) | drag token → pad/bin | keep on ~3-4 stations | as-is |
| **Tune** (existing `BalanceMeters`) | tap dial into the green band | Green City | as-is |
| **Trace** (new) | drag a finger along a path/route | Spaceport (flight path), Weather (evac route) | drag input + path-progress rule |
| **Aim** (new) | pull-back-and-release to launch toward a target | Robotics (fix-bot toss), Spaceport (probe launch) | drag vector + landing check |
| **Pour/Fill** (new) | press-and-hold to fill a vessel to a target level | Kitchen (soup), Vet (medicine dose) | hold timer feeding a meter |
| **Rhythm-tap** (new) | tap pads in time with a beat | Music (remix) | tap input + timing window |
| **Match-flip** (new) | flip and match fact pairs | Newsroom (fact-check) | reveal/match rule |

Right-sizing: we do **not** need 10 unique verbs. Target ~5-6 verb families
across 10 stations, arranged so the campus never feels like "place ×10." The
exact assignment is a decision in the discuss step below.

### Engineering units (A1–A4)

- **A1. Kit verb extension.** Add the new `ToyPatternId`s + their
  `ToyPatternController` input handlers + `ToyPatternRules` completion logic.
  Each new verb gets EditMode golden/reject tests like the existing seven, and
  must run identically host-side (2P parity is free if it routes the existing
  submit seam). *Highest risk; do first, one verb at a time, Robotics-style
  proof before multiplying.*
- **A2. Renderer affordances.** Each verb needs a readable affordance
  (the dial already exists; trace needs a path guide, aim needs a launch arc,
  pour needs a fill gauge, rhythm needs beat pads). Pointer-first + non-color
  cues per R19.
- **A3. Station re-assignment.** Update `PartyStationDefinitions` so each
  station declares its verb; re-tune seeds for the new verbs. Validator + copy
  rules still apply.
- **A4. Re-verify the spine.** All-10 smoke, lifecycle churn, 2P network state,
  reveal/accessory derivation must stay green (the verbs change *how you act*,
  not the result contract — one `MiniGameResult` per completion, unchanged).

### Open decisions (resolve before A1)

1. Which verb maps to which station (the table above is a starting point).
2. How many net-new verbs to build (recommend starting with **3**: Trace, Aim,
   Pour — they cover the most stations and are the most visibly different from
   "place").
3. Whether 2P needs new sync for continuous verbs (trace/aim send a final
   accepted vector, not per-frame — same compact model as today).

---

## Part B — Per-station toy art (#4)

### Approach: real sprites behind the existing placeholder seam

`PartyStationRenderer` already branches on `IsPlaceholderToySprite`. Author real
toy sprites, register them in `AssetCatalog` as **final art**, and the renderer
automatically stops drawing tinted circles and draws the sprite — the #2
task-halo/fade treatment then keys off role, not placeholder status (small
tweak so halos still mark task toys on final art).

### Engineering units (B1–B3)

- **B1. Toy art pipeline.** Decide the source: extend the existing editor
  generators (`CareerQuestAccessoryArtBuilder` / sprite-kit generator) to emit
  toy sprites, or author/import a toy set. ~5-6 toys × 10 stations ≈ 50-60
  sprites (fewer if shared toys repeat across seeds).
- **B2. Catalog + wiring.** Register each toy sprite in `AssetCatalog` against
  its `prop.party.*` key; `AssetValidationTests` confirms every seed object
  resolves to final art (no remaining placeholders for shipped stations).
- **B3. Fit pass.** Toy sprites sit in trays + on pads at consistent scale;
  the #2 halo/fade still reads on real art; screenshot check per station.

### Open decisions (resolve before B1)

1. Art source (generator vs authored/imported set) and visual style — must
   match `DESIGN.md` (Future Workshop Diorama / handmade toy look).
2. Whether to do all 10 stations or start with the first-six wave.

---

## LOCKED DECISIONS (2026-06-13, user)

Three net-new verbs, on top of the existing place/sort/trio/care/compose and
the tune dial:

- **Trace** — drag a finger along a path/route through ordered waypoints;
  completes when the whole path is traced in order. (Distinct from place: one
  continuous gesture, not N drops.)
- **Shoot** — pull-back-and-release to launch a toy toward a target; completes
  on landing in the goal. (Distinct: aim + power, not placement.)
- **Calculate = deduce** — tap to **eliminate** candidate answers that break a
  clue until one remains; the survivor is the answer. CRITICAL: implement as
  tap-to-cross-out, NOT drag-to-bin, so it does not collapse into the existing
  sort/match verb.

### Station → verb map (proposed; trace/shoot homes are low-regret)

| Station | Verb |
|---|---|
| Spaceport | **Trace** (flight path: launch→orbit→deliver→land) |
| Weather Lab | **Trace** (evac/shelter route) |
| Robotics | **Shoot** (launch the rebuilt bot to the rescue spot) |
| Newsroom | **Calculate/deduce** (cross out false facts → true headline) |
| AI Lab | **Calculate/deduce** (eliminate wrong sort rules) |
| Green City | Tune (existing dial) |
| Kitchen / Vet / Music / Design Build / Health Hero / Logic Court | existing place/trio/care/compose/sort |

This keeps every district mixed (no two adjacent stations share a verb).

### Build order / status

1. **Trace proof on Spaceport** — ✅ DONE (commit `85017ab`). Spaceport is
   TracePath: numbered waypoint stops along a drawn flight path, tapped in
   order, over the existing host-validated action seam. EditMode 237/237,
   PlayMode 229/229. Implemented as `ToyPatternId.TracePath` (ordered like
   SequenceCards, per-waypoint `waypoint.{id}` targets) +
   `PartyStationRenderer.MountTraceRoute` + `StationWaypoint` tap component.
   FEEL FOLLOW-UP: currently tap-the-stops; upgrade to a continuous finger-drag
   tracer on the same zones/rules; route layout (zigzag arc) could be prettier.
2. **Shoot proof on Robotics** — NEXT. Pull-back-and-release launch at a target.
   Mirror the trace pattern: new `ToyPatternId`, a renderer affordance (launch
   arc / target), a small input component with a deterministic test seam, wire
   Robotics, EditMode + PlayMode gate, screenshot.
3. **Deduce proof on Newsroom** — after shoot. Tap-to-ELIMINATE candidates that
   break a clue until one remains (NOT drag-to-bin). New completion logic
   (distinct from the existing sort/match).
4. Roll each proven verb to its second station (trace→Weather, deduce→AI Lab).

Then Part B (toy art). Per-verb proof discipline: each lands as its own atomic
commit, EditMode+PlayMode green (combined run), plus a station screenshot.

### New-session pickup notes

- Source of truth: this file (locked verbs + map above).
- Pattern to copy from: the TracePath commit `85017ab` shows the full recipe
  (enum + rules order/target + renderer mount + input component + Spaceport
  wiring + the three test re-baselines).
- Gotchas captured in memory: Unity single-instance/headless detach (trust the
  results XML, not the launcher exit), TMP fallback churn (revert before
  commit), and Bash-tool commit messages (use `git commit -F`, not `@'...'@`).
- PlayMode has a pre-existing order-dependent flake (`AutoEntryPlayModeTests`
  passes in isolation + combined order, fails PlayMode-only) — gate on the
  combined EditMode+PlayMode run.

Part B (toy art) stays gated behind verbs (Gate 2). Wave scope (first-six vs
all-ten) confirmed per verb as it lands.

## Sequencing

- **Gate 1 (A):** verbs first — a "place ×10" campus with pretty toys still
  feels samey, so mechanics lead. Prove one new verb (e.g. Trace on Spaceport)
  end-to-end before building the rest.
- **Gate 2 (B):** toy art second — once the verbs are fun and stable, dress the
  toys. (Mirrors the original plan's KTD11: art after gameplay.)

Each unit ships as its own atomic commit with the standing gate: EditMode +
PlayMode green (combined run), plus a station screenshot.

## Effort (rough)

- Part A: human ~3-5 days / CC ~several focused sessions (new input handlers +
  tests + per-verb proof). The biggest unknown is verb input feel, which needs
  in-app iteration, not just screenshots.
- Part B: human ~2-3 days / CC ~1-2 sessions for generation + wiring + fit, art
  taste pending.

## What I need from you to start

1. Approve the **verb families** and the rough station→verb assignment (Part A
   open decision 1-2).
2. Approve the **toy-art source/style** (Part B open decision 1).
3. Confirm **scope**: first-six wave first, or all ten.

Until approved, this stays a proposal — no kit, definition, or art changes.
