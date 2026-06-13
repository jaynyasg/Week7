# QA / Proof Pack — Party Campus Pack (U1–U11)

Source plan: `docs/plans/2026-06-12-party-campus-pack-implementation-plan.md`
Locked design: `docs/designs/party-campus-pack.md`

## Build

- Merge commit: `5c7dae0` on `main` ("Merge feat/party-campus-pack: 10-station
  party campus (U1-U11)"), pushed to `origin` (Gauntlet Labs + GitHub mirror).
- Unit commits underneath the merge: `2d640e0` U1 (station definition spine),
  `4cd3d84` U2 (generic station-id routing + walk-into-door auto-entry),
  `ca6424f` U3 (ToyInteractionKit, 7 host-validated patterns), `8499f72` U4
  (PartyStationController + Robotics Rescue result-spine proof), `c45d30c` U5
  (first six stations; OptionalRoomController retired), `507172f` U6 (session
  reward events, derived accessories + avatar layer, passport, 2P read model),
  `68d6ab3`/`c440a77` U7 (reveal synthesis, combo resolver, career families),
  `de5b276` U8 (ten-station district campus, evolution from session),
  `e24679c` U9 (PartyRun cadence, classroom access, facilitator controls),
  `1cab9d4` U10 (Wave 2 pack — all 10 stations playable), `5372a11` U11
  (final accessory art + fit, avatar polish, demo routes).
- Unity version: `6000.4.10f1` · Build target: StandaloneWindows64
- Build path: `Builds/Windows/CareerQuestCampus.exe` (rebuilt from the merged
  `main` so captures reflect the 10-station map, not the pre-merge campus).

## Verification ladder (executed 2026-06-13, batchmode `-runTests`)

| Suite | Result | Duration |
|---|---|---|
| EditMode | **237 passed / 0 failed / 0 skipped** | 4.7 s |
| PlayMode | **229 passed / 0 failed / 0 skipped** | 122.9 s |
| **Total** | **466 passed / 0 failed** | — |

Re-run (single Unity instance; results XML is the authoritative signal — the
launcher's exit-0 fires early because Unity relaunches itself headless):

```powershell
$u="C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Unity.exe"
$p="C:\Users\jaynyasg\OneDrive\Documents\GitLab\Week7"
& $u -batchmode -nographics -projectPath $p -runTests -testPlatform EditMode -testResults editmode.xml -logFile editmode.log
& $u -batchmode -nographics -projectPath $p -runTests -testPlatform PlayMode -testResults playmode.xml -logFile playmode.log
```

## Proof-pack coverage (plan R20) — how each item is evidenced

R20 enumerates the proof pack: 90-second demo route, 3-minute impressive proof
route, all-10 station smoke, 2P shared-progress evidence, classroom access
smoke, route teardown/replay churn smoke, and accessory fit screenshots. Most
are machine-verified inside the green PlayMode suite above; the table maps each
to its evidence.

| Proof-pack item | Evidence | Status |
|---|---|---|
| All-10 station smoke through generic station-id routing | `StationPackSmokePlayModeTests` (iterates every station, completes default seed in quick mode, returns to campus, re-enters for replay) | **PASS (automated)** |
| First-six station pack playable | `FirstSixStationPackPlayModeTests` | **PASS (automated)** |
| Wave 2 station pack playable | `Wave2StationPackPlayModeTests` | **PASS (automated)** |
| Robotics Rescue deep proof (route→toy→hint→result→reward→accessory→evolution→gallery→reveal→replay→teardown) | `PartyStationRoboticsPlayModeTests`, `PartyStationControllerTests` | **PASS (automated)** |
| 2P shared-progress / host-validated submission + targeted reject | `PartyStationNetworkStatePlayModeTests`; legacy seams `DesignBuild/HealthHero/LogicCourt NetworkSeam` | **PASS (automated)** |
| Classroom access (quiet/reduced-motion, pointer-first, non-color cues, facilitator controls) | `ClassroomAccessPlayModeTests`, `DemoDebugOverlayTests`, `PauseMenuPlayModeTests` | **PASS (automated)** |
| Route teardown / replay churn (no leaked roots, drags, highlights, coroutines, subscriptions) | `StationLifecycleChurnPlayModeTests` | **PASS (automated)** |
| Accessory fit (follows transform, sorts, flips, slot/clutter rules, no float/clip) | `AvatarAccessoryLayerPlayModeTests`, `AccessoryResolverTests` | **PASS (automated)** + screenshots below |
| 30-path reveal synthesis + completion-count styles (3/5/8/10) + combos | `RevealSynthesisTests`, `CareerComboResolverTests`, `CareerConfigTests`, `ShowcaseRevealFlowTests`, `RevealCinematicPlayModeTests` | **PASS (automated)** |
| PartyRun guided cadence (resume across routes, quit preserves earned state) | `PartyRunPresenterPlayModeTests` | **PASS (automated)** |
| Copy safety (early-reader, strength-based, pretend-play-safe, no deterministic career phrases) | `StationCopySafetyTests`, `PartyStationDefinitionTests` | **PASS (automated)** |
| Auto-entry dwell / cooldown / return-grace / non-overlap radii | `AutoEntryPlayModeTests`, `WorldAnchorsTests`, `SceneFlowRouterTests`, `HubDestinationTests` | **PASS (automated)** |
| Campus evolution from session results | `CampusEvolutionPlayModeTests`, `CampusHubWorldPlayModeTests` | **PASS (automated)** |

## Acceptance examples (plan AE1–AE8)

| AE | Status | Evidence |
|---|---|---|
| AE1 — walk-into-Robotics dwell entry → toy → one `MiniGameResult` → tool belt → campus | **PASS (automated)** | `AutoEntryPlayModeTests` + `PartyStationRoboticsPlayModeTests` |
| AE2 — replay AI Lab Sort alternate seed preserves station id/badge/accessory/unique count | **PASS (automated)** | `FirstSixStationPackPlayModeTests`, `PartyStationDefinitionTests` |
| AE3 — 2P wrong-then-right submit; only submitter gets reject bounce; both see accepted shared progress | **PASS (automated)** | `PartyStationNetworkStatePlayModeTests` |
| AE4 — 5 unique completions → richer reveal style with traits/paths/family/superpower/accessories/combo, no deterministic language | **PASS (automated)** | `RevealSynthesisTests`, `ShowcaseRevealFlowTests`, `StationCopySafetyTests` |
| AE5 — guided Party Run resumes after gallery/campus; free-choice entry still works | **PASS (automated)** | `PartyRunPresenterPlayModeTests` |
| AE6 — quiet/reduced-motion keeps completion clear while reducing particles/camera/audio | **PASS (automated)** | `ClassroomAccessPlayModeTests` |
| AE7 — final build: all 10 stations playable via station-id routing, none "coming soon"/construction-only | **PASS (automated)** | `StationPackSmokePlayModeTests` |
| AE8 — proof/debug artifacts omit names, rosters, free text, analytics, persisted child data | **PASS (automated + by-design)** | `DemoDebugOverlayTests`; KTD12 session-only/local-only posture |

## Demo routes

- **~90-second demo path** (evaluator quick loop): avatar select → walk into the
  nearest station entrance (dwell auto-entry, no key press) → complete the toy →
  see reward/accessory spotlight → repeat 2–3 stations → open reveal.
- **~3-minute impressive route**: drive enough unique completions to cross a
  richer reveal style (5+), showing accessory accumulation, campus evolution
  beats, a combo card, and the strength-based ceremony.
- Guided cadence is owned by `PartyRunPresenter` (round intro, reward preview,
  progress strip, accessory spotlight, evolution beat, continue/quit, reveal
  handoff) and is **presentation-only** — it never forces normal free-choice
  station order (verified by `PartyRunPresenterPlayModeTests`).

## Screenshot evidence

Captured from the rebuilt player via the in-build harness
`-cq-visual-state <state> -cq-screenshot <path>` at 1280×720
(`docs/qa/evidence-party-pack/`). Capture confirms the build is current — the
campus shot shows the merged 10-station map, not the pre-merge campus.

| Capture | State | Observed |
|---|---|---|
| `campus.png` | Campus hub (10 stations) | The merged district map: Tech Lane (AI Lab, Robotics, Spaceport), Quest Yard core (Design Build, Logic Court, Health Hero), Story Street (Newsroom, Music Studio, Game Studio), plus Care Corner, Kitchen, Green City. Robot-guide first-run bubble, 0/3 badge HUD, "Move: WASD" hint, footer "Walk into a career door to start a quest. It opens on its own!" (U2 auto-entry). |
| `avatar.png` | Hero selection | Four Kenney Toon heroes (Sky Builder selected, Care Captain, Logic Spark, Art Inventor) with career-color stripes, large preview card, Back / Enter Campus. |
| `robotics.png` | Robotics Rescue (party station) | Guide *Bolt the Bench Buddy* ("upbeat build coach"), intro "A lunchbox robot lost its parts! Rebuild it and pick a rescue route.", **Tool Belt** reward preview, DragToSlot lots + task tray (Battery Toast, Wheel Sandwich, Sensor Sticker, Route Cards, Rescue Flag), NPC reaction line. |
| `ai-lab.png` | AI Lab Sort (party station) | Guide *Pixel the Pattern Pal*, intro "Teach the bubblegum sorter by putting each example in its matching bin.", **Lab Goggles** reward, SortToBin bins (Reasoning, Creativity, Science), example tray. |
| `music.png` | Music Remix (party station) | Guide *DJ Tempo*, intro "Layer the storm sounds into a parade beat and keep the tempo steady.", **Microphone** reward, Mix Spot + Tempo Dial (compose/meter), sample tray. |
| `kitchen.png` | Community Kitchen Match (party station) | Guide *Chef Sunny*, intro "Solve the soup clues and serve a bowl every guest can enjoy.", **Chef Hat** reward, Match Tray + serving bowl (PickMatchingTrio + serve), ingredient tray. |
| `vet.png` | Vet Clinic Diagnose (party station) | Guide *Nurse Nova* ("calm care guide"), intro "Read the care clues and pick a gentle plan for the hiccuping dragon.", **Care Cape** reward, MatchAndCare (Gentle Care Tool / Care Spot), tray (Care Clue Cards, Water Bowl, Comfort Blanket, Cozy Temp Sticker, Gentle Care Tool), pretend-play-safe NPC line. |
| `game-studio.png` | Game Studio Compose (party station) | Guide *Captain Loop* ("playful design lead"), intro "Pick a goal, an obstacle, and a rule that fit, then run the playtest.", **Sketchbook** reward, Mix Spot (compose), tray (Hero Token, Obstacle Tile, Rule Card, Power-Up Sketch, Playtest Button). |
| `weather.png` | Weather Lab Rescue (party station) | Guide *Radar Rae* ("alert safety planner"), intro "Order the forecast clues, then set up shelter before the parade starts.", **Weather Goggles** reward, Next Step (sequence), tray (Forecast Tiles, Umbrella Sign, Route Cones, Calm Radio, Shelter Flag), safe weather/emergency copy. |
| `spaceport.png` | Spaceport Pilot (party station) | Guide *Commander Orbit* ("focused mission guide"), intro "Sequence launch, orbit, delivery, and landing to fly the snack probe.", **Mission Patch** reward, Next Step (SequenceCards), tray (Launch Checklist, Fuel Bead, Snack Crate, Orbit Arrow, Landing Pad). |
| `newsroom.png` | Newsroom Story Sprint (party station) | Guide *Scoop Rivera* ("fact-checking reporter"), intro "Match the checked facts to who, what, and where, then stamp the headline.", **Press Badge** reward, Mix Spot (compose/match), tray (Who Card, What Photo, Where Map, Quote Recorder, Fact-Check Stamp), source-safe copy. |
| `green-city.png` | Green City Builder (party station) | Guide *Grid Green* ("practical systems planner"), intro "Place four city pieces while keeping both meters happy and green.", **Green Hardhat** reward, **BalanceMeters** — Budget Meter + Happy Meter tap-dials with needles + Build Spot, four-piece tray (Solar Tile, Garden Block, Bike Path, Water Wheel). |
| `design-build.png` | Design Build (core room) | "Future City Workshop" — drag each city piece onto its matching lot, 0/5 placed, five pastel slot pads, city-piece tray, builder NPC. |
| `health.png` | Health Hero (core room) | "Health Hero Clinic" — bring symptom clipboard to the patient first, 0/3 care steps, patient on exam bed, clipboard/thermometer/care-plan tools. |
| `logic.png` | Logic Court (core room) | "Logic Court" — review the case file on the podium then sort each evidence card, 0/3 sorted, two sort zones, evidence tray, judge NPC. |
| `gallery.png` | Quest Passport | Seven locked sticker slots, five Career DNA chips (Building, Collaboration, Communication, Creativity, Focus — all +0 fresh session), "Reveal unlock: 0/3 unique quest badges", Reveal (Locked) / Passport / Campus. |
| `reveal-locked.png` | Reveal locked branch | "Career Reveal Stage", three dark "?" slots, "0/3 quest badges collected" bar, "Complete 3 unique quest badges… 3 games to go", **no Skip control** — matches the locked-branch contract. |
| `reveal-unlocked.png` | Reveal resolved | "REVEAL UNLOCKED! **Future Maker** — Future Tech + Design + Build strengths · You might like: AI Engineer · Architect · Lawyer · Robotics Engineer · City Planner · Very strong match · Hybrid spark: Courtroom Inventor · 'a strength clue from your quest badges — not a life assignment.'" — `RevealSynthesis` family + superpower + paths + hybrid/combo + non-deterministic copy (U7). |

All 10 party stations now have a dedicated `-cq-visual-state` case and a captured
screenshot, each showing full station identity (guide + intro + reward preview +
toy pattern + task tray + NPC reaction). The seven supported toy patterns are all
represented across the set: DragToSlot (robotics), SortToBin (ai-lab),
ComposeSet/meter (music, game-studio, newsroom), PickMatchingTrio (kitchen),
MatchAndCare (vet), SequenceCards (weather, spaceport), and BalanceMeters
(green-city).

### Accessory fit

Per-slot accessory fit (anchor, sorting, facing-flip, no float/clip, one-visible-
per-slot, ceremony gating) is rigorously machine-verified by
`AvatarAccessoryLayerPlayModeTests` + `AccessoryResolverTests`. The runtime
accessory layer binds to the campus player avatar via
`PlayerAvatarController` → `AvatarRuntimeView.BindAccessories(session)`, and the
`accessory-fit` QA visual-state seeds the full earned set onto it for manual /
zoomed inspection. A *static* 1280×720 frame is not included: accessories render
only on the runtime `AvatarRuntimeView` (campus avatar / station-end spotlight /
passport / reveal), and at campus scale individual accessories are too small to
read in a still — a legible large-avatar still would need a dedicated preview
surface, which is out of this pass.

## Known issues (minor, non-blocking)

1. ~~**Guide speech-band overlaps tray labels**~~ — **RESOLVED** (this pass): the
   party-station guide was reworked from a wide bottom bar (whose live line/
   reaction text reached screen-center) into a compact bottom-left card with the
   line + reaction stacked left-aligned, so the whole card stays left of the
   centered tray-label row. Verified in the refreshed `robotics`/`ai-lab`/`music`/
   `kitchen` captures (tray labels fully visible) and across all six new station
   captures. `StationGuideView` text/lifecycle tests remain green.

## Privacy posture (KTD12)

No accounts, profiles, chat, matchmaking, telemetry, analytics, export, or
persisted child histories. Reward events, Party Run state, and the 2P read
model are session-only and presentation-only; best `MiniGameResult`s remain the
single scoring source of truth. Settings are device-local PlayerPrefs; 2P is
same-computer/LAN host-client. Debug/proof output surfaces flags and counts
only — never child-identifying data.
