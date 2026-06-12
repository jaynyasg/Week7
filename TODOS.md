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

### Execute Manual Same-Computer 2P Matrix

**What:** A human runs the six-row two-client checklist in `docs/qa/2026-06-12-wow-pass-final.md`: (a) reject delivery, (b) shared placement rendering, (c) re-entry attempt reset, (d) reveal latch/skip independence, (e) emote delivery, (f) partner held-piece glow. Record PASS/FAIL per row in that doc.

**Why:** Same-computer host/client testing is required by `docs/qa/README.md` and is the only outstanding half of AE2/AE7 — the automated host-authority and latch suites are green, but no human has driven two clients since the drag conversion.

**Context:** Setup is two instances of `Builds/Windows/CareerQuestCampus.exe`: Host Game + Join This PC (P1: WASD + F, P2: IJKL + Enter). Carried from the U9 flagship review's outstanding matrix, extended with the U12 emote/held-piece rows.

**Effort:** S (~30 min)
**Priority:** P1
**Depends on:** Nothing — build and checklist are ready.

### Audio By-Ear Soundcheck Pass

**What:** Play the flagship path (campus → Design Build → ceremony → reveal) and each room with audio on; swap any cue-to-clip mappings that sound wrong.

**Why:** All 29 cue IDs resolve to real clips (AudioCueCoverage green), but the clips were curated by filename/semantics, never heard in context.

**Context:** Mappings live in `Assets/Resources/Audio/`; candidate replacements remain in `Assets/_CareerQuest/Art/Kenney/Audio/`. Swaps are file/name changes only — no code expected.

**Effort:** S
**Priority:** P2
**Depends on:** Nothing.

### Fix Optional-Room Action Button Contrast

**What:** Restyle the bottom action buttons in the four optional rooms (e.g. Robotics "Build Robot", Music "Record Beat", shared "Complete Quest") so they read against the cream instruction-strip band.

**Why:** They currently render pale on pale (visible in all four optional-room captures in `SubmissionBundle/screenshots/`); cosmetic but below the DESIGN.md contrast bar for kid-facing controls.

**Context:** Known minor issue 1 in `docs/qa/2026-06-12-wow-pass-final.md`. Likely a button-style/color token fix in the optional-room chrome, not a layout change.

**Effort:** S
**Priority:** P2
**Depends on:** Nothing.

## Completed

### Wow Quality Pass (2026-06-12)

Landed on `feat/wow-quality-pass` (U1–U13 commits `2c88091`…`ecc9d83`; final ladder EditMode 73/73, PlayMode 155/155, packaged Windows build green). Closed the long-missing Wave 4 `SubmissionBundle/` deliverable plus the audited gaps: imported Kenney/Fredoka/Lexend art pipeline, authored campus + room prefabs, drag-and-drop core rooms, in-world reveal cinematic, full audio cue set, TMP typography, optional-room badge art, pause/settings menu, and build packaging. Evidence: `docs/qa/2026-06-12-wow-pass-final.md`, `docs/qa/2026-06-11-flagship-slice-review.md`, `SubmissionBundle/README.md`.
