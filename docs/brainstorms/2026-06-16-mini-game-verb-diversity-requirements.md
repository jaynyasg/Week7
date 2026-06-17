# Mini-Game Verb Diversity — Requirements

**Date:** 2026-06-16
**Status:** Ready for planning (`/ce-plan`)
**Scope:** Standard (feature) — extends the existing Party Pack mini-game system

## Problem

The 10 party stations are defined as distinct experiences, but only **7 of the
10 `ToyPatternId` verbs are used**, and **3 are duplicated** across stations:

- `DeduceAnswer` -> AI Lab **and** Newsroom
- `TracePath` -> Weather Lab **and** Spaceport
- `ComposeSet` -> Music Studio **and** Game Studio

Three patterns (`DragToSlot`, `SortToBin`, `SequenceCards`) exist in code but no
station uses them. The result: stations feel same-y (a player reads Weather Lab
and Spaceport as "the same game"), and the *felt* verb count is ~4
(drag-drop, launch, tap-out, slider) despite 10 stations.

## Goal

Make all 10 stations play distinctly by **adding 4 new input verbs** and using
them to **resolve the 3 duplicates**. Each station keeps **one fixed verb**
(no rotation in this pass).

Success = all 10 stations map to **10 distinct verbs**; the 4 new verbs ship
with their own input components and tests; badge/scoring behavior is unchanged.

## Architecture context (why this is feasible)

Every interaction funnels through one validated seam:
`ToyPatternRules.Submit(ToyAction(pieceId, targetId, value))`
(`Assets/_CareerQuest/Scripts/Interaction/ToyPatternRules.cs`).

A "mini-game verb" is really four things bundled:

1. **Input verb** — the spatial/timing skill, lives in the *input component*
   (e.g. the launcher for `ShootTarget`). This is the new build per verb.
2. **Target topology** — `ComputeExpectedTarget` in `ToyPatternRules.cs`
   (per-object slot vs. one shared goal vs. waypoints vs. pairs).
3. **Order rule** — `IsOrderSatisfied` in `ToyPatternRules.cs`.
4. **Completion condition** — `Complete` (accepted set full + meters green).

Because the input component produces only a validated `(piece, target, value)`
tuple, **scoring and badges are already verb-agnostic** — completion tier +
accuracy + time remaining (`PartyStationController.BuildResult`,
`Assets/_CareerQuest/Scripts/Core/MiniGameResult.cs`). New verbs need new input
components and new `ExpectedTarget`/order cases, **not** new scoring.

## Final station -> verb map

Exactly 4 stations change — the minimum that introduces all 4 new verbs and
clears all 3 duplicates as a side effect.

| Station | Verb | Change |
|---|---|---|
| Robotics Garage | `ShootTarget` | keep |
| AI Lab | `DeduceAnswer` | keep (no longer duplicated) |
| **Community Kitchen** | **`PourToLine`** | new (was `PickMatchingTrio`) |
| **Music Studio** | **`RhythmTap`** | new (was `ComposeSet`) |
| Vet Clinic | `MatchAndCare` | keep |
| Game Studio | `ComposeSet` | keep (dup resolved) |
| Weather Lab | `TracePath` | keep (dup resolved) |
| **Spaceport** | **`WireUp`** | new (was `TracePath`) |
| **Newsroom** | **`ScanReveal`** | new (was `DeduceAnswer`) |
| Green City | `BalanceMeters` | keep |

Resulting distinct verbs (10): `ShootTarget`, `DeduceAnswer`, `PourToLine`,
`RhythmTap`, `MatchAndCare`, `ComposeSet`, `TracePath`, `WireUp`, `ScanReveal`,
`BalanceMeters`.

## The 4 new verbs

Each maps to the existing seam as noted. Input feel is the new work; rules-layer
work is a new `ToyPatternId` plus `ComputeExpectedTarget`/`IsOrderSatisfied`
cases.

### `RhythmTap` — tap on the beat (Music Studio)
- **Feel:** taps land in time with a beat/metronome; `value` carries timing
  accuracy.
- **Target topology:** one shared "beat" target, any order (like `ShootTarget`).
- **Order:** any order.
- **Completion:** all required beat-taps accepted (each must clear a timing
  threshold in the input component before it submits).

### `PourToLine` — press-and-hold to fill (Community Kitchen)
- **Feel:** hold to fill a gauge into a green band; release at the right level.
- **Target topology:** reuses meter green-band logic
  (`MeterGreenMin`/`MeterGreenMax`), one target per pour.
- **Order:** any order.
- **Completion:** all pours land in their green band (mirrors `BalanceMeters`
  completion, new input).

### `WireUp` — connect matching pairs (Spaceport)
- **Feel:** drag a wire from node A to its matching node B.
- **Target topology:** **pairs** — each draggable's expected target is its
  partner node (new prefix, e.g. `wire.{partnerId}`).
- **Order:** any order.
- **Completion:** all pairs connected.

### `ScanReveal` — investigate to reveal, then tap (Newsroom)
- **Feel:** drag a tool (scanner/magnifier) over a scene to reveal hidden
  items, then tap each revealed item.
- **Target topology:** per-item reveal zone; tap submits the revealed item.
- **Order:** any order (reveal then confirm).
- **Completion:** all hidden items revealed and confirmed.

## In scope

- 4 new `ToyPatternId` values + `InstructionStrip` lines.
- 4 new input components (the per-verb spatial/timing skill).
- `ComputeExpectedTarget` + `IsOrderSatisfied` cases for each new verb in
  `ToyPatternRules.cs`.
- Reassign the 4 stations in
  `Assets/_CareerQuest/Scripts/Config/PartyStationDefinitions.cs`
  (objects, roles, prompts, success rules, instruction strings).
- Tests: rules-layer tests for the 4 new patterns **plus** play-mode/input
  tests for the timing/continuous verbs (see assumptions).

## Out of scope

- **Seed-based verb rotation** — chosen design is fixed assignment. Keep data
  shapes compatible so rotation *could* be layered later, but do not build it.
- **The unused patterns** `DragToSlot`, `SortToBin`, `SequenceCards`, and the
  now-freed `PickMatchingTrio` — left as future/fallback, not wired up.
- **Tier-3 physics verbs** (Stack & Balance, Coverage/Plant) — deferred.

## Dependencies / Assumptions

- **New test surface.** `PourToLine`, `WireUp`, and `RhythmTap` introduce
  timing/continuous input the current all-discrete-drop test suite does not
  cover. Each new input component needs its own play-mode/input tests — the
  rules-layer tests will not catch a mis-timed beat or a half-drawn wire.
- **Art/objects per station.** Reassigned stations need object sets that read
  for the new verb (e.g. wire nodes for Spaceport, pour vessels + gauge for
  Community Kitchen). Reuse curated Kenney art where possible.
- **Scoring is verb-agnostic and stays so** — do not add per-verb scoring; if a
  new verb cannot express completion/accuracy/time through the existing
  `MiniGameResult`, that is a flag to revisit, not to fork scoring.
- **`ToyAction.value` is the timing/level channel** for `RhythmTap` and
  `PourToLine`; confirm the input components populate it and the rules read it.

## Open questions for planning

- Does `WireUp`'s pair topology need a new target-prefix constant, or can it
  reuse an existing per-object target with a partner lookup?
- For `ScanReveal`, is "reveal" a separate accepted step from "tap", or does the
  reveal gate the tap inside one submit? (Affects required-count math.)
- Which of the freed patterns (`PickMatchingTrio`) should be formally retired
  vs. kept available for a future station.
