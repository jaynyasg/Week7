---
date: 2026-06-17
topic: showcase-refresh
---

# Showcase Refresh Requirements (Reflect New Stations + Verbs)

## Summary

The in-game `Showcase` guided tour predates the largest week of feature work (the 10-station Party Pack and the diversified interaction verbs). Today it shows 5 beats and seeds only 3 results (Design Build, Logic Court, Health Hero), so the tour undersells the current game. This refresh makes the Showcase reflect the breadth of 10 career stations and the distinct verbs — without bloating the under-3-minute evaluator tour. Chosen approach: **a montage beat + a richer seed (Approach A)**.

This pairs with `docs/demo-video-script.md`, whose Section 1 narrates the updated tour.

---

## Problem Frame

`Showcase` exists to make the product thesis obvious in minutes. Since it was built, the game gained 10 definition-driven stations, ~7 distinct verbs (ShootTarget, TracePath, DeduceAnswer, BalanceMeters, ComposeSet, MatchAndCare, PickMatchingTrio), campus districts, and accessories. The tour still routes only the original three rooms, so an evaluator watching Showcase never sees the variety that is the game's strongest signal.

The fix is two coordinated edits: what the tour **visibly shows** (presenter beats) and what the Gallery/Reveal **reflect** (seed config).

---

## Key Decisions

- **One new montage beat, not ten station beats.** Add a single `stations` beat between `campus` and `build` that surveys representative stations and their distinct verbs as a quick visual. Keep the tour under 3 minutes; leave verb *depth* to the live-gameplay section of the demo video.
- **Expand the seed, preserve the reveal.** Expand `ShowcaseSeedConfig` so the Achievement Gallery shows a fuller badge wall and the Career Reveal DNA is broader — but the seeded result set must still produce the Architect + AI Engineer co-lead reveal (the established Creative Technical Builder profile).
- **Montage is presentation-only.** The montage surveys stations; it does not mount real interactive station surfaces and does not need a new auto-complete seam (that was the rejected Approach B).
- **No change to `Play`, careers, or privacy.** Free play, the career roster, and the privacy posture are untouched.

---

## Actors

- A1. Evaluator — watches Showcase; should now perceive station breadth and verb variety within the tour.
- A2. Presenter — records the demo video; needs the montage beat to back Section 1 narration.

---

## Key Flows

- F1. Updated tour order: `connection -> campus -> stations (NEW) -> build -> gallery -> reveal`.
- F2. Stations montage beat: a short, time-locked survey that presents several stations with their distinct verbs (e.g., Robotics/launch, Weather/trace, AI Lab/deduce, Green City/balance), reading as "a whole campus of different careers."
- F3. Richer Gallery + Reveal: with the expanded seed, the Gallery badge wall is fuller and the Reveal DNA spans more traits, while the headline result stays Architect + AI Engineer co-leads.

---

## Requirements

**Presenter sequence**

- R1. `ShowcasePresenter.BuildDefaultSequence()` must insert a new `stations` beat between `campus` and `build`.
- R2. `RouteStep()` must handle the `stations` beat by routing to a presentation-only montage of representative stations and their verbs.
- R3. The montage must visibly distinguish at least 3-4 different verbs (not 3-4 of the same action).
- R4. Total Showcase runtime must remain under 3 minutes (R6 of the original demo-wow requirements).
- R5. The montage is presentation-only: it must not mount interactive station play surfaces and must not record additional results by itself.

**Seed expansion**

- R6. `ShowcaseSeedConfig` must seed additional station results so the Achievement Gallery shows a fuller badge wall and the Career DNA spans more traits.
- R7. The seeded set must still resolve the Career Reveal to Architect + AI Engineer co-leads (Creative Technical Builder profile, Building/Spatial Thinking/Creativity/Reasoning/Collaboration emphasis). Choose stations whose trait deltas reinforce that profile (e.g., Robotics, AI Lab, Green City, Weather) rather than ones that would flip the lead.
- R8. Seeded badges must carry the `ShowcaseSeed` result source and must not introduce visible "seeded/tour" labels in the child-facing Gallery (original R17/R34).

**Tests / verification**

- R9. Update `Assets/_CareerQuest/Tests/EditMode/ShowcaseSequenceTests.cs` to expect the new beat at the correct index and the full ordered sequence.
- R10. Add coverage that the expanded `ShowcaseSeedConfig` set still produces an Architect + AI Engineer co-lead reveal (guard against an accidental profile flip).
- R11. EditMode + PlayMode suites must be green before ship (per the project's batchmode gate).

---

## Acceptance Examples

- AE1. Covers R1-R5. Given Showcase runs, when the tour reaches the new beat, then the evaluator sees several stations with visibly different verbs, and the whole tour still completes under 3 minutes.
- AE2. Covers R6-R8. Given Showcase reaches the Gallery, then more badges appear than the prior three, with no visible seeded/tour labels.
- AE3. Covers R7/R10. Given the expanded seed, when the Reveal plays, then it still spotlights Architect + AI Engineer as co-leads.
- AE4. Covers R9/R11. Given the sequence changed, then `ShowcaseSequenceTests` asserts the new ordered beats and the full test gate is green.

---

## Scope Boundaries

- No real auto-completing station surfaces in the tour (rejected Approach B).
- No new careers beyond the existing roster.
- No changes to `Play`, avatar selection, or the privacy posture.
- No new persistent state, accounts, analytics, or telemetry.

---

## Dependencies / Assumptions

- `CareerQuestApp.ShowPartyStation(string)` and `CampusWorldController.ShowPartyStation(...)` already exist; the montage can reuse station catalog data for art/labels without new gameplay systems.
- `CareerQuestCatalog` exposes station ids (e.g., `RoboticsGarageId`, `AiLabId`) used to pick representative stations.
- Implementation is a code change best run through `/ce-plan` -> `/ce-work` with subagents (per project CLAUDE.md), then verified via the Unity batchmode test gate. This brainstorm scopes WHAT; planning decides the montage's exact construction.

---

## Sources / Research

- `Assets/_CareerQuest/Scripts/Showcase/ShowcasePresenter.cs` — sequence + routing.
- `Assets/_CareerQuest/Scripts/Config/ShowcaseSeedConfig.cs` — seeded results.
- `Assets/_CareerQuest/Scripts/UI/CareerQuestApp.cs` — `ShowPartyStation`, `ShowGallery`, `ShowReveal`.
- `Assets/_CareerQuest/Tests/EditMode/ShowcaseSequenceTests.cs` — locked beat order.
- `docs/brainstorms/2026-06-09-demo-wow-showcase-requirements.md` — original Showcase intent and constraints (R6, R17, R20-R23, R34).
- `docs/demo-video-script.md` — the 5-minute video script this refresh supports.

---

## Implementation Note (2026-06-17)

Discovered during implementation: the seeded result COUNT is load-bearing. RevealSynthesis buckets 3-4 unique completions as `RevealStyle.Simple`, and that bucket is an asserted invariant in `RevealCinematicPlayModeTests` (style + exact beat timing) and `ShowcaseRevealFlowTests` (style + confidence). A seed of 5+ results flips the reveal to `RevealStyle.Rich` and rewrites the cinematic beat sequence, breaking those tests.

Decision: the reseed (R6) was scaled to **+1 station (Robotics Rescue, Degree → 4 completions)** so the Gallery gains a badge and Career DNA broadens toward Building/Spatial/Reasoning while staying in the `Simple` bucket — no existing test is disturbed. Co-leads verified: Architect 137 / AI Engineer 134 (next career 111). The visual breadth of all four verbs is carried by the montage beat (R1-R5), not the seed. A future "Rich reveal" reseed is possible but must retune the style/beat tests under a Unity run (the gate could not run here because the Editor held the project lock).
