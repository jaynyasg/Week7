# Career Quest Campus

Career Quest Campus is a Week7 Unity multiplayer game about kid-friendly career discovery. Players choose avatars, explore a bright campus, play career-themed mini-games, earn degree/practice stamps, build a Career DNA profile, and unlock a celebratory Career Reveal.

This repo is now bootstrapped as a Unity project at the repo root.

## Locked Scope

- Engine: Unity `6000.4.10f1`.
- Networking: Unity Netcode for GameObjects with Unity Transport.
- Project shape: Unity project at repo root with `Assets/`, `Packages/`, and `ProjectSettings/` tracked.
- Core architecture: one persistent gameplay scene for P0. Mini-games are implemented as activity states inside that scene, not separate Netcode-loaded scenes.
- Multiplayer proof: same-computer host/client testing is required. LAN support should be implemented if practical, but LAN is not a blocker unless tested and documented.
- Distribution: itch.io page with a downloadable Windows build, screenshots, fallback notes, and optional WebGL preview.
- Privacy: no accounts, chat, saved child profiles, child-identifying analytics, or persisted display names.

## Game Loop

1. Open the entry screen.
2. Choose `Play` for free campus or `Showcase` for the guided evaluator route.
3. Choose an avatar.
4. In `Play`, choose `Host P1`, `Join Localhost as P2`, `Join LAN by IP`, or `Solo Fallback`.
5. Enter Free Campus and choose rooms in any order.
6. Complete any three unique rooms to unlock Career Reveal.
7. View Achievement Gallery badges, top Career DNA traits, top career match, co-leads if tied, and runner-up paths.

## Rooms (13 total)

The campus has **13 playable rooms**: three original bespoke mini-games plus ten
definition-driven Party Pack stations. Every room is playable solo and is
host-authoritative in two-player; each emits the same `MiniGameResult` and feeds
Career DNA. (Full inventory and the framework in [docs/architecture.md](docs/architecture.md).)

**Three bespoke mini-games:**

- Future City Design Build: place clinic, court, studio, lab, and art-tower pieces into a shared future skyline (the protected deep-multiplayer moment).
- Health Hero Clinic: timed diagnosis and tool/treatment matching.
- Logic Court: evidence sorting and strongest closing-argument selection.

**Ten Party Pack stations**, each with a distinct interaction verb: Robotics
Rescue (launch), AI Lab Sort (deduce), Community Kitchen Match (match), Music
Remix (compose), Vet Clinic Diagnose (care), Game Studio Compose (compose),
Weather Lab Rescue (trace), Spaceport Pilot (trace), Newsroom Story Sprint
(deduce), Green City Builder (balance). All ten run on one shared framework
(`PartyStationController` + `ToyPatternRules`) driven from station data.

Only Design Build Studio must be deep multiplayer. All rooms are playable solo;
solo players can use mouse controls.

## Multiplayer And Controls

Same-computer testing must be possible from one machine:

- `Host P1`: WASD-style control preset.
- `Join Localhost as P2`: arrow/IJKL-style split control preset.
- Split controls apply to campus movement and multiplayer mini-game actions.
- Solo/Fallback mode uses normal keyboard plus mouse.
- LAN multiplayer may use normal controls per machine, with manual IP join as the required UI path.

Solo Fallback is available from the start and clearly labeled. It may show a local demo buddy where multiplayer matters, but debug/passport metadata must label the source as `Solo Fallback`.

## Career DNA

The first build uses these traits:

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

Careers: **30 reveal career paths** across six families (Care & Community,
Future Tech, Design & Build, Story & Stage, Nature & Space, Justice &
Leadership) — the original five (Doctor, Lawyer, AI Engineer, Artist, Architect)
plus the Party Pack expansion. Each career carries trait weights and a
path-support tier; see [docs/architecture.md](docs/architecture.md).

Rooms emit degree/practice stamps and trait deltas only. `CareerConfig` computes career rankings from configured trait weights.

Replay is allowed. Only the best result per mini-game counts, ranked by completion tier, then time remaining, then accuracy. Career DNA totals are recomputed from each mini-game's best result.

## Stamps And Reveal

- Stamp tiers: `Degree` for success, `Practice` for partial/soft failure.
- Source badges: `Multiplayer`, `Solo`, or `Solo Fallback`.
- Practice counts toward reveal unlock but lowers confidence.
- Reveal unlocks after three unique mini-games or Showcase-equivalent results.
- Reveal confidence is phrase-only: `Good match`, `Strong match`, or `Very strong match`.
- Close career ties are shown as co-leads.
- Reveal language is strength-based, not deterministic.

## Project Docs

- [Architecture snapshot](docs/architecture.md)
- [Key technical decisions & rationale](docs/technical-decisions.md)
- [Deploy guide (build + itch.io)](DEPLOY.md)
- [Rubric status & evidence](docs/rubric-status.md)
- [Art direction](docs/art-direction.md)
- [Demo checklist](docs/demo-checklist.md)
- [QA evidence template](docs/qa/README.md)
- [Backlog](TODOS.md)

## Local Setup

1. Open Unity Hub.
2. Add this repo root as the Unity project.
3. Use Unity `6000.4.10f1`.
4. Let Unity restore packages from `Packages/manifest.json`.
5. Open `Assets/_CareerQuest/Scenes/CareerQuestCampus.unity`.

Bootstrap verification completed on 2026-06-09: Unity opened this project in batchmode with Netcode for GameObjects `2.11.2` and Unity Transport `2.7.2`, then exited with code `0`.

## Verification Targets

- Unity project opens cleanly in `6000.4.10f1`.
- `Play` enters unseeded free campus and `Showcase` starts the guided route after a disclaimer.
- Same-computer host/client can run from one machine.
- Two clients can see each other move in the campus.
- Design Build Studio accepted placement produces clear feedback.
- Career DNA, Achievement Gallery, and Reveal update from mini-game results.
- Forced failures show retry/return paths.
- Windows build path and QA evidence are recorded under `docs/qa/`.
