# Career Quest Campus Art Direction

## Creative Target

Career Quest Campus should read as a playable pop-up campus: bright 2.5D spaces, polished 2D game characters, clear career buildings, and celebratory feedback when players complete activities. The visual promise is not realism. It is a kid-friendly world where every building quietly explains what kind of future path it represents.

## Real Character Bar

Main-path avatars and NPCs must look like actual video game characters, not procedural block people. A finished character sprite needs:

- A readable head, face, hair or headwear, torso, arms, and legs.
- Outfit details or accessories that give the character personality.
- A clear silhouette at gameplay scale and preview-card scale.
- Transparent background, consistent proportions, and no baked-in text.
- A pose that can support idle bob, walking/movement, and room presence.

Procedural fallback characters are QA placeholders only. They keep the game from breaking, but they do not satisfy visual completion for avatar selection, hub play, activity rooms, Gallery, or Reveal.

## First Visual Layer

The first pass used procedural Unity scene sprites so the game had characters and environment without waiting on imported art packs:

- Campus paths, plaza, lawns, clouds, and themed buildings.
- Flat student characters for player presence and Showcase beats.
- Avatar selection with four readable character silhouettes and color identities.
- A Future City build table with skyline lots.
- Themed backdrops for Health Hero Clinic, Logic Court, Gallery, and Career Reveal.
- A runtime sprite fallback for the network player avatar so it is visible even when the prefab has no assigned sprite.
- Translucent UI panels so menus behave like a HUD over the game world instead of hiding it.

This layer is now below the target bar. Future work should preserve fallback safety but replace player-facing procedural figures, buildings, rooms, props, badges, and icons with generated or curated sprites.

## Next Art Upgrade

Replace the procedural shapes gradually with a reusable sprite kit:

- Four student avatar variants with shared body proportions and color-swappable shirts.
- Four student avatar variants that pass the Real Character Bar above.
- NPC variants for campus guide, builder partner, clinic patient, and logic judge.
- One building sprite per activity: Design Build Studio, Health Hero Clinic, Logic Court, AI Lab, and future campus doors.
- Environment props: signs, benches, badge banners, trees, lamps, path tiles, and activity tables.
- Three animation cues: avatar idle bob, accepted-placement pulse, and reveal-stage light sweep.

## Sprite Kit Pipeline

Generated and curated sprites should enter the project through `AssetCatalog` IDs instead of direct scene paths. The first playable catalog covers:

- `avatar.*` and `npc.*` sprites for selected player identity, campus guide, activity partners, patient, and court judge.
- `campus.*` sprites for the three playable buildings, gallery, reveal stage, and future-label buildings.
- `room.*` and `prop.*` sprites for each activity room and its interactable objects.
- `badge.*` sprites for activity completion and reveal readiness.
- `ui.*` sprites for persistent controls such as exit, gallery, reveal lock/unlock, confirm, and back.

Source prompt for generated batches:

```text
Kid-friendly 2D campus adventure game sprite kit, crisp readable silhouettes, transparent backgrounds for characters and props, warm daylight colors, playful career-themed school buildings, simple shape language, no text baked into art, consistent proportions across avatars, icons, badges, and classroom props.
```

Until final sprites are imported, `SpriteFallbackFactory` generates deliberate placeholder sprites from the same catalog colors. Missing IDs use a high-contrast checker fallback so broken art is visible during QA.

Player-facing milestones must list any fallback sprites still visible in screenshots. If a screenshot uses fallback art for the selected avatar, guide/NPC, primary building, room background, primary prop, badge, or core UI icon, that milestone is not visually complete.

## Generated Sprite Kit Pass - 2026-06-09

The first repeatable sprite kit is generated in-project by `Assets/_CareerQuest/Editor/CareerQuestSpriteKitGenerator.cs`.

- Runtime sprites live under `Assets/Resources/CareerQuest/<Category>/<asset-id>.png` so `AssetCatalog` can load them by stable ID.
- Review copies live under `Assets/_CareerQuest/Art/...` using the same IDs.
- The current kit covers all first-playable avatar, NPC, campus, room, prop, badge, and UI IDs in `AssetCatalog`.
- Characters include head, hair, face, torso, arms, legs, outfit/accent details, and transparent backgrounds.
- This pass is acceptable as a first generated game-art baseline, but it is not a final illustrator polish pass. Future art can replace the PNGs without changing gameplay code as long as IDs stay stable.

## Imported-Assets-First Pass - 2026-06-11

The Wow Quality Pass reverses the generator-first pipeline. Decision log: see
DESIGN.md 2026-06-11 entry.

- **Primary art source:** Kenney CC0 packs, curated under
  `Assets/_CareerQuest/Art/Kenney/<Pack>/` (review/curation location). Only
  curated, catalog-ID-named copies move to `Assets/Resources/CareerQuest/`
  as later units land — the `AssetCatalog` stable-ID pipeline is unchanged.
- **Fonts:** Fredoka (display) + Lexend (body) static TTFs under
  `Assets/Fonts/` with their OFL.txt licenses; baked into TMP SDF assets in
  the typography unit.
- **Buildings:** upgraded owned art styled to the Kenney palette (Kenney has
  no 2D cartoon town buildings). Direction sample:
  `docs/references/building-direction-sample.png`.
- **Import settings:** `CareerQuestTexturePostprocessor` enforces
  Sprite/Single, 100 PPU, bilinear, no mipmaps, uncompressed for everything
  under the CareerQuest art paths — no manual inspector edits.
- **Generator status:** `CareerQuestSpriteKitGenerator` and
  `SpriteFallbackFactory` remain as the QA fallback layer only. Fallback art
  visible in player-facing screenshots still fails a milestone.
- **Quality bar:** `docs/references/` is the standing side-by-side review
  anchor (Toca Boca World, Khan Academy Kids, Skillsville, Kenney previews).

## Final State — Wow Quality Pass Landed - 2026-06-12

The imported-assets-first pipeline is fully landed; every player-facing
surface ships curated art, verified by the extended zero-fallback gate over
the full catalog (EditMode suite). Where each art surface stands:

- **Characters (avatars + NPCs):** Kenney Toon Characters with code-driven
  frame animation (walk/idle, `flipX` facing) — the generated character art
  is retired from the player-facing path.
- **Campus hub:** authored prefab diorama (`CampusHub.prefab`) — parallax
  bands, Kenney props/foliage, ambient motion (clouds, flag, butterflies),
  interactive toys; upgraded owned buildings styled to the Kenney palette
  per the affirmed `docs/references/building-direction-sample.png`.
- **Activity rooms:** authored room prefabs (Design Build workshop, Health
  Hero clinic, Logic Court) with drag interaction; the four optional rooms
  carry the at-bar art pass with simpler interactions by design.
- **Badges and gallery:** all badge IDs (core + ai_lab, music_studio,
  robotics_garage, community_kitchen) exist as real art; the gallery is a
  passport/sticker book.
- **Reveal:** in-world stage prefab with faked 2D lighting (glow/gradient
  sprites) and a scripted-camera cinematic.
- **Typography:** TextMeshPro everywhere — Fredoka display, Lexend body
  (SDF assets baked from static TTFs); the zero-legacy-`Text` scan enforces
  this in the suite.
- **Audio:** 29 cue IDs (UI, gameplay, ambient/music tiers, hub toys) all
  resolve to curated Kenney clips; coverage gate enforced in EditMode.
- **Cursor/emotes:** Kenney Cursor Pack (grab state on drag) and Emotes
  pack (fixed-ID 2P emotes).
- **Generator demoted, enforced in code:** `CareerQuestSpriteKitGenerator`
  is fallback-only — it fills missing files and never overwrites curated
  PNGs (0 overwrites confirmed on the final ladder run). The checker
  fallback remains the visible-breakage QA layer.

Final evidence: `SubmissionBundle/screenshots/` (12 captures, 1280x720,
packaged build) and `docs/qa/2026-06-12-wow-pass-final.md`.

## Visual Rules

- Keep the campus colorful, but avoid one-color themes. Use green ground, warm paths, blue science spaces, coral creative spaces, gold logic/court spaces, and teal health/helping spaces.
- Treat buttons as entrances or controls, not as the whole world.
- Put character presence on every major screen where the player is supposed to feel "in" the campus.
- Preserve the selected avatar color in campus and activity scenes so the choice feels meaningful.
- Make the first screen prove the premise immediately: campus, students, career buildings, and a clear Play/Showcase choice.
- Keep copy short. The visuals should carry the idea before the text explains it.
