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

## Visual Rules

- Keep the campus colorful, but avoid one-color themes. Use green ground, warm paths, blue science spaces, coral creative spaces, gold logic/court spaces, and teal health/helping spaces.
- Treat buttons as entrances or controls, not as the whole world.
- Put character presence on every major screen where the player is supposed to feel "in" the campus.
- Preserve the selected avatar color in campus and activity scenes so the choice feels meaningful.
- Make the first screen prove the premise immediately: campus, students, career buildings, and a clear Play/Showcase choice.
- Keep copy short. The visuals should carry the idea before the text explains it.
