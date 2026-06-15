# Career Quest Campus — Submission Bundle

Career Quest Campus is a kid-friendly career-exploration game built in Unity
6000.4.10f1. You pick a quest hero, walk a hand-made toy campus, and enter
career rooms behind each door. The three core rooms (Design Build, Health
Hero, Logic Court) are played by dragging real objects — city pieces onto
blueprint lots, care tools to a patient, evidence cards to sorting zones.
Four optional rooms (AI Lab, Music Studio, Robotics, Kitchen) offer simpler
quests. Every completed quest stamps a sticker badge into the Quest Passport;
three unique badges unlock the Career Reveal — an in-world stage cinematic
that presents a strength-based career clue ("a strength clue from your quest
badges — not a life assignment").

## How to run

**Packaged build (recommended):**

1. Run `Builds/Windows/CareerQuestCampus.exe` (product name "Career Quest
   Campus", custom icon and Campus Sky splash).
2. From the title screen choose Play, pick a hero, and Enter Campus.

**From the Unity project:**

1. Open the repo in Unity `6000.4.10f1`.
2. Open `Assets/_CareerQuest/Scenes/CareerQuestCampus.unity` and press Play.
   The scene bootstraps the whole game; no other setup is needed.

## Controls

| Input | Action |
|---|---|
| WASD or Arrow keys | Walk the campus |
| E / Space / Enter | Enter a career door (stand near it first) |
| Mouse drag | Pick up and drop pieces/tools/cards inside rooms; click hub toys |
| Escape | Pause menu (resume, SFX/music volume, fullscreen, exit to title) |
| BackQuote (`) | Debug overlay toggle |

In two-player split-keyboard sessions the host uses **P1: WASD + F** and the
joining player uses **P2: IJKL + Enter**.

## Two players (same computer / LAN)

Multiplayer is host-client via the connection screen (reachable from the
title flow):

1. Launch a first instance and choose **Host Game** ("Start a local session").
2. Launch a second instance on the same computer and choose **Join This PC**
   ("Connect to a host on this computer").
3. On a LAN, the second machine can instead use **Join IP** and type the
   host's address (defaults to `127.0.0.1`).

Dragging is local; every drop is validated by the host, so both players see
the same accepted placements. Rejected drops snap back with gentle feedback
on the submitting player only. A one-button emote (fixed IDs, no text) lets
players wave at each other.

## Screenshots (`screenshots/`, 1280x720, captured from the packaged build)

| File | What it shows |
|---|---|
| `avatar.png` | "Choose Your Quest Hero" — four Kenney Toon hero passport cards (Sky Builder selected, Care Captain, Logic Spark, Art Inventor) with career-color stripes and a large passport preview card over the campus backdrop. |
| `campus.png` | Campus hub diorama — three core career buildings with colored roofs and door signs, the optional-room row (AI Lab, Music Studio, Robotics, Kitchen), the robot guide greeting the player with the first-run speech bubble, and the 0/3 badge HUD. |
| `design-build.png` | Future City Workshop — blueprint table with five pastel slot pads, the draggable city-piece tray (0/5 placed), player avatar and builder NPC, drag instruction strip. |
| `health.png` | Health Hero Clinic — patient on the exam bed with a thermometer, care-tool tray (symptom clipboard, kit, bandage, care plan), 0/3 care steps. |
| `logic.png` | Logic Court — judge bench with gavel, case-file podium, two sorting zones, evidence-card tray (0/3 sorted), judge NPC presence. |
| `music.png` | Music Studio (optional) — lavender studio with bunting, keyboard and speaker, player beside the robot NPC, beat/chorus quest. |
| `ai-lab.png` | AI Space Lab (optional) — blue lab with the model console and porthole windows, train-the-model quest. |
| `robotics.png` | Robotics Garage (optional) — teal garage with helper-robot parts laid out on the workbench, build-and-power quest. |
| `kitchen.png` | Community Kitchen (optional) — green kitchen with a steaming pot and ingredient shelf, prep-and-serve quest. |
| `gallery.png` | Quest Passport — spiral-bound passport book with seven sticker-badge slots (locked "?" state shown), five Career DNA chips, and the 0/3 reveal-unlock line. |
| `reveal-locked.png` | Career Reveal Stage, locked branch — stage building behind the card, three dark "?" slots, progress bar with "games to go" guidance, no Skip control. |
| `reveal-unlocked.png` | Reveal resolved — gold/blue stage light beams, three badge tokens landed in glowing slots, "REVEAL UNLOCKED! AI Engineer + Architect — Very strong match" with the strength-clue copy. |

No procedural fallback art is visible in any capture; the extended
zero-fallback gate over the full player-facing catalog runs in the test
suite.

## Videos (`videos/`, 1280x720 MP4)

| File | What it proves |
|---|---|
| `architecture-single-player-loop.mp4` | 29-second walkthrough of the documented Unity architecture and single-player loop: avatar, campus, core room play, gallery progression, and reveal. |
| `multiplayer-progression-proof.mp4` | 23.5-second proof-pack video for two-player networking plus progression, including fresh host/client smoke evidence and the shared-room/passport/reveal path. |

## Verification status

- Final proof ladder: EditMode tests 237/237 green; PlayMode tests 229/229
  green (`docs/qa/2026-06-13-party-campus-pack-proof.md`).
- Packaged Windows build green from the same ladder run.
- Early submission re-check (2026-06-14 local): packaged two-process
  host/client smoke completed in 34.66 seconds, host exit 0, client exit 0,
  host 3/3 and client 6/6 scenarios green, with no `CQ_2P_WAIT_TIMEOUT` lines.
- Gates inside those suites: extended zero-fallback over the full
  player-facing catalog, audio cue coverage (29 cue IDs), zero legacy
  uGUI `Text` scan (all type is TextMeshPro Fredoka/Lexend).

## Limitations

- **Human two-player feel pass still useful:** the host-authority behavior
  (accept/reject, shared placement rendering, attempt reset, reveal latch,
  emotes, partner held-piece glow) is now covered by the automated
  two-process matrix and the fresh early-submission smoke. A live human pass
  remains worthwhile for subjective feel, not for requirements coverage.
- **Audio by-ear pass pending:** all 29 cue IDs resolve to real clips chosen
  by name/semantics; a listening pass may swap individual mappings. The game
  is fully understandable with audio off.
- **Optional rooms are simpler by design:** at-bar art, but button-step
  quests rather than drag-and-drop (scope decision, not a gap).
- **Video capture style:** the two MP4s are proof-pack videos assembled from
  packaged-build captures and fresh smoke-log evidence, not raw camera footage
  of a live screen-recording session.
- The splash screen keeps Unity Personal branding (license requirement).

## Privacy

- No accounts, profiles, chat, analytics, telemetry, or persistent child
  data — nothing leaves the machine.
- Two-player is same-computer or LAN host-client only; no internet
  matchmaking or discovery.
- Emotes are fixed picture IDs, never free text.
- The only stored settings are device-local PlayerPrefs (volume,
  fullscreen).

## Licenses

- **Art, audio, cursors, emotes:** Kenney packs, CC0 1.0 Universal (no
  attribution required). License copies ship in-repo per pack under
  `Assets/_CareerQuest/Art/Kenney/<Pack>/License.txt`.
- **Fonts:** Fredoka (display) and Lexend (body), SIL Open Font License 1.1
  — `Assets/Fonts/Fredoka/OFL.txt`, `Assets/Fonts/Lexend/OFL.txt`.
- Campus building art is owned, made for this project, styled to the Kenney
  palette.
