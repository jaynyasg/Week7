# QA Report — Wow Quality Pass Final Sweep (U14)

## Build

- Commit: U13 head (`ecc9d83`) on `feat/wow-quality-pass` (U1–U13 landed as
  individual commits: `2c88091` U1, `5cb92cd` U2, `b1036a0` U3, `59f10c8` U4,
  `95f2372` U5, `03c0b7b` U6, `3a1f979` U7, `c915ef4` U8, `fb8209b` U9
  evidence, `f7db337` U10, `71dad22` U11, `9e1e6ed` U12, `ecc9d83` U13)
- Unity version: `6000.4.10f1`
- Build target: StandaloneWindows64
- Build path: `Builds/Windows/CareerQuestCampus.exe` (product name
  "Career Quest Campus", custom icon, Campus Sky splash)
- Test ladder (U13 verification run): sprite-kit generator fallback-only
  (0 overwrites confirmed) → EditMode **73/73** → PlayMode **155/155** →
  packaged Windows build — **all green**

## Environment

- Machine: Windows 11 Pro dev machine (same as flagship review)
- Input devices: keyboard + mouse
- Same-computer host/client tested: **automated yes / manual matrix pending
  (owner action — checklist below)**
- LAN tested: NOT TESTED (Join IP path exists; localhost path is the shipped
  configuration)

## Evidence set (`SubmissionBundle/screenshots/`, 1280x720, captured from the packaged build)

| Capture | State | Observed |
|---|---|---|
| `avatar.png` | Avatar selection | Four Kenney Toon hero passport cards (Sky Builder selected, Care Captain, Logic Spark, Art Inventor) with career-color stripes; large passport preview card with hero description; Back / Enter Campus actions. Fredoka display + Lexend body throughout. |
| `campus.png` | Campus hub | Authored toy diorama: parallax sky/hills, three core buildings with colored roofs and door signs, optional-room row (AI Lab, Music Studio, Robotics, Kitchen), robot guide first-run speech bubble ("Hi Sky Builder! Try the Health Hero room first!"), 0/3 badge HUD with controls hint, footer walk prompt. |
| `design-build.png` | Design Build (drag) | Future City Workshop: blueprint table with five pastel slot pads, draggable city-piece tray, 0/5 quest HUD card, player + builder NPC, drag instruction strip. |
| `health.png` | Health Hero (drag) | Clinic diorama: patient on exam bed with thermometer, care-tool tray (symptom clipboard, kit, bandage, care plan), 0/3 care steps, mint dressing. |
| `logic.png` | Logic Court (drag) | Courtroom diorama: judge bench + gavel, case-file podium, two sorting zones, four-card evidence tray, 0/3 sorted, judge NPC. |
| `music.png` | Music Studio (optional) | Lavender studio: bunting, keyboard, speaker, player + robot NPC. Action buttons visible but pale against the strip (known issue 1). |
| `ai-lab.png` | AI Space Lab (optional) | Blue lab: model console with robot face, porthole windows, bunting. Same pale-button issue. |
| `robotics.png` | Robotics Garage (optional) | Teal garage: helper-robot parts on workbench (head, body, wheel, battery). Same pale-button issue. |
| `kitchen.png` | Community Kitchen (optional) | Green kitchen: steaming pot, ingredient shelf, serving counter. Same pale-button issue. |
| `gallery.png` | Quest Passport | Passport book with spiral binding, seven sticker slots (locked "?" state — fresh session), five Career DNA chips, "Reveal unlock: 0/3 unique quest badges", Reveal (Locked) + Campus. |
| `reveal-locked.png` | Reveal locked branch | Stage building behind the card, three dark "?" slots, n/3 progress bar ("0/3 quest badges collected" in this fresh-session capture), "games to go" guidance, **no Skip control** — matches the locked-branch contract. |
| `reveal-unlocked.png` | Reveal resolved | In-world stage with gold/blue light beams (faked 2D lighting), three badge tokens landed in glowing slots, "REVEAL UNLOCKED! AI Engineer + Architect — Very strong match", strength-clue + "not a life assignment" copy. |

No procedural fallback or checker sprites are visible in any capture.

## Gate results (inside the green suites above)

| Gate | Result |
|---|---|
| Extended zero-fallback gate over the full player-facing catalog (R3/AE4) | PASS |
| AudioCueCoverage — every referenced cue ID resolves to a clip (29 IDs incl. hub toys) | PASS |
| AE5 zero-legacy-`Text` scan (all type via TMP Fredoka/Lexend) | PASS |
| Sprite-kit generator fallback-only (fills missing, never overwrites curated art) | PASS (0 overwrites) |
| Packaged Windows build | PASS |

## Acceptance examples (origin doc AE1–AE7)

| AE | Status | Evidence |
|---|---|---|
| AE1 — campus reads at the reference bar beside `docs/references/` | **PASS (owner-affirmed)** | U9 flagship checkpoint affirmed ("Affirm + fold polish in"; folded items delivered in U11/U13). Final composite: `SubmissionBundle/screenshots/campus.png` beside `docs/references/` captures. |
| AE2 — 2P drag accept/reject with host validation | **PARTIAL — automated half PASS, manual half pending** | Host-authority seam suite (accept/reject/attempt lifecycle/locks) green in PlayMode 155/155. Pending manual rows: (a) two-client reject delivery, (b) live shared placement rendering, (f) partner held-piece glow — see matrix below. |
| AE3 — cinematic reveal ≤12s, Skip ≥3s, locked branch | **PASS (automated + screenshots)** | Reveal cinematic + latch tests green; `reveal-unlocked.png` (staged in-world unlock) and `reveal-locked.png` (locked slots, no Skip). Cross-client latch/skip independence is manual row (d). |
| AE4 — zero fallback art in optional rooms/badges | **PASS** | Extended fallback gate green; `music.png`, `ai-lab.png`, `robotics.png`, `kitchen.png`, `gallery.png` show curated art only. |
| AE5 — Fredoka/Lexend everywhere, no LegacyRuntime/Arial | **PASS** | Zero-legacy-`Text` scan green; visible in all 12 captures. |
| AE6 — distinct audio cues; understandable with audio off | **PASS (coverage) / by-ear pass pending** | AudioCueCoverage green over 29 cue IDs; silent no-op behavior tested. Clip mappings are name/semantics-curated — a listening pass may swap individual clips. |
| AE7 — always launchable, full loop playable | **PARTIAL — automated PASS, manual same-computer rows pending** | EditMode 73/73 + PlayMode 155/155 traverse avatar → campus → rooms → gallery → reveal; packaged exe launches the same loop. The human two-client walkthrough is rows (a)–(f) below. |

## Manual 2P matrix — ready to execute (owner action)

Standing QA debt (same-computer host/client testing is required per
`docs/qa/README.md`). Setup: launch `CareerQuestCampus.exe` twice on one
computer → instance 1 **Host Game**, instance 2 **Join This PC** (P1: WASD +
F, P2: IJKL + Enter). Record PASS/FAIL per row.

| # | Row | Steps | Expected |
|---|---|---|---|
| a | Two-client reject delivery | Both enter Design Build. A places a piece; B drags the same piece (or a wrong piece to a filled lot) and drops. | B's piece snaps back with gentle feedback copy + reject cue on B only; A's view unaffected. |
| b | Shared placement rendering | A places pieces while B watches. B then tries to pick up an accepted piece. | B renders A's placements live; accepted pieces are not draggable by B. |
| c | Re-entry attempt reset | Complete a room attempt together; both exit through ceremony; one client re-enters the room. | Fresh attempt: slots render empty, drops accepted (network-state reset via RPC); a client entering mid-attempt joins the in-progress state instead of wiping it. |
| d | Reveal latch/skip independence | Earn 3 badges; both clients navigate to the reveal route. A skips at ~3.5s; B watches to the end. Variant: B stays in a room while A reveals. | Each client starts at its own latch; A's skip does not corrupt B's sequence; B-in-room is unaffected and gets a normal local sequence later. |
| e | Emote delivery | A fires the emote button; spam it past the rate limit. | Emote bubble renders above A's avatar on both clients; excess spam dropped gently. |
| f | Partner held-piece glow | A picks up and holds a piece while B watches; A drops or gets rejected. | B sees the soft highlight on the piece A holds; highlight clears on drop/reject. |

## Known issues (minor)

1. **Optional-room action buttons low contrast** — the bottom action buttons
   (e.g. Robotics "Build Robot", Music "Record Beat", shared "Complete
   Quest") render pale against the cream instruction-strip band; visible in
   all four optional-room captures. Cosmetic; targets remain clickable.
2. **Audio mappings unheard** — clips curated by filename/semantics, by-ear
   pass pending (may swap mappings, no code change expected).
3. Campus footer copy says "press Enter to start a quest" while the HUD hint
   says "Enter doors: E" — E, Space, and Enter all work; copy is merely
   inconsistent between the two surfaces.

## Demo Notes

- Smoothest evaluator path (~1 minute): avatar select → walk campus past the
  guide beat → Design Build drag session → Escape menu peek → (with a seeded
  or completed session) reveal cinematic.
- Reveal copy stays strength-based and exploratory; Practice stamps, never
  failure copy.
- Privacy invariants held end-to-end: no accounts/chat/telemetry/child data;
  emotes are fixed IDs; settings are device PlayerPrefs only; 2P is
  same-computer/LAN host-client.
