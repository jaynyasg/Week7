---
date: 2026-06-11
topic: wow-quality-pass
---

# Wow Quality Pass Requirements

## Summary

Take Career Quest Campus to a committed, checkable reference bar — professional imported art (Kenney/Quaternius) in an editor-authored world, drag-and-drop mini-game play, real fonts, full badge coverage, an in-world cinematic reveal, and sound. One flagship path (campus walk → Design Build → reveal) reaches the full bar first as proof and template; then layer-completion sweeps bring every remaining room, screen, and missed deliverable from the last implementation up to it. No deadline pressure: the end state is the whole game at the bar.

---

## Problem Frame

Three implementation passes (U1–U8 plus a design-review fix loop) shipped the right systems — catalog, netcode, ceremony stack, ShipLadder — but the playable experience never reached the owner's quality bar. The campus does not read as an environment, mini-games are click-a-button rather than playing with objects, the reveal is a UI overlay on a static backdrop, and the game has no real fonts, no sound, no animation, and placeholder badges for optional rooms.

Two root causes kept recurring. First, every visual surface is built from code (`Assets/_CareerQuest/Scripts/UI/UiBuilder.cs`, `Assets/_CareerQuest/Scripts/World/CampusWorldBuilder.cs`, `Assets/_CareerQuest/Scripts/World/CampusRoomScenes.cs`), so the pipeline could only emit programmer art no matter how many polish passes ran. Second, the quality bar was never committed as a checkable artifact, so each pass could "complete" while missing the owner's expectations.

A full audit of the CEO-approved plan (`docs/plans/2026-06-10-career-quest-full-vision-ceo-plan.md` locks and cherry-picks) against the repo confirms these promised items never shipped:

| Promised (locked) | Verified state |
|---|---|
| R12 direct manipulation in mini-games | Zero drag/pointer code in `Assets/_CareerQuest/Scripts/Activities/`; 45 Button/onClick wirings |
| Q7 real walk/idle sprite sheets | No animation, frame, or flip code in `PlayerAvatarController` |
| Step 0E rich parallax (4+ layers + ambient motion) | No parallax code anywhere |
| exp1_living_campus ambient motion | Not present |
| exp2_ambient_hub_sfx + R27 audio fanfare | Zero audio files in `Assets/`; `AudioCueCatalog` cues silently no-op |
| exp5_guide_dialogue speech bubbles | Zero speech/bubble/dialogue code |
| Fredoka/Lexend typography (DESIGN.md) | Zero font files; `UiBuilder.cs` hard-wires `LegacyRuntime.ttf` |
| Optional-room badge art | Only 4 badge PNGs in `Assets/Resources/CareerQuest/Badge/`; optional rooms use procedural fallbacks |
| Wave 4 `SubmissionBundle/` | Directory does not exist |
| exp6_campus_evolution (city pieces) | Partial — wired in `CampusWorldBuilder`, needs verification during the world pass |

---

## Actors

- A1. Child player: walks the campus, plays the rooms with direct manipulation, earns badges, experiences the reveal.
- A2. Evaluator/audience: judges in minutes whether the game looks professional and plays like a real game — the "wow" target.
- A3. Owner: reviews each checkpoint against the committed references and decides whether the bar is met before replication multiplies the pattern.

---

## Key Flows

- F1. Flagship slice proof
  - **Trigger:** First implementation checkpoint of this pass.
  - **Actors:** A3
  - **Steps:** Campus walk, Design Build room, and the reveal are rebuilt to the full bar (authored Kenney world, fonts, animation, drag-and-drop, cinematic ceremony, audio). Owner compares screenshots/clips side-by-side against `docs/references/` and approves or redirects.
  - **Outcome:** The quality template is proven before it is replicated; redirects cost one slice, not the whole game.
  - **Covered by:** R5–R13, R26–R28

- F2. Mini-game direct manipulation
  - **Trigger:** Player enters a core activity room.
  - **Actors:** A1
  - **Steps:** Player drags building pieces to slots (Design Build), drags care tools to the patient (Health Hero), drags evidence cards to sorting zones (Logic Court); each drop gives immediate visual+audio feedback; invalid drops bounce back gently.
  - **Outcome:** Rooms are played with objects, not clicked through as button lists.
  - **Covered by:** R9–R11, R20

- F3. Cinematic reveal
  - **Trigger:** Player triggers Career Reveal with three unique badges earned.
  - **Actors:** A1, A2
  - **Steps:** Camera moves to the in-world reveal stage; badge tokens travel into slots; stage light sweep and unlock burst play in the world; career-path result presents with strength-based copy; UI supports but does not replace the world moment.
  - **Outcome:** The payoff is a world event, not a panel.
  - **Covered by:** R12–R13, R30–R31

---

## Requirements

**Reference bar and evidence**

- R1. Commit the visual bar into the repo under `docs/references/`: captured screenshots of Toca Boca World, Skillsville, and Khan Academy Kids, plus the chosen Kenney/Quaternius pack previews.
- R2. Every visual milestone in this pass produces 1280x720 screenshots/clips reviewed side-by-side against `docs/references/`; "done" means the owner affirms the comparison, not that tasks completed.
- R3. The zero-fallback gate extends to the full catalog: no player-facing surface (core or optional rooms, badges, NPCs, props, UI icons) may show procedural fallback art at completion of this pass.

**Art pipeline shift**

- R4. Imported professional CC0 assets (Kenney/Quaternius) become the primary art source; the in-repo generator (`CareerQuestSpriteKitGenerator`) is demoted to QA fallback only. Record this as a DESIGN.md decision-log amendment.
- R5. The campus and activity rooms become editor-authored content (prefabs/composed scenes instantiated into the persistent scene) rather than code-built geometry; code retains logic, interaction, and state.
- R6. Avatars and NPCs are replaced with reference-bar characters consistent with the imported world style; the existing generated character art is retired from the player-facing path.
- R7. All art continues to route through `AssetCatalog` IDs so gameplay code is unaffected by future art swaps.

**Flagship slice (campus walk → Design Build → reveal)**

- R8. The campus hub reads as a toy-diorama environment at the reference bar: authored ground/buildings/props, 4+ parallax layers, ambient motion, door signs, and entrance highlighting per DESIGN.md.
- R9. The player avatar has real walk/idle animation (sprite-sheet or frame-based), directional facing, and the DESIGN.md idle bob.
- R10. Design Build plays through drag-and-drop: pieces are picked up, dragged, and dropped onto slots with accept/reject feedback; clicking buttons is no longer the interaction model.
- R11. Drag interactions remain compatible with host-authoritative multiplayer: dragging is local, the drop submits the action for host validation, conflicts give gentle visible feedback.
- R12. The Career Reveal becomes an in-world cinematic: camera movement to the reveal stage, badge tokens traveling to slots, light sweep, and unlock burst, synced with the existing ceremony beats.
- R13. The cinematic respects the locked ceremony pacing rules: ~12s cap, Skip available after 3s, host-synced in two-player sessions.

**Game-wide completion layers (after flagship approval)**

- R14. Fredoka (display) and Lexend (body) are imported and wired through `UiBuilder` so every screen uses the DESIGN.md type roles and scale; no UI surface remains on `LegacyRuntime.ttf`.
- R15. All optional-room badges (ai_lab, music_studio, robotics_garage, and every ID `CareerQuestCatalog` requests) exist as real art at the same bar as the core four.
- R16. Health Hero and Logic Court are converted to the flagship interaction pattern: drag care tools to the patient, drag evidence cards to sorting zones, with the same feedback and ceremony quality as Design Build.
- R17. All remaining rooms — Health Hero, Logic Court, and every optional room — receive the authored-content art pass to the reference bar. Optional rooms keep their simpler (non-drag) interactions but must look at-bar.
- R18. Campus evolution (city pieces appearing as badges are earned) is verified working and visually at-bar during the world pass.
- R19. Guide speech bubbles ship per the accepted expansion: the campus guide gives short contextual lines in DESIGN.md speech-bubble styling.
- R20. Audio ships: real audio files for the existing `AudioCueCatalog` cue IDs (button, pickup, accept, reject, badge stamp, ceremony fanfare, reveal) plus ambient hub sound. The game must remain fully understandable with audio unavailable.

**Preserved constraints**

- R21. The one-persistent-scene Netcode architecture, host-authoritative activity state, and single-result-per-attempt contract are preserved unchanged.
- R22. `GameSession` reveal semantics stay locked: three unique completed activities unlock the reveal; copy stays strength-based and exploratory.
- R23. Privacy boundaries are untouched: no accounts, chat, analytics, telemetry, or persistent child data.
- R24. The build stays launchable at every checkpoint; existing tests are updated alongside the world-layer restructure, not deleted.

**Release evidence backfill**

- R25. `SubmissionBundle/` (the undelivered Wave 4 artifact) is produced at the end of this pass: screenshots/clips of every major surface, controls, limitations, and privacy notes, with zero fallback art visible.

---

## Acceptance Examples

- AE1. **Covers R2, R8.** Given the flagship campus is complete, when its screenshot is placed beside the committed Toca Boca/Skillsville references, then an evaluator describes both as professional kid-game environments rather than identifying one as programmer art.
- AE2. **Covers R10, R11.** Given two players in Design Build, when one drags a piece onto a valid slot, then the piece follows the cursor during the drag, the host validates on drop, both clients see the placement, and an invalid drop bounces back with gentle feedback.
- AE3. **Covers R12, R13, R22.** Given a player earns their third unique badge and triggers the reveal, then the camera moves to the stage, tokens travel to slots, and the unlock plays in-world within ~12s with Skip available after 3s; with only two badges, the stage shows locked slots and a 2/3 state.
- AE4. **Covers R3, R15.** Given any optional room is entered, when its badge and room art render, then no procedural fallback or checker sprite is visible anywhere player-facing.
- AE5. **Covers R14.** Given any screen in the game, when text renders, then display text uses Fredoka and body/instruction text uses Lexend at DESIGN.md scale — no surface falls back to LegacyRuntime/Arial.
- AE6. **Covers R20.** Given audio is enabled, when a player drops a piece, earns a badge, or unlocks the reveal, then a distinct sound plays; given audio is unavailable, all the same outcomes remain visually understandable.
- AE7. **Covers R24.** Given any checkpoint commit during the pass, when the project is opened and Play is pressed, then the full loop (avatar → campus → room → badge → reveal) is playable without errors.

---

## Success Criteria

- The owner looks at the flagship slice and affirms "this is the game I described" against the committed references before replication begins.
- An evaluator watching one minute of play sees an animated character walking a real environment, dragging objects in a themed room, and (if shown) a staged in-world reveal — and would not guess the art was procedural.
- All ten missed deliverables from the last implementation audit (table in Problem Frame) are closed or explicitly re-scoped in the requirements of a successor doc.
- A downstream planner can build the wave plan directly from R1–R25 without inventing product behavior; the only open items are the planning-tagged questions below.

---

## Scope Boundaries

- New careers, new mini-game types, or curriculum depth beyond the existing room set.
- Persistence, accounts, profiles, WebGL, LAN discovery, or internet matchmaking (parent vision doc boundaries unchanged).
- Full drag-and-drop gameplay for optional rooms — they get at-bar art and badges but keep simpler interactions.
- Music composition / licensed soundtrack — audio scope is SFX and ambient loops from CC0 sources.
- Four-player support — the 2P ship/test ceiling stays.

---

## Key Decisions

- Flagship vertical slice before breadth: previous passes went wide and missed the bar everywhere; the slice proves the bar is reachable and becomes the template, with an owner checkpoint before the pattern multiplies.
- Imported assets over the owned generator (reverses DESIGN.md 2026-06-10 decision): the generator's ceiling is the reason three passes produced programmer art; Kenney/Quaternius CC0 packs carry no cost or redistribution risk.
- Editor-authored world over code-built world: composition quality at the reference bar is not reachable through runtime geometry builders; prefab content also makes future art passes "edit a prefab" instead of "rewrite a builder."
- The reference bar is a committed artifact (`docs/references/`), and milestone review is a side-by-side comparison: this converts "wow" from an opinion into a checkable gate, addressing why prior passes completed while missing expectations.
- Generated character art is retired from the player-facing path: a reference-bar world with generator-art characters would read as mismatched; style consistency wins over preserving existing PNGs.
- Audio is in the wow bar: a silent game cannot feel professional regardless of visuals; scope is CC0 SFX/ambient, not composed music.

---

## Dependencies / Assumptions

- Unity `6000.4.10f1`, Netcode for GameObjects, and the one-persistent-scene constraint remain fixed.
- Kenney/Quaternius packs contain (or can be lightly adapted into) a coherent kid-friendly 2D campus style covering environments, characters, props, and UI; the specific packs are selected during planning with owner sign-off on the look.
- Drag-and-drop drop-submission fits the existing host-authoritative action contract without netcode redesign (verified pattern: local drag, validated drop).
- The entrance/veil/camera logic can operate on authored prefab content; coupling to builder-generated objects is assumed shallow (unverified — planner should confirm early).
- CC0 audio libraries (e.g., Kenney audio packs) cover the needed cue set.

---

## Outstanding Questions

### Deferred to Planning

- [Affects R14][Technical] Legacy uGUI `Font` wiring vs TextMeshPro migration for Fredoka/Lexend — TMP is the professional route but touches every text element; planner decides based on effort/payoff.
- [Affects R12][Technical] Cinemachine/Timeline vs scripted camera tweens for the reveal cinematic, given the one-scene constraint and 2P sync requirement.
- [Affects R4, R6][Needs research] Which specific Kenney/Quaternius packs form the kit, and whether characters come from a pack or a styled regeneration; owner approves the selected look against `docs/references/` before integration.
- [Affects R5][Needs research] Depth of coupling between `BuildingEntranceController`/`RoomVeilController`/`HubCameraRig` and builder-generated objects; determines whether the world restructure is swap-in or refactor.
- [Affects R24][Technical] Which existing world/UI tests assert on code-built geometry and need rewriting against authored content.
