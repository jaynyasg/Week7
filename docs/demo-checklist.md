# Demo Checklist

Use this checklist for the five-minute evaluator path and for local smoke testing. Showcase is the preferred evaluator route; live host/client testing remains required QA proof.

## Showcase Evaluator Path

1. Open the game.
2. Select `Showcase`.
3. Confirm the friendly guided-tour disclaimer appears before seeded state or auto-advance.
4. Accept the disclaimer and confirm Showcase begins.
5. Within the first minute, verify the evaluator sees two-player proof, Career DNA or badge meaning, and at least one larger-campus signal.
6. Enter or auto-enter Future City Design Build.
7. Place or watch accepted city pieces with clear contribution feedback.
8. Open Achievement Gallery and verify badges feel celebratory without visible seeded/tour labels.
9. Trigger Career Reveal and verify Architect + AI Engineer co-leads, Future Problem Solver language, and strength-based copy.
10. Confirm the Showcase path can complete in under three minutes.

## Play Free-Campus Path

1. Open the game.
2. Select `Play`.
3. Confirm the game enters the normal connection/free-campus flow.
4. Confirm no Showcase seeded results, presenter auto-advance, or forced route are active.

## Required Live Multiplayer Path

1. Open the game.
2. Select `Host P1`.
3. Launch a second local client on the same computer.
4. Select `Join Localhost as P2`.
5. Confirm split controls are visible on the connection screen.
6. Enter campus with both players visible.
7. Move both players for at least 30 seconds using split controls.
8. Enter Design Build Studio.
9. Place a blueprint piece as P1 and confirm P2 sees it.
10. Place a blueprint piece as P2 and confirm P1 sees it.
11. Attempt simultaneous or duplicate placement and confirm the second action gets gentle rejection feedback.
12. Complete the activity and verify one shared result appears on both clients.
13. Open Achievement Gallery and verify badge tier, source, and trait updates.
14. Confirm Career Reveal remains locked after one completed mini-game and shows `1/3` progress.
15. Complete the remaining themed mini-games and confirm the reveal unlocks at `3/3`.
16. Trigger Career Reveal and verify strength-based language, co-lead handling if applicable, and confidence phrase.
17. Toggle debug overlay and confirm network + game facts are visible.

## Solo Fallback Path

1. Open the game.
2. Select `Solo Fallback`.
3. Confirm fallback mode is clearly labeled.
4. Verify normal keyboard + mouse controls are available.
5. Enter Design Build Studio.
6. Complete the activity.
7. Verify source metadata labels the result as fallback/solo while the ceremony stays celebratory.

## Three Mini-Game Path

Each mini-game has one polished Week7 challenge:

- Design Build Studio: place correct shapes/colors into blueprint slots.
- Health Hero Clinic: match symptoms to tools/treatments.
- Logic Court: sort evidence and choose the strongest closing argument.

For each mini-game:

1. Start from campus.
2. Complete the activity in under two minutes.
3. Confirm success produces a `Degree` stamp.
4. Confirm partial/soft failure produces a `Practice` stamp.
5. Confirm the best result replaces weaker prior attempts.
6. Confirm Career DNA totals recompute instead of stacking replay attempts.
7. Confirm Solo Fallback supports normal keyboard + mouse controls for the activity.

## Connection Testing

### Same-Computer Required Path

1. Run host and client from one computer.
2. Use `Host P1` and `Join Localhost as P2`.
3. Verify split controls work in campus.
4. Verify split controls work in Design Build Studio.
5. Confirm both clients see shared state changes.

### LAN Optional Path

LAN should be implemented if practical, but is not a blocker unless tested.

1. Host starts session.
2. Host displays or documents local IP if available.
3. Client selects `Join LAN by IP`.
4. Client enters host IP.
5. Confirm both devices enter campus.
6. If this path is not tested, mark it experimental in README and QA evidence.

### LAN Discovery Stretch

If automatic LAN discovery/server browser is attempted:

1. Confirm host appears in discovery list.
2. Confirm joining from discovery works.
3. If discovery is incomplete or untested, hide it or label it experimental.

## Forced Failure Checks

Before demo-ready:

1. Force host startup failure and verify retry/return controls.
2. Force client join failure and verify visible guidance.
3. Disconnect one client during campus movement and verify the surviving client remains responsive.
4. Trigger Design Build Studio timeout and verify safe result/return path.
5. Submit duplicate placement and verify result is applied once.
6. Trigger invalid/empty mini-game result and verify fallback copy.
7. Open WebGL preview, if present, and verify fallback message when multiplayer is unavailable.

## Build And Distribution

1. Package Windows build.
2. Record build path in `docs/qa/`.
3. Record Unity version.
4. Record build size.
5. Run same-computer host/client smoke test from build or editor + build.
6. Create itch.io page with Windows build, screenshots, controls, and fallback notes.
7. Add optional WebGL preview only if it loads and behaves.
8. If WebGL multiplayer is unavailable, state clearly that Windows build is the multiplayer proof.
