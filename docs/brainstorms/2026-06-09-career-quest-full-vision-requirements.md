---
date: 2026-06-09
topic: career-quest-full-vision
---

# Career Quest Full Vision Requirements

## Summary

Career Quest Campus should become a credible first playable: a kid-friendly Unity career campus with generated sprite art, avatar selection, a playable hub, room-scale mini-games, all-three multiplayer support, a three-game Career Reveal gate, visual QA evidence, and a Windows-first release. `Play` is the honest game path; `Showcase` remains the guided evaluator path.

---

## Problem Frame

The current build has the right loop on paper: avatar choice, campus, mini-games, gallery, reveal, and multiplayer proof. The remaining gap is experiential. It still risks reading like a UI-driven prototype with procedural scenery rather than a video game where a player inhabits a world, moves a character, performs actions, and earns a reveal through play.

The full-vision pass exists to turn the existing loop into a real first playable without losing the privacy boundaries, reveal semantics, or Netcode proof already established. The goal is not to build the entire 12-month campus. It is to make the first campus slice feel real, coherent, testable, and releasable.

---

## Key Decisions

- **Real first playable over small polish.** The next pass should address visual identity, movement, interactivity, multiplayer, and proof together because the central gap is "does this feel like a game?"
- **Dual-path release.** `Play` is the normal first-run game path; `Showcase` remains available for guided evaluation and reliable presentation.
- **Three-game reveal gate.** Career Reveal unlocks only after three unique mini-games; one or two results build progress but do not reveal the final path.
- **Generated/imported sprite kit with fallbacks.** Generated art is acceptable for the first playable, but every major asset category needs fallback behavior so the game never shows invisible characters or blank rooms.
- **Playable hub over campus menu.** Campus navigation should happen through avatar movement, building entrances, camera framing, and a guide/NPC presence rather than only through screen buttons.
- **Room-scale mini-games.** Design Build, Health Hero, and Logic Court should feel like themed activity rooms with visible objects and player actions, not staged button checklists.
- **All-three multiplayer, sequenced carefully.** All mini-games should support multiplayer, but Design Build is the first protected multiplayer wow moment and stabilizes before Clinic/Court synchronization.
- **Prefab/entity rewrite inside one persistent scene.** The build should move toward Unity-native prefabs and entities while keeping one persistent gameplay scene to avoid Netcode scene-transition risk.
- **Host-authoritative activity state.** Shared mini-game state is validated by the host, syncs visible progress, emits one result, and lets `GameSession` remain the source for Career DNA and reveal readiness.
- **Windows-first release.** Windows is the authoritative proof artifact. WebGL remains optional and only ships if it behaves clearly.

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
  - **Steps:** The player selects an avatar, chooses a connection mode or solo fallback, enters the hub, moves through the campus, enters three activity buildings, completes mini-games, views Gallery progress, then unlocks Career Reveal after three unique games.
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

---

## Requirements

**Visual Identity And Art**

- R1. The first playable must use generated/imported 2D sprite art for avatars, guide/NPC presence, campus buildings, activity rooms, props, badges, and reveal ceremony pieces.
- R2. Every major screen must include character presence and environment art; no first-run screen should read as a blank panel or text-only UI.
- R3. Generated/imported art must be curated informally for safety, consistency, and obvious brand/IP issues before import.
- R4. Every major asset category must have a fallback sprite or fallback visual so missing art does not make the game blank or invisible.

**Avatar And Hub**

- R5. Avatar selection must appear before normal Play and Showcase entry into the campus.
- R6. The selected avatar must remain visually recognizable in the hub and activity contexts.
- R7. The campus hub must support avatar movement, visible buildings, building entrances, guide/NPC orientation, Gallery destination, Reveal destination, and exit access.
- R8. The hub camera should follow the active avatar within bounded campus framing while activity rooms use fixed room framing.
- R9. Campus buttons may remain as fallback or debug affordances, but the primary Play path should feel like walking to places.

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
- AE6. **Covers R29-R32.** Given a milestone is considered done, when QA evidence is reviewed, then automated proof, visual evidence, and manual host/client notes exist for that milestone's promised surface.
- AE7. **Covers R33-R39.** Given the release package is prepared, when an evaluator opens the Windows build or itch page materials, then controls, limitations, fallback notes, privacy boundaries, and optional WebGL status are clear.

---

## Success Criteria

- SC1. The first minute of Play proves avatar choice, character movement, campus world, and activity entry without relying on explanatory docs.
- SC2. A reasonable evaluator can describe the core loop as: choose avatar, explore campus, play three career challenges, earn badges, reveal career paths.
- SC3. The game looks like a kid-friendly 2D/2.5D game rather than a decorated UI prototype.
- SC4. Design Build provides a reliable shared multiplayer wow moment before Clinic/Court multiplayer expands.
- SC5. Career Reveal unlocks only after three unique games and remains strength-based.
- SC6. Every major screen has screenshot or clip evidence before release.
- SC7. Windows build is playable as the main proof artifact; WebGL is either absent or clearly labeled as optional/limited.

---

## Scope Boundaries

**Deferred for later**

- More career buildings, career clusters, and curriculum depth beyond the first playable campus set.
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
- `docs/architecture.md` for persistent-scene model, host-authoritative multiplayer, mini-game result contract, and reveal language boundaries.
- `docs/art-direction.md` for current procedural art direction and next art upgrade goals.
- `docs/demo-checklist.md` for evaluator, Play, live multiplayer, solo fallback, and release proof expectations.
- `docs/qa/2026-06-09-showcase-smoke.md` for current QA gaps around visual/manual multiplayer proof and distribution.
- `docs/brainstorms/2026-06-09-demo-wow-showcase-requirements.md` as historical Showcase-specific context; this full-vision doc supersedes it for the next planning pass.
- `docs/plans/2026-06-09-demo-wow-showcase-plan.md` as historical completed implementation context.
- Local CEO and engineering review notes for full-vision decisions; the relevant decisions are reflected here so downstream plans stay portable.
