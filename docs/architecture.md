# Architecture Snapshot

This document is an implementation-facing snapshot of the local gstack design, CEO review, engineering review, and grill-me decisions. The richer review history remains in `.gstack`; this file exists so builders and graders can see the current plan from the repo.

## Technical Defaults

- Unity version: `6000.4.10f1`.
- Networking: Unity Netcode for GameObjects.
- Transport: Unity Transport.
- Distribution target: itch.io with downloadable Windows build. WebGL is preview-only unless networking is already working.
- Visual style: Future Workshop Diorama + Junior Quest UX: bright 2.5D campus, polished 2D game characters, clean quest HUD, strong feedback moments.

## Privacy Boundaries

The Week7 build does not add accounts, saved profiles, persistent child data, analytics, telemetry, or chat. Optional display names are session-only. Showcase seeded/live source can appear in debug or QA evidence, but the child-facing Gallery and Reveal stay celebratory.

## Scene Model

P0 uses one persistent gameplay scene. Mini-games are full activities, but they run as states/panels/rooms inside the persistent scene instead of separate Unity scenes.

Separate Netcode-managed scenes are deferred until after the P0 loop ships.

```text
EntryScreen
   |
   +-- Play
   |     -> AvatarSelection
   |     -> Solo/free campus
   |
   +-- Multiplayer / Testing
   |     -> ConnectionScreen
   |     -> host/join/solo fallback campus
   |
   +-- Showcase
   |     -> friendly disclaimer
   |     -> PresenterController
   |     -> guided campus route
   |
   v
Gameplay Scene
   |
   +-- NetworkBootstrap
   +-- GameSession
   |     +-- app mode: Play or Showcase
   |     +-- player/session display state
   |     +-- connection mode
   |     +-- best MiniGameResult per activity
   |     +-- Career DNA totals
   |     +-- Achievement Gallery state
   |     +-- reveal readiness/confidence
   |     +-- seeded/live debug source metadata
   |     +-- recovery routing
   |
   +-- Campus
   |     +-- PlayerAvatar
   |     +-- CampusGuide
   |     +-- Building entrances
   |
   +-- Activity states
   |     +-- DesignBuildStudio
   |     +-- HealthHeroClinic
   |     +-- LogicCourt
   |
   +-- MiniGameResultHandler
   +-- AchievementGalleryUi
   +-- RevealCeremony
   +-- RecoveryUi
   +-- DemoDebugOverlay
```

## Connection Modes

Normal `Play` should bypass connection setup: `Play -> Avatar Selection -> Campus`. The connection screen is a secondary Multiplayer/Testing route for local proof and QA. It shows four clear options:

1. `Play Solo`
2. `Host Game`
3. `Join This PC`
4. `Join By IP`

LAN discovery/server browser is a stretch attempt. The required LAN path is manual IP join plus optional local IP display. If LAN is not tested, README and QA docs must mark it experimental.

## Showcase And Play Split

`Showcase` is a separate guided route for evaluators. It starts with a friendly disclaimer, may seed route/results/badges/camera beats, and uses PresenterController to reach the core proof path quickly. Seeded state must never leak into normal `Play` defaults.

`Play` remains the honest free-campus path. It does not auto-advance, does not seed Career DNA, and does not show Showcase-only pacing unless the player explicitly chooses Showcase.

## Input Modes

```text
Solo/Fallback
   -> normal keyboard + mouse mini-game controls

Same-computer multiplayer
   -> P1 split keyboard preset
   -> P2 split keyboard preset
   -> split controls apply to campus and Design Build Studio actions

LAN multiplayer
   -> each computer may use normal keyboard + mouse
   -> P1/P2 presets remain available when sharing hardware
```

Preset controls are enough for Week7. Full remapping is out of scope.

## Multiplayer Authority

The host/server is authoritative for the facts that matter:

- Activity timer
- Blueprint slots and accepted placements
- Duplicate placement rejection
- Mini-game result emission
- Best-result replacement
- Career DNA recomputation
- Achievement Gallery badge source/tier

Clients may show local previews, but they submit discrete action requests to the host.

```text
Client input
   -> local preview
   -> ServerRpc action request
   -> host validates action
   -> host updates authoritative state
   -> host broadcasts low-frequency state/result
   -> clients render accepted result
```

If two players try to place the same Design Build Studio piece at once, the host grants the first valid placement and rejects the second with gentle UI feedback.

## Protected Wow Moment

The protected visual moment is the first polished playable room: a selected avatar, helper NPC, recognizable room/workbench, direct-manipulation props, immediate feedback, and badge ceremony. The protected multiplayer wow moment remains shared placement in Design Build Studio. Both clients should clearly see:

- Snap animation
- Sound or UI pulse
- Highlighted accepted placement
- Player badge/name feedback
- Shared result applied once

The Career Reveal should still feel celebratory, but if time gets tight, polish the shared placement first.

## Mini-Game Contracts

All mini-games return the same `MiniGameResult` shape:

- activity id
- completion tier: `Degree` or `Practice`
- source: `Multiplayer`, `Solo`, or `Solo Fallback`
- trait deltas
- time remaining
- accuracy
- summary text

Mini-games do not emit career weights. `CareerConfig` computes career ranking from traits.

## Mini-Games

### Design Build Studio

Mechanic: collaborative spatial placement.

Win condition: place the right shapes/colors into blueprint slots before timer ends.

AI Engineer support: includes a scripted Pattern Helper clue. It is not live AI.

### Health Hero Clinic

Mechanic: timed diagnosis and tool/treatment matching.

Win condition: match symptoms to the right tool/treatment sequence before timer ends.

### Logic Court

Mechanic: evidence sorting and argument ranking.

Win condition: sort evidence into helpful/not helpful and choose the strongest closing argument.

## Career DNA

Traits:

- Helping
- Science
- Focus
- Reasoning
- Communication
- Leadership
- Creativity
- Building
- Spatial Thinking
- Collaboration

Careers:

- Doctor
- Lawyer
- AI Engineer
- Artist
- Architect

AI Engineer remains in the first career set and is supported through Design Build Studio, Logic Court, Pattern Helper, Reasoning, Building, Spatial Thinking, Creativity, and Collaboration.

## Replay And Scoring

Replay is allowed before and after reveal. Only the best result per mini-game counts.

Best result ordering:

1. Completion tier
2. Time remaining
3. Accuracy

`GameSession` stores each activity's best result and recomputes Career DNA totals from best results. This avoids trait inflation from replay farming.

## Achievement Gallery And Reveal

The Achievement Gallery shows best results only. Attempts are debug-only.

Stamp tiers:

- `Degree`: success
- `Practice`: partial/soft failure

Source badge:

- `Multiplayer`
- `Solo`
- `Solo Fallback`

Reveal unlocks after three unique completed mini-games or Showcase-equivalent results. One or two results build Career DNA and Gallery progress, but the final recommendation stays locked until the player has tried the full themed set.

Confidence phrases:

- `Good match`
- `Strong match`
- `Very strong match`

Confidence is based on number and quality of unique best results, not raw percentages. Close career ties are shown as co-leads.

Reveal copy must be strength-based. It should say a career is one path worth exploring, not a life assignment.

## Debug Overlay

Hidden by default. Toggle through a small debug button and a keyboard shortcut.

Show compact demo facts:

- Host, Client, or Solo Fallback
- Connection status
- Player count
- Current activity
- Timer
- Last accepted action
- Last result id
- Top three Career DNA traits
- Attempt/debug source metadata

## Repo Layout

Unity project lives at repo root.

Track:

- `Assets/`
- `Packages/`
- `ProjectSettings/`
- `docs/`
- `README.md`

Ignore:

- `Library/`
- `Temp/`
- `Obj/`
- `Build/`
- `Builds/`
- logs
- user-local Unity files

## First Implementation Checkpoints

1. Unity opens cleanly with Netcode/Transport installed and docs updated.
2. One computer can run two local clients with split controls.
3. Two clients can see each other move in campus.
4. Design Build Studio shared placement is visible on both clients.
5. Career DNA, Achievement Gallery, and Reveal work from mini-game results.
6. Windows build and QA evidence are recorded.
