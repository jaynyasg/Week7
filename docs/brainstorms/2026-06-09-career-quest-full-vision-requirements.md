---
date: 2026-06-09
topic: career-quest-full-vision
---

# Career Quest Full Vision Requirements

## Summary

Career Quest Campus should become a credible first playable: a kid-friendly Unity career campus with generated sprite art, avatar selection, a playable hub, room-scale mini-games, all-three multiplayer support, a three-game Career Reveal gate, visual QA evidence, and a Windows-first release. The next pass is hybrid visual-first: prove the game can look and feel good through real characters, a polished campus, a cleaner Play flow, and one beautiful room template before broadening the rest of the systems.

---

## Problem Frame

The current build has the right loop on paper: avatar choice, campus, mini-games, gallery, reveal, and multiplayer proof. The remaining gap is experiential. It still risks reading like a UI-driven prototype with procedural scenery rather than a video game where a player inhabits a world, moves a character, performs actions, and earns a reveal through play.

The full-vision pass exists to turn the existing loop into a real first playable without losing the privacy boundaries, reveal semantics, or Netcode proof already established. The goal is not to build the entire 12-month campus. It is to make the first campus slice feel real, coherent, testable, and releasable.

---

## Key Decisions

- **Real first playable over small polish.** The next pass should address visual identity, movement, interactivity, multiplayer, and proof together because the central gap is "does this feel like a game?"
- **Dual-path release.** `Play` is the normal first-run game path; `Showcase` remains available for guided evaluation and reliable presentation.
- **Hybrid visual-first execution.** The next implementation slice prioritizes visible game quality first and includes only the shared activity architecture needed to make the first polished room reusable.
- **Real character bar.** Main-path avatars and NPCs must read as real 2D video game characters with face/head detail, body, limbs, outfit/personality, and clear silhouette at gameplay scale.
- **Fallbacks are not final art.** Procedural and fallback sprites keep QA playable, but they do not satisfy visual completion for player-facing characters, buildings, room backdrops, primary props, badges, or icons.
- **Three-game reveal gate.** Career Reveal unlocks only after three unique mini-games; one or two results build progress but do not reveal the final path.
- **Generated/imported sprite kit with fallbacks.** Generated art is acceptable for the first playable, but every major asset category needs fallback behavior so the game never shows invisible characters or blank rooms.
- **Playable hub over campus menu.** Campus navigation should happen through avatar movement, building entrances, camera framing, and a guide/NPC presence rather than only through screen buttons.
- **Normal Play bypasses connection setup.** Multiplayer and connection modes remain available as secondary testing/evaluator flows, but the kid-facing path should be `Play -> Avatar Selection -> Campus`.
- **Room-scale mini-games.** Design Build, Health Hero, and Logic Court should feel like themed activity rooms with visible objects and player actions, not staged button checklists.
- **All-three multiplayer, sequenced carefully.** All mini-games should support multiplayer, but Design Build is the first protected multiplayer wow moment and stabilizes before Clinic/Court synchronization.
- **Prefab/entity rewrite inside one persistent scene.** The build should move toward Unity-native prefabs and entities while keeping one persistent gameplay scene to avoid Netcode scene-transition risk.
- **Host-authoritative activity state.** Shared mini-game state is validated by the host, syncs visible progress, emits one result, and lets `GameSession` remain the source for Career DNA and reveal readiness.
- **Windows-first release.** Windows is the authoritative proof artifact. WebGL remains optional and only ships if it behaves clearly.
- **Approach B — Full Loop Parity (All-In Parallel).** Walkable campus, all three room-scale activities on shared `ActivityRoomController`, Play + Showcase parity, visual-first proof with full generated/imported sprite kit synced to Resources. CEO-approved 2026-06-10 (SELECTIVE EXPANSION).
- **Ceremony stack.** Shared completion ceremony on all three rooms via `CeremonyController` + `FeedbackController` + `AudioCueCatalog`; hybrid pacing (~12s cap, Skip after 3s, host-synced 2P).
- **Session spine.** Host-authoritative `CampusSessionState` with local `GameSession` mirror; ceremony spine lands before room controller refactors.
- **Optional hub rooms in parallel.** Optional buildings (kitchen, music, robotics, etc.) ship in the same wave priority as core three — not deferred past first playable.
- **Multiplayer ceiling.** Design campus for four local avatar slots; ship and test two-player only.
- **Reveal gate catalog.** Extensible `CareerQuestCatalog`; reveal unlocks when completed unique activity count ≥ 3 (configurable).
- **Shared kid instructions.** `InstructionStrip` on hub and every activity room (≤8-word line + verb CTA icon).
- **Visual cohesion scheduling.** Wave 4 dedicated campus-wide polish pass after Waves 1–3 (HUD chrome, parallax budget, evaluator screenshot set).

---

## CEO Review Locks (2026-06-10)

**Status:** CEO_APPROVED — SELECTIVE EXPANSION  
**Canonical plan:** `docs/plans/2026-06-10-career-quest-full-vision-ceo-plan.md`  
**Next:** `/plan-eng-review` → `/ce-plan` → implement (no code until eng review + plan complete)

### Brainstorm Q1–Q10 (locked)

| # | Decision |
|---|----------|
| Q1–Q5 | Play/Showcase parity; visual-first scope; walkable hub; action bar hidden in Play; avatar before campus |
| Q6 | All three rooms share the same completion ceremony template this week |
| Q7 | Real walk/idle sprite sheets this week |
| Q8 | Local multiplayer on walkable campus |
| Q9 | Keep three unique completed games to unlock Career Reveal |
| Q10 | All three activities migrate to `ActivityRoomController` with in-world direct manipulation |

### Step 0D cherry-picks (all ACCEPTED)

| ID | Expansion |
|----|-----------|
| `exp1_living_campus` | Ambient motion / living hub |
| `exp2_ambient_hub_sfx` | Hub ambient SFX |
| `exp_ceremony_polish` | Full DESIGN.md ceremony on all three rooms |
| `exp_gallery_reveal_polish` | Gallery badge wall, Reveal curtain/cards, synced 2P celebration |
| `exp5_guide_dialogue` | Guide contextual speech bubbles |
| `exp6_campus_evolution` | Growing skyline via `prop.city_piece_*` |

### Step 0E (locked)

- Hub avatar motion: sprite sheets (Q7)
- Ceremony: shared `CeremonyController` stack + `AudioCueCatalog`
- Multiplayer sync: campus positions + activity state + ceremony moments (host-authoritative)
- Parallax: rich (4+ layers + ambient motion)
- Art pipeline: full catalog batch — generate/import entire sprite kit, one integration pass

### Sections 10–11 (trajectory + UX)

| Issue | Decision |
|-------|----------|
| S10-1 Optional rooms | **C** — parallel with core three (kitchen, music, robotics, etc.) |
| S10-2 Multiplayer ceiling | **B** — design for 4 local slots; ship/test 2P only |
| S10-3 Post-reveal growth | **A** — `CareerQuestCatalog`; gate = count ≥ 3 |
| S11-1 Instructions | **A** — shared `InstructionStrip` (hub + all rooms) |
| S11-2 Ceremony pacing | **Hybrid** — 3 beats, richer animation, ~12s cap, Skip after 3s, host-synced 2P |
| S11-3 Polish scheduling | **A** — Wave 4 visual cohesion after Waves 1–3 |

### Implementation waves (`/ce-plan` input)

| Wave | Deliverable |
|------|-------------|
| 1 | `CampusSessionState` + ceremony stack + 2P harness + `InstructionStrip` + art batch + tiered warmup |
| 2 | Design Build / Health Hero / Logic Court on `ActivityRoomController` + shared chrome + per-room net state |
| 3 | Optional hub rooms (S10-1 C) + `CareerQuestCatalog` |
| 4 | Visual cohesion (S11-3) + `CareerQuestBuild.ShipLadder` + `SubmissionBundle/` |

---

## Actors

- A1. Child player
  - Wants to choose a character, explore a friendly world, play with visible objects, and feel encouraged rather than judged.
- A2. Second local player
  - Joins the same-computer session, sees both players in the hub, and contributes to shared activity outcomes.
- A3. Evaluator
  - Needs to quickly understand the game loop, multiplayer proof, career-discovery purpose, and polish level.
- A4. Presenter
  - Uses Showcase when a reliable guided route is safer than free play under time pressure.
- A5. Builder or future maintainer
  - Needs requirements that preserve the playable loop while art, hub, rooms, multiplayer, and QA evolve.

---

## Key Flows

- F1. Play first-run route
  - **Trigger:** A player chooses `Play`.
  - **Actors:** A1, A2
  - **Steps:** The player selects an avatar, enters the hub directly in solo play, moves through the campus, enters three activity buildings, completes mini-games, views Gallery progress, then unlocks Career Reveal after three unique games.
  - **Outcome:** The player experiences Career Quest as a game path rather than a menu tour.

- F2. Showcase evaluator route
  - **Trigger:** An evaluator or presenter chooses `Showcase`.
  - **Actors:** A3, A4
  - **Steps:** A short disclaimer appears, then Showcase guides through avatar/campus, multiplayer proof, activity beats, Gallery, and Reveal without hiding the existence of normal `Play`.
  - **Outcome:** The project can be understood reliably under demo pressure.

- F3. Playable hub exploration
  - **Trigger:** A player arrives in the campus hub.
  - **Actors:** A1, A2
  - **Steps:** The selected avatar moves through a bright 2.5D campus, the camera frames the character, buildings have visible entrances, and the guide/NPC helps orient available activities.
  - **Outcome:** The campus reads as a place to inhabit and explore.

- F4. Room-scale activity loop
  - **Trigger:** A player enters Design Build, Health Hero, or Logic Court.
  - **Actors:** A1, A2
  - **Steps:** The room shows themed art and interactable objects. The player makes multiple meaningful decisions, receives visual/audio feedback, and completes with either a Degree or Practice result.
  - **Outcome:** Career DNA progress feels earned through play.

- F5. Shared multiplayer activity
  - **Trigger:** Host/client players enter a shared activity.
  - **Actors:** A1, A2
  - **Steps:** Player actions submit to host authority, valid changes sync visibly, conflicts are handled gently, and the activity records one final result.
  - **Outcome:** Multiplayer is a real shared play moment rather than only a connection proof.

- F6. Gallery and three-game reveal
  - **Trigger:** A player completes mini-games and opens Gallery or Reveal.
  - **Actors:** A1, A3
  - **Steps:** Gallery shows earned stamps and Career DNA progress. Reveal remains locked until three unique mini-games are complete, then presents strength-based career paths and confidence language.
  - **Outcome:** The payoff feels celebratory, exploratory, and earned.

- F7. Multiplayer/testing route
  - **Trigger:** A tester, presenter, or second player chooses a multiplayer/testing option.
  - **Actors:** A2, A3, A4
  - **Steps:** The player opens connection options, chooses solo fallback, host, join-this-PC, or join-by-IP, then enters the same campus and activity flow.
  - **Outcome:** Multiplayer remains available without confusing the normal child-facing Play path.

---

## Requirements

**Visual Identity And Art**

- R1. The first playable must use generated/imported 2D sprite art for avatars, guide/NPC presence, campus buildings, activity rooms, props, badges, and reveal ceremony pieces.
- R2. Every major screen must include character presence and environment art; no first-run screen should read as a blank panel or text-only UI.
- R3. Generated/imported art must be curated informally for safety, consistency, and obvious brand/IP issues before import.
- R4. Every major asset category must have a fallback sprite or fallback visual so missing art does not make the game blank or invisible.
- R4a. Player-facing visual completion requires non-fallback art for selected avatars, guide/NPCs, primary campus buildings, room backdrops, primary props, badges, and core UI icons.
- R4b. Main-path avatars and NPCs must look like real 2D game characters: readable face/head detail, hair or headwear, torso, arms, legs, outfit/accent, personality, and clean silhouette.

**Avatar And Hub**

- R5. Avatar selection must appear before normal Play and Showcase entry into the campus.
- R6. The selected avatar must remain visually recognizable in the hub and activity contexts.
- R7. The campus hub must support avatar movement, visible buildings, building entrances, guide/NPC orientation, Gallery destination, Reveal destination, and exit access.
- R8. The hub camera should follow the active avatar within bounded campus framing while activity rooms use fixed room framing.
- R9. Campus buttons may remain as fallback or debug affordances, but the primary Play path should feel like walking to places.
- R9a. Normal Play must route from avatar selection directly into the campus; connection mode choice belongs behind a secondary multiplayer/testing action.

**Mini-Games**

- R10. Design Build must become the first deep room-scale activity and the protected multiplayer wow moment.
- R11. Health Hero and Logic Court must become room-scale activities with visible objects, at least three interactive decisions, success and practice outcomes, and a return path to hub.
- R12. Mini-games should use mouse-first direct manipulation where it best fits the activity: placement, tool choice, evidence sorting, and argument selection.
- R13. Each mini-game must emit the shared mini-game result contract: activity id, tier, source, trait deltas, time remaining, accuracy, and summary.
- R14. Replay must preserve best-result replacement and must not inflate Career DNA totals.

**Multiplayer**

- R15. Same-computer host/client remains the required multiplayer proof path.
- R16. All three mini-games must eventually support multiplayer state synchronization, with Design Build implemented first and Clinic/Court added after that path is stable.
- R17. Shared activities must use host-authoritative state for meaningful actions, conflict handling, completion, and result recording.
- R18. Shared activities must emit exactly one final result per completed attempt, even if callbacks or client messages repeat.
- R19. Multiplayer conflicts must produce visible, gentle feedback rather than silent failure.
- R20. Solo and Solo Fallback must remain available and clearly labeled where multiplayer is not running.

**Gallery, Career DNA, And Reveal**

- R21. `GameSession` remains the only source of Career DNA totals, best results, and reveal readiness.
- R22. Career Reveal must remain locked until three unique mini-games are complete.
- R23. Gallery must show progress toward the three-game reveal gate clearly.
- R24. Reveal copy must remain strength-based and exploratory, never deterministic or life-assigning.
- R25. Close career ties must be shown as co-leads where the scoring model supports it.

**Game Feel And Ceremony**

- R26. The game must include visible feedback for hover, accepted placement, invalid choices, badge earning, avatar celebration, activity completion, and Career Reveal.
- R27. Simple sound effects and fanfare should support key actions, with the game still understandable if audio is unavailable.
- R28. Feedback should appear for both clients when the underlying action is shared.

**QA And Evidence**

- R29. Each milestone must leave the game launchable and preserve the existing playable loop until its replacement path is working.
- R30. QA must combine automated tests/smokes, screenshots or short clips, and manual same-computer host/client evidence.
- R31. Visual evidence must cover avatar selection, hub movement, every mini-game, Gallery, Reveal, exit, and host/client proof.
- R32. Performance and build evidence must track FPS watchpoints, texture-size expectations, build-size changes, startup smoke, and network smoke.
- R32a. Visual QA evidence must include 1280x720 screenshots or clips for avatar selection, campus, the first polished activity room, Gallery, locked Reveal, and unlocked Reveal, with any fallback art explicitly noted as a blocker.

**Release And Distribution**

- R33. The Windows build is the authoritative release proof.
- R34. WebGL remains optional and should only ship if it loads reliably and limitations are clearly stated.
- R35. Release materials must include screenshots or clips, controls, fallback notes, known limitations, and privacy boundaries.
- R36. `Showcase` must remain available as a guided evaluator path, but it must not leak seeded assumptions into normal `Play`.

**Privacy And Trust**

- R37. The game must not add accounts, chat, analytics, telemetry, saved child profiles, persistent child data, or child-identifying data.
- R38. Any display names or identity-like labels must remain session-only.
- R39. Debug/QA metadata may mark Showcase or seeded state, but child-facing Gallery and Reveal should remain celebratory and uncluttered.

---

## Acceptance Examples

- AE1. **Covers R1-R9.** Given a new player starts `Play`, when they choose an avatar and enter the hub, then they see a character-led campus with movement, buildings, guide/NPC presence, and no blank first-run screens.
- AE2. **Covers R10-R14.** Given a player enters each mini-game, when they complete the room challenge, then the activity produces one Degree or Practice result through visible interactions rather than a text-only checklist.
- AE3. **Covers R15-R20.** Given host and client enter a shared activity, when both players make meaningful actions, then the host validates shared state, both clients see progress, conflicts give feedback, and only one result records.
- AE4. **Covers R21-R25.** Given one or two unique games are complete, when the player opens Reveal, then it remains locked with progress shown; after the third unique game, Reveal unlocks with strength-based copy.
- AE5. **Covers R26-R28.** Given a player completes a correct action or invalid choice, when feedback plays, then visual ceremony clearly communicates the outcome and shared feedback appears on both clients where relevant.
- AE6. **Covers R29-R32a.** Given a milestone is considered done, when QA evidence is reviewed, then automated proof, 1280x720 visual evidence, fallback-art notes, and manual host/client notes exist for that milestone's promised surface.
- AE7. **Covers R33-R39.** Given the release package is prepared, when an evaluator opens the Windows build or itch page materials, then controls, limitations, fallback notes, privacy boundaries, and optional WebGL status are clear.
- AE8. **Covers R1-R4b, R32a, SC8.** Given reviewers inspect avatar, campus, and first polished room screenshots, when they compare them to the design system, then the main avatar and NPCs read as real 2D game characters and the environment reads as a playable campus/room rather than procedural placeholder shapes.
- AE9. **Covers R5-R9a.** Given a child chooses normal `Play`, when avatar selection is confirmed, then the player enters the campus directly and connection choices are only visible through the secondary multiplayer/testing path.

---

## Success Criteria

- SC1. The first minute of Play proves avatar choice, character movement, campus world, and activity entry without relying on explanatory docs.
- SC2. A reasonable evaluator can describe the core loop as: choose avatar, explore campus, play three career challenges, earn badges, reveal career paths.
- SC3. The game looks like a kid-friendly 2D/2.5D game rather than a decorated UI prototype.
- SC4. Design Build provides a reliable shared multiplayer wow moment before Clinic/Court multiplayer expands.
- SC5. Career Reveal unlocks only after three unique games and remains strength-based.
- SC6. Every major screen has screenshot or clip evidence before release.
- SC7. Windows build is playable as the main proof artifact; WebGL is either absent or clearly labeled as optional/limited.
- SC8. Avatar, campus, and first polished room screenshots show real game-character art and recognizable environments, not procedural block placeholders.

---

## Scope Boundaries

**Deferred for later**

- Career clusters and curriculum depth beyond the first playable campus set (individual optional rooms are **in scope** in parallel with core three — see S10-1 C).
- Persistent progression, long-term profiles, rewards across sessions, or player accounts.
- Automatic LAN discovery or internet matchmaking after same-computer/LAN proof is stable.
- Separate Unity scenes or additive scene loading for each activity after the persistent-scene first playable is stable.

**Outside this product's identity for this pass**

- Chat, analytics, telemetry, saved child data, persistent child profiles, or child-identifying tracking.
- Real AI/LLM career recommendations or live profiling.
- Deterministic "you should become X" reveal language.
- Required WebGL multiplayer as a release blocker.

---

## Dependencies / Assumptions

- The project remains a Unity `6000.4.10f1` game with Netcode for GameObjects and Unity Transport.
- The existing one-scene architecture remains the runtime constraint for this first playable.
- The current `GameSession` best-result and three-game reveal semantics remain valid.
- Generated art can be produced and curated quickly enough to improve the first playable without becoming the schedule's main risk.
- Same-computer host/client proof is enough for the core multiplayer claim if QA evidence is clear.
- The current build on `main` stays launchable while the expanded prefab/entity structure is introduced.

---

## Sources / Research

- `README.md` for locked scope, privacy rules, game loop, reveal semantics, and verification targets.
- `DESIGN.md` for Future Workshop Diorama + Junior Quest UX, real character bar, fallback-art rules, and visual QA expectations.
- `docs/architecture.md` for persistent-scene model, host-authoritative multiplayer, mini-game result contract, and reveal language boundaries.
- `docs/art-direction.md` for current procedural art direction and next art upgrade goals.
- `docs/demo-checklist.md` for evaluator, Play, live multiplayer, solo fallback, and release proof expectations.
- `docs/qa/2026-06-09-showcase-smoke.md` for current QA gaps around visual/manual multiplayer proof and distribution.
- `docs/brainstorms/2026-06-09-demo-wow-showcase-requirements.md` as historical Showcase-specific context; this full-vision doc supersedes it for the next planning pass.
- `docs/plans/2026-06-09-demo-wow-showcase-plan.md` as historical completed implementation context.
- `docs/plans/2026-06-10-career-quest-full-vision-ceo-plan.md` — CEO_APPROVED SELECTIVE EXPANSION plan (Sections 1–11 complete).
- `docs/plans/2026-06-10-career-quest-ceo-review-handoff.md` — session handoff notes.
