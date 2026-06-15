---
title: "Built content can be invisible to players: audit the three data-to-display seams"
date: 2026-06-15
category: design-patterns
module: "career-quest-campus / party-stations"
problem_type: design_pattern
component: development_workflow
severity: high
applies_when:
  - "A code-assembled (procedural) scene where data-layer state and player-facing display are separate seams, so passing EditMode/PlayMode tests do not prove the player can see or correctly read the content."
tags:
  - unity
  - procedural-scene
  - data-display-seam
  - visual-verification
  - headless-screenshot
  - test-false-confidence
  - placeholder-fallback
related_components:
  - testing_framework
  - tooling
---

# Built content can be invisible to players: audit the three data-to-display seams

## Context

In a code-assembled (procedural) game scene, the **data layer** (what's playable, routable, scored) and the **display layer** (what the player actually sees in the hub, on the HUD, on the playfield) are separate seams stitched together at build/runtime — there is no hand-authored Unity scene where "what you see is what's wired." Career Quest Campus composes its entire campus from `CareerQuestHubPrefabBuilder.ComposeHub` plus runtime renderers, so content can be fully built and pass every logic test yet be **invisible or misleading to the player**.

This work found three concrete data→display gaps where the logic was green but the presentation lied:

1. **Spawn / visual seam** — six fully-playable stations (Spaceport, Weather Lab, Newsroom, Vet Clinic, Game Studio, Green City) rendered as "Soon" construction-site scaffolds (`AddStationSite`) because no `campus.{id}.png` building art existed. The new-verb showcase stations (Spaceport/Weather = trace, Newsroom = deduce) were exactly the ones masked as unbuilt, so kids never walked into them.
2. **Copy seam** — the `PartyStation` instruction-strip branch emitted a generic `"Play in the {building} to finish your quest."` for *every* station, so trace / shoot / deduce all read identically; the new verbs were invisible at the HUD level.
3. **Art-catalog seam** — `PartyStationRenderer.ResolveToySprite` returns a placeholder `CampusWorldSprites.Circle` whenever a sprite key is not cataloged as final art (`AssetCatalog.SpriteResolution.IsFinalArt`), so an un-cataloged toy silently degrades to a placeholder circle on the playfield.

The reason these stay hidden: EditMode/PlayMode tests assert **logic**, not the rendered hub. `NoInPlanStationRemainsConstructionOnly` (`StationPackSmokePlayModeTests.cs`) asserts every in-plan station id is in `PlayableStationIds` — it checks the *catalog*, never the hub prefab's building art. So the suite stayed 239/239 + 233/233 green while the player saw "Soon" scaffolds, generic copy, and placeholder-circle toys.

## Guidance

When you get a "the new thing isn't showing" report in a code-assembled scene, treat it as **three independent data→display seams** and check each one separately — passing tests rule out none of them:

1. **SPAWN / visual list** — Is the entity actually in the prefab-builder's spawn list as *final* content, or is it rendered as a placeholder/scaffold? In this repo: is the station built with `AddSmallBuilding(..., "campus.{id}")` or with the placeholder `AddStationSite(...)`? A placeholder spawn still routes, scores, and passes playability tests.
2. **COPY** — Is the player-facing string generic, or does it name the new behavior? Find the one copy seam keyed by id (never a switch-case per entity) and make it surface the distinct verb.
3. **ART key CATALOG** — Is the art key cataloged as final, or falling back to a generated placeholder? `IsFinalArt` is `IsCataloged && Sprite != null && !IsFallbackGenerated && !IsMissingDefinition`. `IsPlaceholderToySprite` / `ResolveToySprite` (`PartyStationRenderer.cs`) will quietly swap a real toy for a `Circle` if the key isn't registered — no error, no test failure.

Diagnosis path to confirm a seam is the *display* layer and not the *data* layer: the data side is reachable via `WorldAnchors` (entrance tables → districts) → `PlayableHubController.EnterEntrance` (dispatch) → the station runs. If you can enter and play the station but still don't *see* the right building / copy / toy, the gap is purely presentational — exactly seams (1)/(2)/(3) above.

**Before/after — copy seam** (`InstructionStrip.cs`, commit `f0492a4`):

```csharp
// before — generic for every station:
return $"Play in the {stationEntry.BuildingName} to finish your quest.";

// after — per-verb cue keyed by the station's ToyPatternId, still station-aware & < 80 chars:
return PartyStationDefinitions.TryGetById(stationId, out var stationDef)
    ? $"{VerbCue(stationDef.Pattern)} in the {stationEntry.BuildingName}!"
    : $"Play in the {stationEntry.BuildingName} to finish your quest.";   // fallback kept

private static string VerbCue(ToyPatternId pattern) => pattern switch
{
    ToyPatternId.TracePath    => "Trace the route",
    ToyPatternId.ShootTarget  => "Pull back and launch",
    ToyPatternId.DeduceAnswer => "Cross out the wrong ones",
    // …
    _ => "Play"
};
```

**Before/after — spawn seam** (`CareerQuestHubPrefabBuilder.ComposeHub`, commits `02f5317` + `1c1bd20`):

```csharp
// before — six playable stations masked as "Soon" construction sites:
AddStationSite(world, "SpaceportSite",  ScienceBlue, new Vector2(-3.9f, 0.1f));
AddStationSite(world, "WeatherLabSite", ScienceBlue, new Vector2( 0.6f, -2.1f));
AddStationSite(world, "NewsroomSite",   Amber,       new Vector2( 5f,    0.8f));
// …+ VetClinic, GameStudio, GreenCity

// after — real campus buildings (art keys match the new BuildingSpec + AssetCatalog entries):
AddSmallBuilding(world, "Spaceport",  "campus.spaceport",   new Vector2(-3.9f, 0.1f));
AddSmallBuilding(world, "WeatherLab", "campus.weather_lab", new Vector2( 0.6f, -2.1f));
AddSmallBuilding(world, "Newsroom",   "campus.newsroom",    new Vector2( 5f,    0.8f));
// …+ VetClinic, GameStudio, GreenCity
```

The art seam is closed by registering the keys in the `Buildings` `BuildingSpec[]` table (e.g. `new("campus.spaceport", ScienceBlue, AccentKind.Dome, 320, 288)`) so `IsFinalArt` becomes true and the placeholder fallback no longer fires; the prefab and `campus.{id}.png` are then regenerated.

## Why This Matters

Logic-green hides presentation gaps. Tests like `NoInPlanStationRemainsConstructionOnly` are deliberately data-layer assertions (catalog playability), so a 100%-green suite gives false confidence that the *player experience* is complete. The cruelest part: the stations the user cared most about — the **new-verb showcase stations** (trace, deduce) — were precisely the ones masked as "Soon," because new content is exactly the content most likely to be missing its building art, its verb-specific copy, and its cataloged toy sprite. The work was done; the player just couldn't tell.

## When to Apply

- When adding stations / verbs / art to a procedurally-assembled scene (anything composed by `ComposeHub` / a prefab builder rather than hand-placed in an editor scene).
- When a user reports "I don't see the new X" / "the new station looks unbuilt" / "they all read the same" **despite passing tests** — go straight to the three-seam checklist; do not re-run the logic suite expecting it to reproduce.
- Any time new player-facing content lands behind an `IsFinalArt` / placeholder-fallback path, a per-id copy seam, or a placeholder spawn marker.

## Examples

1. **Verb-named instruction strip** — `Assets/_CareerQuest/Scripts/UI/InstructionStrip.cs` (the `PartyStation` branch + new `VerbCue`), commit `f0492a4`. Generic "Play in the {building}" → per-verb cue keyed by `ToyPatternId`, fallback preserved, stays under the 80-char `MaxGuideLineLength` and copy-safe so `StationRouteInstructionCopyIsStationAwareAndSafe` stays green.
2. **Un-masked station buildings** — `Assets/_CareerQuest/Editor/CareerQuestHubPrefabBuilder.cs` (`Buildings` spec table, `ComposeHub` station block), commits `02f5317` + `1c1bd20`. Six `AddStationSite(...Site)` "Soon" scaffolds → six `AddSmallBuilding(..., "campus.{id}")` real buildings + matching `BuildingSpec` art entries; 16 building PNGs regenerated; EditMode 239/239 + PlayMode 233/233 still green.
3. **De-doubled door labels** — `Assets/_CareerQuest/Editor/CareerQuestHubPrefabBuilder.cs` (`AddMainBuilding`), commits `aee5808` + `ff067e3`. `AddMainBuilding` baked a `DoorSign` *and* the runtime added a door label at the entry circle, so the three core names (Design Build, Health Hero, Logic Court) rendered twice; dropped the baked sign so all 13 doors read with exactly one name. No test asserted the baked sign — another presentation-only gap invisible to the suite.

## How to Verify (prevention)

Because no test guards the rendered hub, the only way these fixes were *confirmed* — and the only durable guard against the same class of gap — is to drive the built Windows player headless and diff before/after screenshots:

```
CareerQuestCampus.exe -cq-visual-state <state> -cq-screenshot <path> -screen-width 1280 -screen-height 720
```

- (auto memory) Capture from the **MAIN checkout**, not a worktree — the main checkout has a warm graphics Library; a cold worktree Library on OneDrive never converges and the capture hangs/blanks. This is how `ff067e3` was verified ("Design Build / Logic Court now render once").
- (session history) The `-cq-visual-state` cases are added **per station** (`CareerQuestApp.ShowVisualQaState`); the six originally-missing station states (vet_clinic, game_studio, weather_lab, spaceport, newsroom, green_city) were added in the 2026-06-13 design-review session. A new station needs its own visual-state case before it can be visually confirmed.
- (session history) The task-toy clarity treatment (chain-role toys haloed at full opacity, optional/reaction toys faded ~45% with no halo) is a renderer-only change in `PartyStationRenderer.DecoratePlayfield` from that same session — it keys off **role**, so it survives once a toy gains real art (the art-catalog seam fix must preserve this).
- (auto memory) PlayMode runs mutate the TMP fallback SDF asset (`LiberationSans SDF - Fallback.asset`) — **revert it before staging** so the commit isn't polluted with noisy binary diffs.
- (auto memory) Unity batchmode is single-instance — trust the results XML (`-testResults`), **not** the launcher exit code.
- (session history) The PlayMode `AutoEntryPlayModeTests.WalkIntoStationEntranceAutoEntersAfterDwell...` dwell test is a known pre-existing cross-test singleton-pollution flake (suspected `CampusSessionState.Instance` / `ClassroomAccessSettings` leaking from an earlier PlayMode test). It passes in isolation and in the combined EditMode+PlayMode ordering — gate on the **combined** green run, don't chase it as a regression.

Treat the before/after screenshot as the acceptance artifact for any change to a procedurally-assembled scene's presentation layer, since the green test suite explicitly will not catch it.

## Related Context

- (session history) When parallel sessions both implement the same player-facing feature (here: two independent toy-art passes — procedural shapes vs. Kenney sprites), the push that lands second is rejected non-fast-forward. Resolve by `reset --hard origin/main` then cherry-picking only the **non-conflicting** commits (the campus/HUD work) on top, dropping the superseded implementation — never force-push over a teammate's merged work. The dropped work stays recoverable on its local branch.
