# Career Quest Campus — 5-Minute Demo Video Script

**Target length:** 5:00. **Date:** 2026-06-17.

How to use this: text in quotes is **what to say**. `[ON SCREEN]` lines are **what to do / show**. Time windows are guides — narrate at a calm ~140 words/min and let gameplay breathe. Sections: (1) Showcase walkthrough, (2) Real gameplay + loop, (3) Technical walkthrough, (4) AI-augmented development reflection.

Record the Windows build (the multiplayer proof). Have a second local client ready to launch for the multiplayer beat.

---

## Section 1 — Showcase Walkthrough (0:00 – 1:00)

`[ON SCREEN]` Launch the build. Entry screen: the "Career Quest Campus" wordmark over the live campus diorama, three buttons (Play / Showcase / Multiplayer).

> **(0:00–0:12)** "This is Career Quest Campus — a multiplayer career-discovery game for kids. Instead of a quiz, children explore a toy campus, play hands-on career mini-games, and discover careers that match their strengths. Let me start with the built-in guided tour."

`[ON SCREEN]` Click **Showcase**. The friendly disclaimer appears. Pause on it, then click Accept.

> **(0:12–0:22)** "Pressing Showcase starts a curated tour. It opens honestly — a short disclaimer that tells you this is a guided walkthrough, and that 'Play' is right there for free exploration. I'll accept it."

`[ON SCREEN]` Tour auto-plays. **Beat 1 — Two-Client Proof.**

> **(0:22–0:32)** "First beat: the two-client proof. It shows the game is genuinely multiplayer — host and client — while being upfront that the tour simulates two local players for reliability. Real networking is proven separately in QA."

`[ON SCREEN]` **Beat 2 — Free Campus + Future Labels.**

> **(0:32–0:40)** "Next, the campus — color-coded districts, labeled career buildings, and future-career signs that promise a world bigger than this build."

`[ON SCREEN]` **Beat 3 (NEW) — Ten Career Stations montage.** It fans through representative stations: Robotics (launch), Weather (trace), AI Lab (deduce), Green City (balance).

> **(0:40–0:52)** "Then the new station montage. There are ten career stations, and each one plays differently — you launch a rescue robot, trace a flight path, deduce the right answer, balance a city's meters. The variety is the whole point: every career feels different in your hands."

`[ON SCREEN]` **Beat 4 — Future City Design Build** auto-completes (five pieces snap in). **Beat 5 — Achievement Gallery** (badge wall). **Beat 6 — Career Reveal.**

> **(0:52–1:00)** "It closes with the cooperative Future City build, an Achievement Gallery of earned badges, and the Career Reveal — here, Architect and AI Engineer as co-leads, framed as a 'Future Problem Solver.'"

---

## Section 2 — Real Gameplay + Loop (1:00 – 2:30)

`[ON SCREEN]` Back to the entry screen. Click **Play**. Avatar selection — pick an avatar. Enter the campus; move with WASD.

> **(1:00–1:12)** "Now the real thing. I'll choose Play, pick an avatar, and walk the campus myself — no seeding, no script. This is exactly what a child plays."

`[ON SCREEN]` Walk to **Robotics Rescue**. Play the launch verb: pull back and fire a robot part onto the rescue spot. Complete it — show the **Degree** stamp and the new badge.

> **(1:12–1:32)** "Here's the loop. I walk up to a career station and play it. Robotics Rescue is a launch verb — pull back and fire the part onto the target. Finishing earns a Degree stamp, a badge, and trait points that feed my Career DNA."

`[ON SCREEN]` Walk to a second station with a **different verb** — the **AI Lab** (tap to cross out the wrong rules) or **Weather Lab** (trace the path). Play and complete it.

> **(1:32–1:50)** "A different station, a completely different action — the AI Lab is a deduction puzzle where you eliminate the wrong rules until the right one survives. That's the design bet: ten careers, seven distinct interaction verbs, so breadth never turns into repetition."

`[ON SCREEN]` Open **Multiplayer → Host P1**. Launch the second local client → **Join Localhost as P2**. Enter **Design Build Studio**. Place a piece as P1 (P2's screen updates); place a piece as P2; attempt a duplicate placement → show the gentle rejection.

> **(1:50–2:12)** "It's real multiplayer too. I'll host as Player 1 and join a second local client as Player 2, then enter the cooperative Future City build. When I place a piece, my partner sees it instantly. If we both reach for the same slot, the host accepts the first and gently rejects the second — host-authoritative, so there are no conflicts."

`[ON SCREEN]` Complete a third unique room so the reveal unlocks (3/3). Open the **Achievement Gallery**, then trigger **Career Reveal** — show top traits, the primary career, co-leads, and the confidence phrase.

> **(2:12–2:30)** "After three unique rooms, the Career Reveal unlocks. The Gallery shows everything earned, and the reveal reads my strongest traits, names a best-fit career with possible co-leads, and stays strength-based — 'a path worth exploring,' never 'this is your future.'"

---

## Section 3 — Technical Walkthrough (2:30 – 3:45)

`[ON SCREEN]` Optional: cut to the editor / a code file, or stay on gameplay and toggle the **debug overlay** (backtick) to show live network + game facts.

> **(2:30–2:45)** "Technically, it's Unity 6 with Netcode for GameObjects, host-authoritative. One persistent scene — the mini-games are rooms inside it, not separate networked scenes to load."

> **(2:45–3:05)** "The ten stations aren't ten code paths — they're data. A single station controller renders every station from a definition: its verb, its art, its seeds, its traits, its rewards. Adding a career means adding a definition, not building a new system."

> **(3:05–3:22)** "Every action funnels through one 'drop' seam — submit a piece to a target. Solo play runs the rules directly; multiplayer routes the same rules through the host; the tests call the same seam. The logic is proven once and reused everywhere."

> **(3:22–3:38)** "Scoring keeps only your best result per room, so replays can't inflate your Career DNA. And it's privacy-first: no accounts, no chat, no telemetry. The network replicates only indexes and values — never names, never drag positions."

> **(3:38–3:45)** "All of it is gated by tests — over 240 edit-mode and 230 play-mode tests, green before every ship."

---

## Section 4 — AI-Augmented Development Reflection (3:45 – 5:00)

`[ON SCREEN]` Optional: show the `docs/` tree — `brainstorms/`, `plans/`, `solutions/`, `qa/` — or `BRAINLIFT.md`.

> **(3:45–4:02)** "Last, how it was built. This was a ten-day sprint built with an AI-augmented, compound-engineering workflow. Every feature ran the same cycle: brainstorm the idea, put it through CEO, engineering, and design plan reviews, then hand implementation to AI subagents."

> **(4:02–4:25)** "The biggest lesson was that 'built' isn't the same as 'visible.' Early on, logic tests were green while the screen still showed placeholders — the code worked, but the player saw nothing. The fix was a design-review step that drives the game headlessly and screenshots every screen. Verifying with real screenshots instead of just unit tests was the single highest-leverage change in the whole project."

> **(4:25–4:45)** "The workflow also compounds. Whenever we solved something hard — like that data-versus-display gap — we wrote it into a solutions library that the agents read before touching the same area again. So the system gets smarter each cycle instead of repeating mistakes."

> **(4:45–5:00)** "The result is a complete, polished, multiplayer game — ten career stations, seven distinct verbs, a full discovery loop — built fast without trading away quality, because the AI handled implementation while the workflow enforced planning, design review, and verification. Thanks for watching."

---

## Timing Cheat Sheet

| Time | Section | On-screen anchor |
|------|---------|------------------|
| 0:00 | Showcase | Entry screen -> Showcase -> disclaimer |
| 0:22 | Showcase | Two-Client Proof -> Campus -> **Stations montage** -> Build -> Gallery -> Reveal |
| 1:00 | Real play | Play -> avatar -> campus -> Robotics (launch) |
| 1:32 | Real play | Second verb (AI Lab deduce / Weather trace) |
| 1:50 | Real play | Host P1 + Join P2 -> Design Build co-op + rejection |
| 2:12 | Real play | 3rd room -> Gallery -> Career Reveal |
| 2:30 | Technical | Unity 6 + Netcode, persistent scene |
| 2:45 | Technical | Definition-driven stations, one drop seam, best-result scoring, privacy, tests |
| 3:45 | AI reflection | Workflow cycle |
| 4:02 | AI reflection | "Built != visible" + screenshot verification |
| 4:25 | AI reflection | Compounding solutions library |
| 4:45 | AI reflection | Close |

## Recording Notes

- The Section 1 montage beat depends on the Showcase update (see `docs/brainstorms/2026-06-17-showcase-refresh-requirements.md`). If you record before that ships, either skip the montage line or narrate over the existing campus beat.
- Keep narration ahead of the auto-advancing tour in Section 1 — the beats dwell only ~1.25s each, so you may want to pause/scrub or record narration as voiceover.
- For the multiplayer beat, arrange both windows side by side before recording so the "partner sees it instantly" moment is visible in one frame.
