# Monster Builder Pack (CC0, Kenney)

Source: https://kenney.nl/assets/monster-builder-pack - Creative Commons CC0 1.0.
Full license in `License.txt`. Only the `Parts/` used by the station scene
subjects are vendored here (the full pack is modular: body + face + detail
parts you assemble).

## Why it is here

Design-review (2026-06-16): the party-station "scene subjects" (the dragon,
critter, blob the seed copy names) were drawn from flat `CampusWorldSprites`
primitives and read as low-quality next to the curated Kenney avatars. They now
use real Kenney art, routed through `AssetCatalog` like the room NPCs.

## Composite recipe -> Assets/Resources/CareerQuest/Npc/npc.subject_*.png

Each subject is a flattened overlay (body, then details, then eyes, then mouth),
centered, exported as one PNG. Built offline (System.Drawing):

- npc.subject_dragon  = body_redB + two detail_red_horn_large (right mirrored) + two eye_cute_light + mouth_closed_happy
- npc.subject_critter = body_blueA + two detail_blue_ear_round (right mirrored) + two eye_cute_light + mouth_closed_happy
- npc.subject_blob    = body_greenB + two eye_cute_light + mouth_closed_happy
- npc.subject_cloud   = BackgroundElements/Flat/cloud3 + two eye_cute_light + mouth_closed_happy
- npc.subject_robot   = ToonCharacters/Robot/character_robot_idle (direct)
- npc.subject_person  = ToonCharacters/FemalePerson/character_femalePerson_idle (direct)
