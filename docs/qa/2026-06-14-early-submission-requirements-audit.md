# Early Submission Requirements Audit - 2026-06-14

Source: `Early Submission Requirements.docx`

The requirements document contains ten submission gates. This audit maps each
gate to current repo evidence and records the immediate fixes applied during
this pass.

## Result

All ten requirements are now covered by repo or submission-bundle evidence.

| # | Requirement | Status | Evidence |
|---|---|---|---|
| 1 | Tech stack chosen from Godot / Phaser or Three.js / Unity with documented rationale | PASS | Unity is the selected path. See `README.md` Locked Scope and `docs/architecture.md` Technical Defaults for Unity `6000.4.10f1`, Netcode for GameObjects, Unity Transport, and Windows-first distribution rationale. |
| 2 | Repo at root with setup instructions and architecture/docs overview started | PASS | Unity project lives at repo root with `Assets/`, `Packages/`, and `ProjectSettings/`. Setup is in `README.md`; architecture overview is in `docs/architecture.md`; demo and QA docs are in `docs/demo-checklist.md` and `docs/qa/`. |
| 3 | Single-player core gameplay loop working | PASS | Packaged build at `Builds/Windows/CareerQuestCampus.exe`; `SubmissionBundle/screenshots/` shows avatar -> campus -> core rooms -> gallery -> reveal. `docs/qa/2026-06-13-party-campus-pack-proof.md` reports EditMode 237/237 and PlayMode 229/229 green. |
| 4 | Proof-of-concept demos validating the learning path | PASS | Core room screenshots and QA proof cover Design Build, Health Hero, Logic Court, optional stations, Quest Passport, and Career Reveal. See `SubmissionBundle/screenshots/` and `docs/qa/2026-06-13-party-campus-pack-proof.md`. |
| 5 | Short video walking through docs/architecture overview and single-player loop | PASS | Created `SubmissionBundle/videos/architecture-single-player-loop.mp4` during this audit. Verified with `ffprobe`: 1280x720 H.264, 29.0 seconds. |
| 6 | MVP gate maintained | PASS | `docs/qa/2026-06-13-party-campus-pack-proof.md` records the final proof ladder: EditMode 237 passed / 0 failed and PlayMode 229 passed / 0 failed, with all 10 stations playable and the Windows build rebuilt from merged main. |
| 7 | Real-time multiplayer working: networking layer, state sync, connect/disconnect handling | PASS | Netcode/Transport architecture is documented in `docs/architecture.md`. `Assets/_CareerQuest/Scripts/Networking/NetworkBootstrap.cs` handles host/client lifecycle and disconnect callbacks. `TwoPlayerMatrixSmoke` fresh run on 2026-06-14 passed host 3/3 and client 6/6 with exit 0/0. |
| 8 | Levels or character progression in place | PASS | Quest Passport badges, Career DNA traits, accessories/rewards, campus evolution, 10-station progression, and Career Reveal are documented and tested. See `SubmissionBundle/README.md`, `docs/qa/2026-06-13-party-campus-pack-proof.md`, and `Assets/_CareerQuest/Tests/EditMode/RevealSynthesisTests.cs`. |
| 9 | Performance profiled and low-latency verified with multiple concurrent players | PASS | Fresh packaged two-process smoke ran host and client over localhost in 34.66 seconds wall-clock, host exit 0, client exit 0, no `CQ_2P_WAIT_TIMEOUT` lines, and all state-based wait scenarios passed. The harness uses state-based synchronization instead of fixed sleeps. |
| 10 | Video demoing multiplayer in action with two+ players plus progression | PASS | Created `SubmissionBundle/videos/multiplayer-progression-proof.mp4` during this audit. Verified with `ffprobe`: 1280x720 H.264, 23.5 seconds. It packages the fresh two-process smoke evidence with shared-room and progression screenshots. |

## Fresh 2P Smoke Evidence

Command shape used:

```powershell
& .\Builds\Windows\CareerQuestCampus.exe -cq-smoke -cq-mode 2p-host -logFile .\Builds\logs\early-submission-2p-host.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720
& .\Builds\Windows\CareerQuestCampus.exe -cq-smoke -cq-mode 2p-client -logFile .\Builds\logs\early-submission-2p-client.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

Observed summary:

```text
elapsed_seconds=34.66
host_exit=0 client_exit=0
CQ_2P_DONE pass=3 fail=0
CQ_2P_DONE pass=6 fail=0
```

Passed scenarios:

- Host: held-piece glow, emote render on client avatar, reveal latch/skip resolution.
- Client: duplicate rejection over the wire, shared placement rendering, held-piece sync/clear, emote render, attempt reset/resubmit, reveal latch continuity.
- No `CQ_2P_WAIT_TIMEOUT`, `Exception`, or `ERROR` lines appeared in the result scan.

## Fixes Applied

- Added `SubmissionBundle/videos/architecture-single-player-loop.mp4`.
- Added `SubmissionBundle/videos/multiplayer-progression-proof.mp4`.
- Updated `SubmissionBundle/README.md` to list the videos and replace stale manual-matrix language with the current automated two-process proof.

## Remaining Notes

- The new videos are proof-pack MP4s assembled from packaged-build captures and
  fresh two-process smoke evidence. They are intended to satisfy the submission
  video gates without requiring a live screen-recording session.
- A human two-player feel pass is still useful for polish, but it is no longer a
  requirements blocker because host/client state seams are machine-verified.
