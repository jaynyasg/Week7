# Game Week Rubric — Status & Evidence

Status of the project against the Game Week gate checklist (`week7 schedule.docx`).
Legend: **DONE** (met, with evidence) · **PARTIAL** (substantially met, caveat noted)
· **HUMAN** (requires a person — recording, live playtest, or external sign-off; not
something the codebase can self-satisfy).

Last test gate: **EditMode 241/241 + PlayMode 233/233 green** (re-run 2026-06-16 on `main`).

## Gate 1 — Foundation

| Rubric line | Status | Evidence |
|---|---|---|
| Tech stack chosen (Unity) + documented rationale | **DONE** | `README.md` Locked Scope; `docs/architecture.md` Technical Defaults; `docs/technical-decisions.md` |
| Repo at root + setup instructions + architecture/docs overview | **DONE** | Unity project at repo root; `README.md` Local Setup; `docs/architecture.md`; `DEPLOY.md` |
| Single-player core gameplay loop working | **DONE** | Solo play through campus → stations → badges → reveal; PlayMode station-pack suites |
| Proof-of-concept demos validating the learning path | **DONE** | `docs/qa/` proof docs; per-station PlayMode proofs (e.g. `RoboticsProvesShootTargetLaunchToGoal`) |
| Short video: docs/architecture overview + single-player loop | **HUMAN** | Script-ready (this doc + `docs/demo-checklist.md`); recording is a person task |
| MVP gate maintained | **DONE** | Green test gate; MVP loop intact end-to-end |

## Gate 2 — Multiplayer

| Rubric line | Status | Evidence |
|---|---|---|
| Real-time multiplayer (networking, state sync, connect/disconnect) | **DONE (code)** | Netcode for GameObjects host/client; host-authoritative `CampusSessionState`; `*NetworkSeamPlayModeTests`; client wrong-then-right submission test. Live two-player demo = HUMAN |
| Levels or character progression in place | **DONE** | Badges/passport, Career DNA traits, Career Reveal, campus evolution pieces, accessory rewards |
| Performance profiled, low-latency with multiple concurrent players | **PARTIAL → HUMAN** | Architecture is low-traffic by design (client submits discrete actions; host broadcasts low-frequency state). Actual concurrent-player profiling needs a live run |
| Video: multiplayer in action (2+ players) + progression | **HUMAN** | Recording task |

## Gate 3 — Polish & Ship

| Rubric line | Status | Evidence |
|---|---|---|
| All prior gates hold end-to-end, no regressions | **DONE** | EditMode 241/241 + PlayMode 233/233 green |
| Polish: UI/UX, gameplay balance, engagement | **DONE (floor) / enhancements open** | Multiple design-review passes (verb-aware copy, un-masked buildings, de-cluttered yard, real toy art on trace/deduce/launch). Above-floor enhancements remain optional (see below) |
| Stress tested at max concurrent players, low-latency confirmed | **HUMAN** | Needs a live multi-client stress run |
| Deployed and accessible for testing | **HUMAN** | `DEPLOY.md` gives the build + itch.io publish steps; the actual publish + access sign-off is a person task |
| GitHub repo complete: architecture, setup/deploy guide, decisions + rationale | **DONE** | `README.md` (refreshed), `docs/architecture.md` (refreshed to the shipped party-station game), `DEPLOY.md`, `docs/technical-decisions.md` |
| 5-min demo video: gameplay + technical walkthrough + AI-dev reflection | **HUMAN** | `BRAINLIFT.md` + `docs/technical-decisions.md` supply the reflection material; recording is a person task |

## Human-only punch list (cannot be AI-completed)

1. **Three demo videos** — (a) Gate-1 docs/architecture + single-player walkthrough, (b) Gate-2 two-player multiplayer + progression, (c) the 5-min final (gameplay + technical walkthrough + AI-augmented-dev reflection). Script material is in this doc, `docs/demo-checklist.md`, `docs/technical-decisions.md`, and `BRAINLIFT.md`.
2. **Live multiplayer playtest + concurrent-player stress test** — confirm two+ clients on real hardware, low latency at max players.
3. **Deploy + "accessible for testing" sign-off** — run `DEPLOY.md` to build + publish to itch.io, then confirm the live page is downloadable and runs on a clean machine.

## Above-the-floor enhancements (not required to pass the rubric)

Tracked from the plan docs; the rubric's "polish/engagement" line is already met without them:

- **Green City Builder station** + the `BalanceMeters` toy primitive (the one Wave-2 station not yet built).
- **Health Hero + Logic Court** conversion to the flagship drag-and-drop station pattern.
- **Career reveal synthesis** — expanded career paths / combo cards by completion count.
- **Art passes** that degrade gracefully today: evolution city pieces, station badges, room interiors.
- **Audio by-ear soundcheck** (all 29 cues resolve; never heard in context).
- **Cleanup:** remove the now-dead `AddStationSite` helper (superseded by real buildings).
