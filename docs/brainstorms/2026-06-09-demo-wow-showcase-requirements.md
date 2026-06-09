---
date: 2026-06-09
topic: demo-wow-showcase
---

# Demo Wow Showcase Requirements

## Summary

Career Quest Campus should add a polished `Showcase` path that reliably communicates the game's multiplayer promise, career-discovery loop, and larger campus vision in a short evaluator-friendly sequence. `Play` remains the normal free-campus path, while Showcase behaves like a guided product tour with a transparent upfront disclaimer and a magical Achievement Gallery / Career Reveal payoff.

---

## Problem Frame

The current Week7 scope already defines a strong Unity campus game, but the demo path risks depending on too much late-scope live gameplay. The reveal is one of the strongest payoffs, yet the older two-game reveal gate could block the safest P0 path if only Design Build Studio is fully stable early.

The Demo Wow pass exists to make the project feel creative, well-implemented, and well-thought-out even under evaluator time pressure. The game should not merely expose features; it should guide the evaluator through a curated sequence that makes the whole product thesis obvious within minutes.

---

## Key Decisions

- **Showcase as a separate entry path.** The connection screen should expose both `Play` and `Showcase`. `Play` leads to the free campus. `Showcase` launches the curated demo path.
- **Presenter-style Showcase.** Showcase may seed route, results, Passport/Gallery state, camera beats, and reveal timing to guarantee the strongest path lands quickly.
- **Transparent start, immersive middle.** Pressing `Showcase` shows a short friendly disclaimer before launch. Once accepted, the Gallery and Reveal should stay polished rather than cluttered with seeded-state labels.
- **One-game reveal unlock.** Career Reveal can unlock after one mini-game so the payoff is never blocked. Additional unique games improve confidence.
- **Hybrid multiplayer proof.** Showcase may use a reliable split-screen/shared-input simulation for the two-player proof beat. Actual Netcode two-client testing remains required in QA evidence.
- **Achievement Gallery metaphor.** Stamps and badges should read like an earned gallery moment that flows into the reveal.
- **Showcase profile.** The seeded showcase result should present Architect + AI Engineer co-leads through a Creative Technical Builder profile.
- **World promise through labels.** Future buildings may use extra non-playable career/building labels as long as they do not delay core work or create new gameplay obligations.

---

## Actors

- A1. Evaluator
  - Wants to understand quickly that the project has multiplayer, progression, creativity, technical execution, and a complete loop.
- A2. Child player
  - Wants the experience to feel playful, encouraging, and not like a deterministic quiz.
- A3. Presenter
  - Needs a reliable path that can be shown under time pressure without depending on perfect live input.
- A4. Normal player
  - Chooses `Play` and explores the campus manually without seeded Showcase pacing.

---

## Key Flows

- F1. Showcase launch
  - **Trigger:** The evaluator or presenter presses `Showcase`.
  - **Actors:** A1, A3
  - **Steps:** A friendly tour disclaimer appears, explains that Showcase is guided, and says `Play` is available for free exploration. Accepting starts the curated sequence.
  - **Outcome:** The evaluator understands this is a guided tour before any seeded moments appear.

- F2. First-minute clarity
  - **Trigger:** Showcase begins.
  - **Actors:** A1, A3
  - **Steps:** The sequence shows the connection/two-player proof beat, the campus/future-building promise, and the Career DNA / badge concept in quick balanced beats.
  - **Outcome:** Within 60 seconds, the evaluator understands multiplayer, career discovery, and a larger world promise.

- F3. Future City Design Build
  - **Trigger:** Showcase moves from campus into Design Build Studio.
  - **Actors:** A1, A2, A3
  - **Steps:** Two demo players assemble a Future City Model with clinic, court, studio, lab, and art-tower pieces. The experience can use shared-input simulation in Showcase while preserving the same action language as normal multiplayer where practical.
  - **Outcome:** The game communicates teamwork, building, creativity, and the Living Campus theme.

- F4. Achievement Gallery into Reveal
  - **Trigger:** The Future City Model beat completes or Showcase advances.
  - **Actors:** A1, A2, A3
  - **Steps:** Badges and traits appear in an Achievement Gallery. The gallery transitions into a Career Reveal that celebrates Architect + AI Engineer co-leads.
  - **Outcome:** The reveal feels earned, clear, and celebratory even if Showcase used seeded state.

- F5. Normal play remains separate
  - **Trigger:** A player chooses `Play` instead of `Showcase`.
  - **Actors:** A2, A4
  - **Steps:** The player enters the free campus path without Showcase seeding, cinematic auto-advance, or forced route.
  - **Outcome:** Normal play stays honest and exploratory.

---

## Requirements

**Entry And Mode Clarity**

- R1. The opening/connection screen must offer separate `Play` and `Showcase` choices.
- R2. `Play` must lead to the free campus path rather than the curated Showcase route.
- R3. Pressing `Showcase` must show a short friendly disclaimer before the guided tour begins.
- R4. The disclaimer must communicate the spirit: "We'll give you a quick tour of the campus, badges, and reveal. Choose Play to explore on your own."
- R5. Showcase must avoid persistent labels that interrupt the Gallery or Reveal; the upfront disclaimer carries normal-user transparency.

**Showcase Experience**

- R6. Showcase must aim to complete the core demo path in under three minutes.
- R7. Showcase must balance multiplayer proof, career discovery, and larger-world promise in the first minute.
- R8. Showcase may seed route, results, badge/Gallery state, camera beats, and reveal timing to guarantee the best presentation.
- R9. Showcase must be able to present a split-screen or equivalent local two-player proof beat without requiring a second Unity process.
- R10. The Showcase two-player proof should reuse the same control/action language as normal play where practical, even when not running through the network layer.
- R11. The QA/demo checklist must separately prove actual two-client networking; Showcase simulation cannot replace that evidence.

**Design Build Beat**

- R12. The Showcase Design Build challenge should be the Future City Model: a small skyline with clinic, court, studio, lab, and art-tower pieces.
- R13. The Design Build beat must communicate teamwork and accepted contribution through visible placement feedback.
- R14. If live Design Build implementation is incomplete near ship, Showcase may use scripted or seeded pacing as long as `Play` and QA evidence remain clear about live capabilities.

**Achievement Gallery And Reveal**

- R15. The Passport payoff should use an Achievement Gallery metaphor rather than a plain report screen.
- R16. Showcase should plan for badges from Design Build Studio, Health Hero Clinic, and Logic Court, but may fall back to implemented-only badges near ship if the fuller version becomes misleading or brittle.
- R17. Showcase badges do not need visible "seeded" or "tour" labels inside the normal Gallery UI.
- R18. The reveal must be reachable after one completed mini-game or Showcase-equivalent result.
- R19. Additional unique mini-games should improve reveal confidence rather than gate reveal access.
- R20. The seeded Showcase reveal must spotlight Architect + AI Engineer as co-leads.
- R21. The seeded Showcase profile should emphasize Building, Spatial Thinking, Creativity, Reasoning, and Collaboration.
- R22. AI Engineer reveal copy should frame the path as a Future Problem Solver: using logic and creativity to solve problems people care about.
- R23. Reveal copy must remain strength-based and exploratory, not deterministic or life-assigning.

**Campus Promise**

- R24. Showcase should include visible non-playable future building labels to make the campus feel larger.
- R25. Future labels may include additional career/building themes beyond the first five reveal careers, but they must not imply playable scope for this build.
- R26. The preferred future-label mix is balanced across STEM, creative, helping, and sustainability themes, such as Space Lab, Music Studio, Green Energy Center, Robotics Garage, and Community Kitchen.
- R27. The number of future labels is flexible, capped by what fits cleanly without delaying core work.

**Preserved Live Play Decisions**

- R28. The experience remains fully inside Unity for this build.
- R29. Every mini-game must be single-player optional, with normal keyboard + mouse controls available for solo and fallback play.
- R30. Same-computer two-client testing must support distinct split controls for the local players.
- R31. LAN manual-IP join should be implemented alongside local testing when practical, but it is not a blocker if it remains untested and clearly documented.

**Privacy And Trust**

- R32. The next build must not add accounts, saved profiles, persistent child data, analytics, telemetry, or chat.
- R33. Display names and any child-facing identity remain session-only if present.
- R34. Showcase seeded/live source may appear in debug metadata or QA evidence, but should not clutter the child-facing Gallery experience.

---

## Acceptance Examples

- AE1. **Covers R1-R5.** Given the evaluator presses `Showcase`, when the disclaimer appears, then it clearly says this is a guided tour and that `Play` is available for free exploration.
- AE2. **Covers R6-R11.** Given Showcase starts, when 60 seconds have passed, then the evaluator has seen a two-player proof beat, a Career DNA/badge hook, and at least one larger-campus signal.
- AE3. **Covers R12-R14.** Given the Future City Model beat is shown, when pieces are placed, then accepted placement feedback makes the collaboration/building moment legible.
- AE4. **Covers R15-R23.** Given Showcase reaches the Gallery, when reveal begins, then badges/traits transition into an Architect + AI Engineer co-lead reveal with kid-friendly Future Problem Solver language.
- AE5. **Covers R18-R19.** Given a player has only one mini-game result, when they open reveal, then reveal is available with lower confidence; additional unique results improve confidence.
- AE6. **Covers R24-R27.** Given Showcase tours the campus, when future buildings appear, then they sell the larger world without acting as playable entrances.
- AE7. **Covers R28-R34.** Given Showcase or Play is used, when the session ends, then solo controls, local/LAN testing boundaries, and privacy rules remain intact with no account, chat, analytics, telemetry, or child profile state created or persisted.

---

## Success Criteria

- SC1. An evaluator can complete Showcase in under three minutes.
- SC2. Within the first 60 seconds of Showcase, multiplayer, career discovery, and larger-world promise are all obvious.
- SC3. The reveal makes Architect + AI Engineer co-leads feel earned and explainable.
- SC4. A reasonable evaluator reaction is: "Wow, this is a creative, well-implemented, well-thought-out project."
- SC5. Normal `Play` remains available as the non-seeded free-campus path.
- SC6. QA evidence separately verifies actual two-client networking so Showcase simulation is not mistaken for the only multiplayer proof.

---

## Scope Boundaries

- No accounts, saved child profiles, analytics, telemetry, chat, or persistent child data.
- No requirement to make future building labels playable in this build.
- No requirement to add new reveal careers beyond Doctor, Lawyer, AI Engineer, Artist, and Architect.
- No requirement for Showcase's split-screen proof to run through Netcode, as long as real two-client Netcode proof is separately tested and documented.
- No requirement to visibly label seeded badges inside the child-facing Gallery UI.

---

## Dependencies / Assumptions

- The Unity project remains the whole experience.
- The first implementation should still protect a reliable build over breadth.
- Design Build Studio remains the first deep multiplayer mini-game.
- Health Hero Clinic and Logic Court remain planned mini-games, but Showcase may not depend on their full live implementation if time is tight.
- The plan should preserve the existing privacy stance from `README.md` and `docs/architecture.md`.

---

## Sources / Research

- `README.md` for locked scope, privacy rules, game loop, and verification targets.
- `docs/architecture.md` for persistent-scene architecture, input modes, multiplayer authority, mini-game contracts, Passport, Reveal, and debug overlay decisions.
- `docs/demo-checklist.md` for evaluator/demo path expectations.
- `docs/qa/README.md` for QA proof expectations.
- `TODOS.md` for deferred Living Career Campus expansion boundaries.
