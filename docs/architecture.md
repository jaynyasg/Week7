# Architecture Snapshot

Implementation-facing snapshot of how Career Quest Campus is built. The "why"
behind these choices lives in [technical-decisions.md](technical-decisions.md).

## Technical Defaults

- Unity version: `6000.4.10f1`.
- Networking: Unity Netcode for GameObjects, host-authoritative.
- Transport: Unity Transport.
- Distribution target: itch.io with a downloadable Windows build (see [DEPLOY.md](../DEPLOY.md)). WebGL is preview-only.
- Visual style: Future Workshop Diorama + Junior Quest UX — bright 2.5D campus, polished 2D characters, clean quest HUD, strong feedback moments (see [DESIGN.md](../DESIGN.md)).

## Privacy Boundaries

No accounts, saved profiles, persistent child data, analytics, telemetry, or
chat. Optional display names are session-only. The network replicates indexes
and values, never drag positions, free text, or names. Showcase seeded/live
source can appear in debug/QA evidence, but the child-facing Gallery and Reveal
stay celebratory.

## Scene Model

P0 uses one persistent gameplay scene. Mini-games and stations are full
activities, but they run as states/rooms inside the persistent scene rather than
separate Unity scenes. Separate Netcode-managed scenes are deferred until after
the P0 loop ships (`TODOS.md`).

```text
EntryScreen
   |
   +-- Play       -> AvatarSelection -> free campus (solo or host/join)
   +-- Multiplayer/Testing -> ConnectionScreen -> host/join/solo-fallback
   +-- Showcase   -> friendly disclaimer -> PresenterController -> guided route
   |
   v
Gameplay Scene
   +-- NetworkBootstrap
   +-- CampusSessionState        (host-authoritative session/route/progress)
   +-- GameSession               (best MiniGameResult per room, Career DNA, reveal readiness)
   +-- Campus                    (PlayerAvatar, CampusGuide, building entrances)
   +-- Activity rooms            (3 bespoke mini-games + 10 party stations)
   +-- MiniGameResultHandler / AchievementGalleryUi / RevealCeremony / RecoveryUi / DemoDebugOverlay
```

## Rooms: 13 Total

Two kinds of room share one drop seam and one result contract:

### Three original bespoke mini-games

Distinct controllers under `Assets/_CareerQuest/Scripts/Activities/`, each with
its own room/network state and a dedicated `ActivityRoute` value:

| Room | Catalog id | Controller | Mechanic |
|---|---|---|---|
| Future City Design Build | `design_build` | `DesignBuildController` | collaborative spatial placement (the protected 2P shared-placement moment) |
| Health Hero Clinic | `health_hero` | `HealthHeroController` | timed diagnosis + tool/treatment matching |
| Logic Court | `logic_court` | `LogicCourtController` | evidence sorting + strongest-argument pick |

### Ten definition-driven Party Pack stations

All defined in `Config/PartyStationDefinitions.cs`, all mounting the single
`PartyStationController`. Each has one interaction verb (`ToyPatternId`):

| Station | Station id | Verb (`ToyPatternId`) |
|---|---|---|
| Robotics Rescue | `robotics_garage` | `ShootTarget` |
| AI Lab Sort | `ai_lab` | `DeduceAnswer` |
| Community Kitchen Match | `community_kitchen` | `PickMatchingTrio` |
| Music Remix | `music_studio` | `ComposeSet` (+ meter dial) |
| Vet Clinic Diagnose | `vet_clinic` | `MatchAndCare` |
| Game Studio Compose | `game_studio` | `ComposeSet` |
| Weather Lab Rescue | `weather_lab` | `TracePath` |
| Spaceport Pilot | `spaceport` | `TracePath` |
| Newsroom Story Sprint | `newsroom` | `DeduceAnswer` |
| Green City Builder | `green_city` | `BalanceMeters` |

Of the ten, four (`robotics_garage`, `ai_lab`, `music_studio`,
`community_kitchen`) are converted former optional rooms that keep legacy
`ActivityRoute` values; six are net-new and route by station-id string through
the single `ActivityRoute.PartyStation` branch (`UsesStationIdRouting`). Station
ids are declared in `CareerQuestCatalog.PartyStationIds`.

## Party-Station Framework (definition-driven)

One framework drives all ten stations from static data — no per-station code:

- **`PartyStationDefinition` / `PartyStationDefinitions`** — identity + content:
  id, display name, verb tags, `ToyPatternId`, guide, prompt, object set, success
  rule, `TraitDeltas`, `AccessoryRewardId`, career tags, `BadgeArtKey`,
  `CampusArtKey`, `EvolutionPropAssetId`, and exactly two `Seeds` (one default,
  one alternate). `PartyStationValidator` enforces content invariants.
- **`ToyPatternRules` / `ToyPatternId`** (`Interaction/`) — the pure, scene-free
  rule core. Verbs: `DragToSlot`, `SortToBin`, `PickMatchingTrio`, `SequenceCards`,
  `ComposeSet`, `MatchAndCare`, `BalanceMeters`, `TracePath`, `ShootTarget`,
  `DeduceAnswer` (the first three are engine-supported but unused by current
  stations). Builds the golden order + drop targets per pattern, validates one
  `ToyAction` via `Submit()`, tracks accepted set + meter values, exposes
  `Complete` / `NextExpectedObjectId` / `BuildGoldenActionSequence()`.
  `ForSeed(definition, seed)` is the canonical constructor — one instance per
  resolved seed, reused across solo, host, and tests.
- **`ToyInteractionKit`** (`Interaction/`) — the drag/drop seam: one `DropZone`
  per target, one draggable piece per non-meter toy; owns teardown, hint pulse,
  accepted-piece lockdown, and partner-hold rendering.
- **`PartyStationRenderer`** (`Activities/PartyStations/`) — visuals: accent
  color, layout, placeholder-token fallback, set dressing, and pattern-specific
  surfaces over the same seam (`MountTraceRoute`, `MountLaunchRange`,
  `MountDeduceBoard`, `MountMeterWidgets`).
- **`PartyStationController`** — lifecycle: seed selection (default first play,
  choice on replay), 3s intro beat, guide + reward preview, playfield mount, hint
  ladder, the serve/pitch confirmation beat, 2P glue, and exactly one
  `MiniGameResult` + one `StationRewardEvent` per completion.

## The Drop Seam & Host Authority

Every action funnels through `PartyStationController.TrySubmitDrop(pieceId,
targetId, value)` → `DropSubmitResult` (`Accepted`, `Pending`,
`RejectedWrongSlot`, `RejectedOccupied`, `RejectedLocked`, `RejectedUnknownPiece`).

```text
Client input
   -> local preview
   -> ServerRpc (SubmitActionRpc, SendTo.Server)
   -> host ApplySubmission(): runs ToyPatternRules.Submit(), validates
   -> host updates authoritative NetworkList/values, sets Complete
   -> every peer re-renders from OnListChanged/OnValueChanged (SyncFromNetwork)
```

- **Solo** runs `ToyPatternRules.Submit()` directly. **Multiplayer** routes the
  same call to the host, which runs the same `Submit()`. Clients never complete
  optimistically — `TrySubmitDrop` returns `Pending` until host-accepted state
  replicates.
- Rejects use a sender-targeted reply RPC echoing the client's `submissionId`
  (stale-reject guard).
- `StationProgressNetworkState` (host-authoritative `NetworkBehaviour`) rides the
  always-spawned `CampusSessionState` NetworkObject. It replicates station/seed
  index, accepted-object indexes, meter values, hint/highlight, completion,
  held-piece presence flags, and compact reward facts — never per-frame drag
  positions, never names.
- The same authority holds for the bespoke mini-games (Design Build's shared
  placement is the original instance of this model). If two players place the
  same piece at once, the host grants the first and gently rejects the second.

## Input Modes

```text
Solo/Fallback        -> normal keyboard + mouse
Same-computer 2P     -> P1 split keyboard preset, P2 split keyboard preset
LAN multiplayer      -> each machine normal keyboard + mouse (manual IP join)
```

Preset controls are enough for Week7; full remapping is out of scope. Solo
Fallback is available from the start and clearly labeled; debug/passport metadata
labels its source as `Solo Fallback`. The required LAN path is manual IP join
plus optional local-IP display; if LAN is untested it is marked experimental in
the README and QA docs.

## Mini-Game Result Contract

Every room returns the same `MiniGameResult`: activity id, completion tier
(`Degree` | `Practice`), source (`Multiplayer` | `Solo` | `Solo Fallback`), trait
deltas, time remaining, accuracy, summary text. Rooms do not emit career weights —
`CareerConfig` computes career ranking from traits.

## Progression: Career DNA, Badges, Accessories, Reveal

- **Traits (10):** Helping, Science, Focus, Reasoning, Communication, Leadership,
  Creativity, Building, Spatial Thinking, Collaboration. `CareerDnaProfile` sums
  trait deltas from best results.
- **Careers (30)** across **6 families** (Care & Community, Future Tech, Design &
  Build, Story & Stage, Nature & Space, Justice & Leadership), each with a path-
  support tier (`StationBacked` / `RevealSupported` / `FuturePackReady`). Ranking
  is a weighted trait dot-product; co-leads are returned for ties within 5 points.
- **Badges:** one per room — 3 core + 10 station (`badge.{id}`).
- **Accessories (14):** 10 station accessories (one per station, across Head/Face/
  Torso/Back/Hand/Sash slots) + 4 milestone accessories at 3 / 5 / 8 / 10 unique
  completions. Visual/story only — never affect scoring.
- **Campus evolution:** one skyline city piece per earned badge
  (`CampusEvolutionController`); first appearance per session gets a pop + sparkle
  + camera nudge.
- **Reveal:** unlocks at 3 unique completed rooms (`GameSession.RevealReady`).
  Confidence is phrase-only (`Good` / `Strong` / `Very strong match`) based on the
  number and tier of unique best results, not raw percentages. Close ties show as
  co-leads. Copy is strength-based, never a life assignment.

## Replay & Scoring

Replay is allowed before and after reveal; only each room's best result counts
(tier → time remaining → accuracy). `GameSession` recomputes Career DNA from best
results, preventing trait inflation from replay farming.

## Art / Content Policy

`Art/AssetCatalog.cs` defines every asset; party-toy defs are generated from
`PartyStationDefinitions` so the catalog can't drift from seed data. Toy keys
follow `prop.party.{stationId}.{objectId}`; badge = `BadgeArtKey`; building =
`campus.{station}`; evolution = `prop.city_piece_*`.
`PartyStationRenderer.IsPlaceholderToySprite()` returns true until a key has final
art; placeholders render as a tinted handmade token (never the magenta missing-
checker). Toy/badge/building/evolution defs are intentionally `required: false`
until their art pass; a player-facing fallback gate (asserted in
`AssetValidationTests` / `PartyToyArtTests`) flags keys that still need final art.

## Build & Test

- **Build:** `CareerQuest.Editor.CareerQuestBuild.BuildWindowsPlayer` (menu
  `Career Quest ▸ Build Windows Player`) → `Builds/Windows/CareerQuestCampus.exe`.
  See [DEPLOY.md](../DEPLOY.md).
- **Tests:** two assemblies — EditMode (~241 tests: pure rules/config/result
  surface) and PlayMode (~233 tests: station lifecycle, network/2P matrix, reveal
  cinematic, station packs). Last green gate: EditMode 241/241 + PlayMode 233/233.
- **Screenshot harness:** `CareerQuestApp` parses `-cq-visual-state <state>
  [-cq-screenshot <path>]` — shows a QA state, waits, captures, and quits. A
  separate `-cq-smoke -cq-mode <solo|host|client>` runs the netcode smoke.

## Debug Overlay

Hidden by default; toggled by a small debug button + keyboard shortcut. Shows
compact demo facts: host/client/solo, connection status, player count, current
activity, timer, last accepted action, last result id, top three traits, and
attempt/debug source metadata.

## Repo Layout

Tracked: `Assets/`, `Packages/`, `ProjectSettings/`, `docs/`, `README.md`.
Ignored: `Library/`, `Temp/`, `Obj/`, `Build/`, `Builds/`, logs, user-local Unity
files.
