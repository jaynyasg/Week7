# TODOS

## Career Quest Campus

### Expand Living Career Campus After Week7

**What:** Add more career buildings, mini-games, end careers, avatar rewards, and curriculum depth after the Week7 demo ships.

**Why:** Preserves the 12-month Living Career Campus vision without letting future content compete with the Week7 P0/P1 deliverable.

**Context:** The CEO review accepted an ambitious Unity scope for Week7, but the implementation plan protects a complete first slice: networked campus, Design Build Studio, Career DNA, Career Passport, guide, reveal ceremony, guided demo, desktop build, and required documentation. After that loop is stable, expand toward the full career-cluster campus with more buildings, activities, careers, and progression systems.

**Effort:** L
**Priority:** P3
**Depends on:** Week7 P0 demo loop shipped and reviewed.

### Evaluate Netcode Scene Split After P0

**What:** Revisit whether mini-games should move from one persistent gameplay scene into separate Unity scenes loaded through Netcode `NetworkSceneManager`.

**Why:** P0 intentionally keeps mini-games as states/UI panels inside one gameplay scene to avoid scene synchronization risk during the sprint. After the core loop ships, separate scenes may improve long-term organization for a larger campus.

**Context:** The engineering review chose a persistent-scene architecture for Week7 because the repo is starting from docs/assets only and Netcode scene transitions add late-join and synchronization edge cases. If Career Quest Campus expands beyond the first three activities, evaluate whether each building should become a separate Netcode-managed scene, and only migrate after the current host/client, Career DNA, passport, reveal, and recovery paths have stable tests.

**Effort:** M
**Priority:** P3
**Depends on:** Week7 P0 demo loop shipped and reviewed.

### B-Narrow Late Join (Hub Catch-Up)

**What:** Allow P2 to join after host has started, but only when host is on Campus. Host pushes full `CampusSessionState` + `GameSession` mirror (results, DNA, reveal gate, evolution pieces, avatars). Reject join if host is in a room, ceremony, gallery, or reveal.

**Why:** Useful for LAN/dev testing when P2 launches late; deferred from Week7 to keep Netcode surface small for ship. CEO review chose lobby-only connect (S2-A) for first playable.

**Context:** Requires snapshot DTO, `GameSession.ApplySnapshot()`, `OnClientConnected` ClientRpc, and hub bootstrap tests. Mid-room full snapshot (Health Hero / Logic Court network state) remains out of scope even for B-narrow.

**Effort:** S–M (~2–3 days)
**Priority:** P3
**Depends on:** Week7 first playable shipped; `CampusSessionState` + disconnect/ceremony policies (S1/S2) stable.

### Audio By-Ear Soundcheck Pass

**What:** Play the flagship path (campus → Design Build → ceremony → reveal) and each room with audio on; swap any cue-to-clip mappings that sound wrong.

**Why:** All 29 cue IDs resolve to real clips (AudioCueCoverage green), but the clips were curated by filename/semantics, never heard in context.

**Context:** Mappings live in `Assets/Resources/Audio/`; candidate replacements remain in `Assets/_CareerQuest/Art/Kenney/Audio/`. Swaps are file/name changes only — no code expected.

**Effort:** S
**Priority:** P2
**Depends on:** Nothing.

## Completed

### Two-Process 2P Matrix Automated (2026-06-12)

The six-row matrix no longer needs a human pass: `TwoPlayerMatrixSmoke`
(`-cq-smoke -cq-mode 2p-host` / `2p-client`) runs two built-player processes
over localhost through all six scenarios with state-based synchronization.
First run: client 6/6 PASS, host 3/3 PASS, exit codes 0/0 — wire reject
delivery with submission-id echo, shared placement rendering, attempt reset
over RPC, reveal latch fallback + per-client skip independence, emote delivery
to both screens, and partner held-piece glow all verified on real processes.
Results recorded in `docs/qa/2026-06-12-wow-pass-final.md`; logs at
`Builds/logs/2p-{host,client}.log`. Re-run anytime with the two command lines
in that doc.

### Optional-Room Action Button Overlap Fixed (2026-06-12)

Root cause was placement, not color: the action buttons sat at y −320, under
the instruction strip's translucent band (y −337..−273), which rendered over
them. Optional-room tray/Complete/Campus and the three core-room Campus
buttons moved to y −238 (above the band); chrome tests now assert the
above-strip contract. Verified in a fresh Robotics capture — all buttons
render at full contrast.

### Wow Quality Pass (2026-06-12)

Landed on `feat/wow-quality-pass` (U1–U13 commits `2c88091`…`ecc9d83`; final ladder EditMode 73/73, PlayMode 155/155, packaged Windows build green). Closed the long-missing Wave 4 `SubmissionBundle/` deliverable plus the audited gaps: imported Kenney/Fredoka/Lexend art pipeline, authored campus + room prefabs, drag-and-drop core rooms, in-world reveal cinematic, full audio cue set, TMP typography, optional-room badge art, pause/settings menu, and build packaging. Evidence: `docs/qa/2026-06-12-wow-pass-final.md`, `docs/qa/2026-06-11-flagship-slice-review.md`, `SubmissionBundle/README.md`.
