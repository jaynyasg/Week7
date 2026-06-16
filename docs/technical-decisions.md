# Key Technical Decisions & Rationale

Why Career Quest Campus is built the way it is. Each decision lists the choice,
the reason, and the tradeoff accepted. This is the "why" companion to
`architecture.md` (the "what").

## Engine: Unity `6000.4.10f1`

**Why:** the game is a bright 2.5D diorama campus with polished 2D characters and
strong feedback moments — Unity gives mature 2D tooling, a one-click Windows
build, and (decisively) first-party real-time networking. **Tradeoff:** heavier
than a web engine (Phaser/Three.js) for a 2D game, accepted for the networking
and build maturity. WebGL stays preview-only.

## Networking: Netcode for GameObjects + Unity Transport, host-authoritative

**Why:** the host/server is the single source of truth for everything that
affects fairness — activity timers, accepted placements, duplicate rejection,
result emission, best-result replacement, Career DNA, badge source/tier. Clients
send discrete action requests and render only host-accepted state. For a kids'
co-op game this keeps two players' progress consistent and impossible to desync
by spamming input. **Tradeoff:** a client never completes optimistically (a
submit returns `Pending` until the host's accept replicates), so there is a small
round-trip latency on success — acceptable because actions are deliberate taps,
not twitch input.

## One persistent gameplay scene; activities are states, not Netcode scenes

**Why:** Netcode scene management (per-room `NetworkSceneManager` loads) adds
late-join and state-sync complexity that is real risk for a one-week build. P0
runs everything in one persistent scene; mini-games and stations are
states/rooms inside it. **Tradeoff:** one large scene instead of clean per-room
scenes; revisiting a scene split is explicitly deferred (`TODOS.md`).

## Definition-driven station framework (one controller, ten stations)

**Why:** the 10 Party Pack stations are **data, not code**. Each is a
`PartyStationDefinition` (id, verb, objects, seeds, rewards, art keys); a single
`PartyStationController` + `ToyPatternRules` core renders and runs all of them.
Adding a station is adding a definition, not a class. The same `ToyPatternRules`
instance runs in solo play, on the host, and in EditMode tests, so the rules are
proven once and reused everywhere. **Tradeoff:** the framework must be general
enough to express distinct verbs (trace / shoot / deduce / balance / compose /
match), which concentrates complexity in `ToyPatternRules` — paid down by an
extensive pure-rules EditMode suite.

## The shared drop seam: `TrySubmitDrop`

**Why:** every action — pointer drag, automated test, network submission — funnels
through one method, `PartyStationController.TrySubmitDrop(pieceId, targetId, value)`,
returning a `DropSubmitResult`. Solo runs `ToyPatternRules.Submit()` directly; multiplayer
routes the same call through a `ServerRpc` to the host's `ApplySubmission()`, which
runs the **same** `Submit()`. One validation path means the tests drive exactly
what players and the network drive. **Tradeoff:** none worth noting — this is the
load-bearing seam and the reason the suite is trustworthy.

## Distinct interaction verbs (`ToyPatternId`)

**Why:** identical drag-to-slot across ten stations would feel same-y. Each
station picks a verb — `ShootTarget` (launch a toy at a goal), `TracePath` (tap an
ordered route), `DeduceAnswer` (cross out wrong cards), `BalanceMeters` (tune
dials into a green band), `ComposeSet`, `MatchAndCare`, `PickMatchingTrio` — so the
mechanic matches the career fantasy. All run over the one drop seam. **Tradeoff:**
more surface area in the rules engine and renderer; mitigated by sharing the
seam, the kit, and the placeholder/art pipeline across all verbs.

## Pointer-first, non-color cues, no harsh fail (the "R19" rules)

**Why:** the audience is young kids and classrooms. Every interaction works with a
pointer; order/state is shown with shape/number/text, not color alone; a wrong
move bounces gently and offers a hint ladder instead of failing. **Tradeoff:**
some "game feel" intensity is traded for accessibility and a no-frustration floor
— the right call for the audience.

## Best-result-only scoring

**Why:** replay is allowed, but only each room's best result counts (ranked by
completion tier, then time remaining, then accuracy), and Career DNA is recomputed
from best results. This prevents trait inflation from replay farming. **Tradeoff:**
a great-then-worse replay is silently ignored — intended.

## Strength-based Career Reveal, never deterministic

**Why:** the reveal unlocks after 3 unique completed rooms and speaks in
strength phrases ("Good / Strong / Very strong match"), shows co-leads on close
ties, and frames careers as paths worth exploring — never a life assignment. 30
careers across 6 families are ranked by a weighted trait dot-product. **Tradeoff:**
less "gotcha" precision, deliberately, because telling a child their fixed future
is the wrong product.

## Placeholder → final art policy with a fallback gate

**Why:** gameplay ships before the art pipeline finishes. Toy/badge/building/
evolution assets are cataloged `required: false`; a key with no final PNG resolves
to a tinted handmade token (never the magenta missing-checker). `IsFinalArt` and a
player-facing fallback gate (asserted in tests) track which keys still need art.
**Tradeoff:** some surfaces show tokens until their art pass lands — visible, but
never broken, and the gate prevents a placeholder from silently shipping as
"final."

## Privacy: no accounts, chat, or persisted child data

**Why:** child-safety and compliance. No accounts, saved profiles, persistent
child data, analytics, telemetry, or chat; display names are session-only. The
network replicates **indexes and values** (accepted-object indexes, meter values,
compact reward facts) — never drag positions, free text, or names. **Tradeoff:**
no cross-session progression or leaderboards — an intentional non-goal for this
audience.
