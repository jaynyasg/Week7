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

## Completed
