# Brainlift — Career Quest Campus

A daily build log for the rubric's learning/process requirement: **what shipped each day, and which AI prompts/skills helped.** Career Quest Campus is a kid-facing, multiplayer Unity game where a child explores a handmade "Future Workshop" campus, plays short career mini-games (each a distinct verb), earns badges and a Career DNA reveal. Built 2026-06-08 → 2026-06-17.

Stack: Unity `6000.4.10f1`, C#, Netcode for GameObjects + Unity Transport, Windows-first. All art is procedurally generated in-editor (Kenney CC0 + code-drawn sprites) — no hand-authored scene.

## How AI was used (the workflow)

The whole project ran on an agentic loop in **Claude Code** with the **gstack / compound-engineering** skill ecosystem. The repeating cycle, captured in `docs/brainstorms/`, `docs/plans/`, and `docs/qa/`:

```
brainstorm → plan (CEO / eng / design review) → autoplan → implement (subagents)
   → design-review (live screenshots) → QA / 2P smoke → ship → compound (document learnings)
```

The AI "prompts" that did the heavy lifting are slash-command skills, each a reusable prompt:

- **`/office-hours`, brainstorming** — turn a fuzzy idea into a requirements doc.
- **`/plan-ceo-review`, `/plan-eng-review`, `/plan-design-review`, `/autoplan`** — stress-test scope/architecture/design before building; produced the dated plans.
- **`/design-review`** — drive a built player headless, screenshot each screen, and fix visual gaps. This single skill drove design reviews #3 (distinct game verbs) and #4 (per-station toy art), and today's campus-polish pass.
- **`/qa`, 2P smoke harness** (`-cq-smoke`) — machine-verified single- and two-player loops.
- **`/ship`, `/gauntlet-submit`** — package and gate the submission.
- **`/ce-compound`, `/ce-sessions`** — document a solved problem into `docs/solutions/` so the next occurrence is a lookup, and mine prior sessions for context.

Verification ran on Unity batchmode (`-runTests` EditMode + PlayMode, gated on the combined green run) plus the `-cq-visual-state`/`-cq-screenshot` capture harness for visual proof.

## Daily log

### 2026-06-08 — Bootstrap
Shipped: repo + Unity project at root, project bootstrap, initial scaffolding. (`docs/qa/2026-06-09-unity-bootstrap.md`)
AI: brainstorming + repo/architecture setup prompts; tech-stack rationale (Unity vs Godot/Phaser/Three.js) written to `README.md` / `docs/architecture.md`.

### 2026-06-09 — Core loop
Shipped: the end-to-end showcase loop — SceneFlowRouter, AssetCatalog with procedural fallbacks, selectable sprite identities, playable campus navigation, shared activity lifecycle, generated sprite-kit baseline, Design Build HUD.
AI: `/office-hours` + brainstorming → `docs/brainstorms/2026-06-09-*-requirements.md`; planning skills → `docs/plans/2026-06-09-*-plan.md`. Implementation by Claude Code with subagents.

### 2026-06-10 — Foundations + CEO-approved plan
Shipped: visual foundation pass; `CampusSessionState` spine with a join-lock for 2-player Play.
AI: `/plan-ceo-review` → `docs/plans/2026-06-10-career-quest-full-vision-ceo-plan.md` + `…-ceo-review-handoff.md` (scope locked before the big build).

### 2026-06-11 — Wow Quality Pass (U1–U8)
Shipped (20 commits): Kenney CC0 import pipeline + import hygiene, TextMeshPro migration with a Fredoka/Lexend type system, single-owner `CameraDirector`, authored parallax campus-hub prefab with ambient motion, frame-animated characters + name tags + speech bubbles, the drag-and-drop framework + Design Build conversion, in-world cinematic Career Reveal, three-tier `AudioDirector`, `NetcodePlayModeHarness` + LAN matrix tests, `ShipLadder` + sprite fallback gate, mandatory completion ceremony, `InstructionStrip`.
AI: `/autoplan` + `/plan-eng-review` → `docs/plans/2026-06-11-001-feat-wow-quality-pass-plan.md`; `/design-review` → `docs/qa/2026-06-11-flagship-slice-review.md`.

### 2026-06-12 — Wow pass finalized + Party Pack spine (U1–U6)
Shipped (22 commits): Health Hero + Logic Court drag conversions with seeded shuffle, zero-fallback optional rooms, passport gallery + campus-evolution fanfare, hub toys + synced emotes + partner drag indicator, pause menu / persisted settings / build identity / paper HUD, reveal-latch + RPC hardening, automated two-process 2P matrix. Then the Party Campus Pack foundation: 10-station definition spine + validator, generic station-id routing with walk-into-door auto-entry (dwell/latch/return-grace), `ToyInteractionKit` (7 shared toy-pattern rules, host-validated network state), `PartyStationController` + Robotics Rescue proof, first six stations, reward events + derived accessories.
AI: `/autoplan`, `/plan-design-review` → `docs/designs/party-campus-pack.md` + `docs/plans/2026-06-12-party-campus-pack-implementation-plan.md`; `/qa` → `docs/qa/2026-06-12-wow-pass-final.md`.

### 2026-06-13 — Wave 2 all-10 + distinct verbs
Shipped (19 commits): reveal synthesis + combo resolver + career families, ten-station district layout with evolution pieces, PartyRun cadence + classroom access (quiet/reduced-motion) + facilitator controls, all-10 stations playable through the shared spine, final accessory art + fit tuning. Then design-review #3 — distinct game **verbs**: TracePath proof on Spaceport, ShootTarget proof on Robotics.
AI: `/design-review` surfaced "the games all feel the same" and drove the verb plan → `docs/plans/2026-06-13-party-distinct-mechanics-and-toy-art.md`; `docs/qa/2026-06-13-party-campus-pack-proof.md` (EditMode 237/237, PlayMode 229/229).

### 2026-06-14 — Third verb + submission audit
Shipped: DeduceAnswer (tap-to-eliminate) proof on Newsroom — all three verbs built; rolled trace→Weather Lab + deduce→AI Lab so no two adjacent stations share a verb; avatars-chosen-before-campus-spawn fix. Audited all 10 early-submission gates to PASS.
AI: `/design-review` (verb rollout); `/gauntlet-submit` discipline → `docs/qa/2026-06-14-early-submission-requirements-audit.md`.

### 2026-06-15 — Toy art + campus polish + compounding
Shipped: Kenney-soft per-station toy art for all ten stations (parallel session); verb-aware instruction strip (the HUD now names trace/shoot/deduce); six "Soon" construction-scaffold stations turned into real enterable buildings; Quest Yard de-cluttered to one name per door. Gate green (EditMode 241/241, PlayMode 233/233). Documented the core lesson into `docs/solutions/`.
AI: `/design-review` (caught "I don't see the new toys / ways to solve / it's clunky") drove the four fixes, each verified with before/after `-cq-visual-state` screenshots; `/ce-sessions` mined the 06-13 session; `/ce-compound` wrote `docs/solutions/design-patterns/data-display-seams-invisible-content-2026-06-15.md`.

### 2026-06-16 — De-compact the campus + four more verbs + scene subjects
Shipped (19 commits): repo documentation gate for the Game Week rubric; a bigger, spread-out campus — color-coded district roads, glowing doormats at every door, Tech Lane & Story Street spread into tall reachable triangles, Care Corner raised above the HUD strip, calmer central roads; final station art for the 6 net-new Party Pack stations plus Kenney-art "scene subjects" (the creature each mini-game's copy names) tucked inside the room panels; readable building entrances that stop accidental entry; avatar accessories anchored to the visible body (not the padding) and gear kept attached through the reveal celebrate jump; robotics launch parts made grabbable. Then **four more distinct verbs** beyond the first three — RhythmTap, PourToLine, WireUp, ScanReveal — each with its own input component (beat / pour / wire / scan).
AI: `/design-review` drove the campus de-compacting and entrance-readability passes (before/after `-cq-visual-state` frames); the new verbs continued the "variety lives in the verb" thread from 06-13.

### 2026-06-17 — Difficulty tuning + campus scale + design-review polish
Shipped (4 commits): mini-games made less obvious — Future City tray deranged so no piece sits in the slot under its matching lot, party-station trays + AI Lab deduction cards + Spaceport/Newsroom boards shuffled, and an extra distractor rule added to each AI Lab seed (`ContentShuffle.DeriveDerangement` + tests). Campus scaled down (buildings −10%, player character −20%) with the character raised above the door labels so it reads in front. A `/design-review` pass then fixed three player-reported issues — the bottom instruction strip lowered off the Care Corner door labels, the Music tempo dial moved off the center beat pad, and both characters spawned on the central plaza — plus a toy-art bug where the Rain Shaker beat showed a warning icon instead of a music note (the art builder matched "rain" to the weather glyph group).
AI: `/design-review` (user-invoked) drove the three layout fixes and surfaced the Rain Shaker art bug, each verified with before/after `-cq-visual-state` captures; EditMode 250/250 + PlayMode 235/235 green throughout.

## Key learnings (the spiky bits)

- **Built + test-green ≠ player-visible.** In a code-assembled scene the data layer and the display layer are separate seams; logic tests stay green while the player sees placeholder scaffolds, generic copy, and placeholder-circle art. The fix is a *visual* acceptance artifact (headless screenshot), not more logic tests. Full write-up: `docs/solutions/design-patterns/data-display-seams-invisible-content-2026-06-15.md`.
- **Variety has to live in the verb, not the validation rules.** Seven distinct `ToyPatternId` rule-sets still all reduced to "drag a token onto a pad" — invisible to a 7-year-old. The win was three genuinely different input verbs (trace a path, pull-back-and-launch, tap-to-eliminate), not more content.
- **`/design-review` with real screenshots is the highest-leverage AI prompt for a game.** Most of the "it feels off" gaps (samey games, "Soon" masking, doubled labels, clunky layout) were invisible to me and to the test suite, but obvious in a captured frame. Driving a built player headless and diffing before/after frames is what made each fix verifiable.
- **Plan-before-build paid off.** The CEO/eng/design review skills caught scope and architecture problems on paper (cheap) instead of in code (expensive) — the dated `docs/plans/` are the audit trail.
- **AI lets you "boil the ocean."** Subagents made it cheap to do the complete thing (all 10 stations, full 2P matrix, edge cases, docs) rather than just the demo path.

## Evidence trail

- Requirements: `docs/brainstorms/` · Plans + reviews: `docs/plans/` · QA + proof: `docs/qa/` · Designs: `docs/designs/` · Compounded learnings: `docs/solutions/` · Submission: `SubmissionBundle/`.
- Git history is the ground truth: `git log --date=short --pretty=format:"%ad %s"`.
