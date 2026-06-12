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
- Same-computer host/client tested: **YES — automated two-process matrix
  PASS 2026-06-12** (client 6/6, host 3/3; see matrix results below)
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
| AE2 — 2P drag accept/reject with host validation | **PASS** | Host-authority seam suite green in PlayMode, PLUS the automated two-process matrix (2026-06-12): rows (a) wire reject delivery with submission-id echo, (b) live shared placement rendering, (f) partner held-piece glow all PASS on real host+client processes — see matrix results below. |
| AE3 — cinematic reveal ≤12s, Skip ≥3s, locked branch | **PASS (automated + screenshots)** | Reveal cinematic + latch tests green; `reveal-unlocked.png` (staged in-world unlock) and `reveal-locked.png` (locked slots, no Skip). Cross-client latch/skip independence is manual row (d). |
| AE4 — zero fallback art in optional rooms/badges | **PASS** | Extended fallback gate green; `music.png`, `ai-lab.png`, `robotics.png`, `kitchen.png`, `gallery.png` show curated art only. |
| AE5 — Fredoka/Lexend everywhere, no LegacyRuntime/Arial | **PASS** | Zero-legacy-`Text` scan green; visible in all 12 captures. |
| AE6 — distinct audio cues; understandable with audio off | **PASS (coverage) / by-ear pass pending** | AudioCueCoverage green over 29 cue IDs; silent no-op behavior tested. Clip mappings are name/semantics-curated — a listening pass may swap individual clips. |
| AE7 — always launchable, full loop playable | **PASS** | EditMode 73/73 + PlayMode 156/156 traverse avatar → campus → rooms → gallery → reveal; packaged exe launches the same loop; the two-process matrix drove a real host+client through connect → room → drag → ceremony → reveal end-to-end (rows a–f below, all PASS). |

## 2P matrix — EXECUTED (automated two-process run, 2026-06-12)

The standing same-computer host/client QA debt (`docs/qa/README.md`) is now
covered by an automated harness: `TwoPlayerMatrixSmoke` drives two built
player processes over localhost through all six rows with state-based
synchronization (no fixed-sleep choreography; cross-process signals ride the
emote relay). Re-run anytime:

```powershell
& .\Builds\Windows\CareerQuestCampus.exe -cq-smoke -cq-mode 2p-host -logFile .\Builds\logs\2p-host.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720
# ~4s later:
& .\Builds\Windows\CareerQuestCampus.exe -cq-smoke -cq-mode 2p-client -logFile .\Builds\logs\2p-client.log -screen-fullscreen 0 -screen-width 1280 -screen-height 720
```

Exit code 0 on both = all rows pass; structured lines `CQ_2P_RESULT
scenario=<id> pass=<bool>` in each log. First run results (client 6/6,
host 3/3, exit 0/0; logs at `Builds/logs/2p-{host,client}.log`):

| # | Row | Result | Evidence detail (from the run) |
|---|---|---|---|
| a | Two-client reject delivery | **PASS** | dup=RejectedOccupied, rejectEvent=True, wireReject=True (host reject RPC delivered to the submitting client with submission-id echo), dragFree=True, count=1 |
| b | Shared placement rendering | **PASS** | accepted=True, dragBlocked=True, zoneOccupied=True, pieceLocked=True on the client for the host's placement |
| c | Re-entry attempt reset | **PASS** | complete=True, ceremonyPhase=True, reset=True (AttemptNumber bump + slots cleared over RPC), resubmit=Pending → accepted=True |
| d | Reveal latch/skip independence | **PASS both sides** | client: latch opened via fallback while host unannounced, skip armed, continuity held through host's later announce, resolved. host: announced, skipped at own pace, resolved — client uncorrupted |
| e | Emote delivery | **PASS both sides** | bubble rendered above the sender's avatar on the sender's AND the partner's screen |
| f | Partner held-piece glow | **PASS both sides** | host saw the glow on the client-held piece and its clear; client confirmed its held entry synced and cleared |

Note: the harness asserts state seams (render flags, events, network values),
not pixels — a human playthrough remains worthwhile for feel, but the wire
contracts of AE2/AE7 are machine-verified.

## Known issues (minor)

1. ~~Optional-room action buttons low contrast~~ — **FIXED 2026-06-12**: root
   cause was placement under the instruction strip's translucent band, not
   color; buttons moved above the band (y −238) in optional AND core rooms,
   chrome tests updated to assert the above-strip contract, verified in a
   fresh Robotics capture.
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
