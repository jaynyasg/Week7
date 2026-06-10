# Design System - Career Quest Campus

## Product Context

- **What this is:** Career Quest Campus is a kid-friendly Unity career exploration game where players choose an avatar, walk through a campus, complete career-themed mini-games, earn badges, and unlock a career reveal.
- **Who it is for:** Elementary and middle-grade learners, plus evaluators/teachers who need to quickly understand that the game is safe, playful, and learning-oriented.
- **Space:** Educational games, career exploration, kid-safe creative play.
- **Project type:** Unity 2D/2.5D game with a playable hub, activity rooms, multiplayer/testing support, and a guided showcase mode.
- **Core promise:** Kids do not just read about jobs. They walk into career worlds, use tools, help characters, solve problems, and collect proof of what they tried.

## Aesthetic Direction

- **Direction:** Future Workshop Diorama + Junior Quest UX.
- **Mood:** A handmade toy campus that feels built from paper, cardboard, stickers, classroom supplies, and bright career props. The UX should feel like a light quest game: map, doors, tools, badges, progress, and celebration.
- **Decoration level:** Expressive, but disciplined. Use visual richness in the world and activity rooms; keep HUD elements clean and legible.
- **Key metaphor:** Careers are places you can enter. Skills are tools you can use. Badges are souvenirs from what you practiced.
- **Research basis:** Khan Academy Kids uses character-led joyful learning; PBS KIDS emphasizes safe, familiar, character-led games; Toca Boca World succeeds through open-ended colorful locations and simple controls; Minecraft Education frames learning through building and agency; Skillsville connects careers to a virtual city, skills, and badges.

## Visual Principles

1. **World first, UI second.** The screen should read as a playable place before it reads as a menu.
2. **Every screen needs character presence.** If the player is making a choice, the selected avatar or a guide/NPC should be visible.
3. **Rooms teach visually.** Health, logic, and design activities should be recognizable from props before text explains them.
4. **Buttons should feel like controls, doors, cards, or signs.** Avoid generic floating rectangles as the main visual language.
5. **One instruction at a time.** Kids need direct, specific prompts and immediate feedback.
6. **Badges are progress, not decoration.** Badge states must be visible in the HUD, gallery, and reveal gate.
7. **No oversized world text.** Environmental labels are small signs only. Player instructions belong in HUD cards or speech bubbles.
8. **Fallback art is not final art.** Procedural sprites can keep QA playable, but player-facing avatars, NPCs, buildings, rooms, props, badges, and icons are not visually complete until generated or curated sprites replace fallbacks.

## Character Bar

Main-path avatars and NPCs must read as real 2D game characters:

- Distinct head, face, hair or headwear, body, arms, and legs.
- Outfit detail or accessory that gives role/personality.
- Clean silhouette at gameplay scale and preview-card scale.
- Transparent background, consistent proportions, no baked-in text.
- Friendly pose that supports idle, walking, and room-presence animation.

Blocky placeholder figures are acceptable only as visible QA fallbacks.

## Color

### Approach

Expressive career zones with a stable warm-paper UI foundation. Do not let the game become a single teal/blue/green palette.

### Palette

- **Ink:** `#19323C` - primary text, outlines, important icon strokes.
- **Paper:** `#FFF7E0` - HUD cards, quest cards, speech bubbles.
- **Paper Shadow:** `#D9B66F` - card depth, sticker undersides, badge stamp shadows.
- **Campus Sky:** `#9DE2FF` - background sky.
- **Campus Grass:** `#8BD17C` - ground and soft campus surfaces.
- **Path Gold:** `#F3C45B` - paths, quest highlights, revealed progress.
- **Workshop Teal:** `#0E6B6F` - primary UI control color.
- **Creative Coral:** `#F76C5E` - Design Build identity.
- **Health Mint:** `#58C894` - Health Hero identity.
- **Logic Amber:** `#F2A33B` - Logic Court identity.
- **Science Blue:** `#4A9DEB` - future science/AI spaces.
- **Music Lilac:** `#9E85DC` - future creative/music spaces.
- **Success:** `#31A66A`
- **Warning:** `#E39B2E`
- **Error:** `#D95040`
- **Info:** `#2F7FBF`

### Usage

- Use **Paper** for readable UI surfaces over the game world.
- Use each activity color as a world identity: building, door, room accents, badge, and selected mission card.
- Use **Path Gold** for progression, reveal readiness, and "go here next" cues.
- Use **Ink** for outlines where procedural shapes need more crafted definition.
- Avoid large opaque panels that hide the world unless the player is in a modal decision.

## Typography

Unity can start with built-in fonts, but the target system should import real fonts.

- **Display / titles:** Fredoka or Baloo 2. Use for screen titles, room names, badge ceremonies, and large quest moments.
- **Body / instructions:** Lexend. Use for kid-facing instructions, HUD text, and button labels.
- **Debug / technical:** JetBrains Mono. Use only in debug overlays and QA displays.
- **Scale:**
  - Hero title: 48-56 px
  - Screen title: 36-42 px
  - Room prompt: 24-30 px
  - Button label: 22-28 px
  - HUD/body: 16-20 px
  - Small labels: 12-15 px
- **Rules:**
  - Never use giant world-space text for instructions.
  - Prefer short verbs on buttons: `Enter`, `Use`, `Build`, `Help`, `Sort`, `Reveal`.
  - Keep debug/network terms out of player-facing text.

## Spacing And Shape

- **Base unit:** 8 px.
- **Density:** Comfortable, with large click/touch targets.
- **Minimum primary button size:** 160 x 56 px.
- **Minimum small control size:** 44 x 44 px.
- **HUD card padding:** 16-24 px.
- **Panel radius target:** 8 px for most HUD cards, 12 px for speech bubbles or sticker cards, full radius only for circular badges/icons.
- **World object proportions:** Chunky silhouettes, strong color separation, visible outlines or shadows.
- **Do not nest cards inside cards.** Use bands, HUD strips, speech bubbles, or single cards.

## Layout

### Entry

- First screen should show the campus as the main signal.
- Primary actions should be simple: `Play`, `Showcase`, and a small secondary `Multiplayer / Testing` if needed.
- The normal player path should be `Play -> Avatar -> Campus`.

### Avatar Selection

- Treat avatar choice like picking a quest character, not filling a settings form.
- Show the selected avatar large, standing on a small platform or passport card.
- Avatar cards should include silhouette, name, role vibe, and a clear selected state.
- Confirm button copy should be `Enter Campus` or `Start Quest`, not generic `Start`.

### Campus Hub

- Campus is the main game board.
- Doors/buildings should be visibly clickable and reachable by movement.
- Use a compact top HUD for player badge progress and selected avatar.
- Use a bottom quest bar only when helpful; avoid covering the world.
- Future/unavailable buildings should look like locked construction sites or "coming soon" tents, not equal priority to playable rooms.

### Connection / Multiplayer

- Multiplayer is a secondary/testing flow, not the default first-run player path.
- Keep labels player-friendly:
  - `Play Solo`
  - `Host Game`
  - `Join This PC`
  - `Join By IP`
- Put host/IP details in an advanced panel.

### Activity Rooms

Activity rooms should be diorama playsets with props, NPCs, and a clear action area.

- **Design Build Studio:** Blueprint table, draggable building pieces, skyline preview, craft supplies, builder NPC.
- **Health Hero Clinic:** Patient/NPC, symptom clipboard, care tools, warm table, care plan board.
- **Logic Court:** Judge/guide NPC, evidence cards, sorting zones, podium, conclusion stamp.
- Each room needs:
  - Room title sign.
  - One active instruction.
  - 3-5 interactive props or cards.
  - Immediate feedback for each action.
  - Completion ceremony and badge stamp.

### Gallery And Reveal

- Gallery should look like a sticker/passport book.
- Reveal should feel like a stage ceremony, with badge tokens physically unlocking the path.
- Before three unique games, reveal should show locked badge slots and a clear `2/3 badges` state.
- After three unique games, reveal should animate open.

## Motion

### Approach

Intentional and playful. Motion should communicate state changes and make the world feel alive without making controls harder to read.

### Motion Rules

- **Avatar idle:** 1-2 px or small world-unit bob, slow and gentle.
- **Door hover / focus:** Soft pulse or glow, 600-900 ms loop.
- **Button press:** Quick scale down/up, 80-140 ms.
- **Tool pickup:** Pop + small sparkle, 180-250 ms.
- **Correct action:** Object pulse and badge meter bump, 250-400 ms.
- **Invalid action:** Small shake and friendly hint, 180-250 ms.
- **Completion:** Badge stamp, confetti/sparkles, NPC reaction, 500-900 ms.
- **Reveal:** Stage light sweep, badge tokens travel to slots, unlock burst, 700-1200 ms.

Use motion for comprehension first, delight second.

## UX Rules

1. **Make the next action obvious.** At any moment the player should know where to go or what to click.
2. **Prefer direct manipulation.** Pick up tools, drag cards, move to doors, stamp badges.
3. **Keep text short.** One sentence for setup, one sentence for feedback.
4. **Separate player UI from QA/debug UI.** Debug overlays should be hidden or visually separated.
5. **Use icons plus labels for young players.** Do not rely on icon-only controls for important actions.
6. **Show progress in-world and in-HUD.** The badge meter should match visible badges in the world/gallery.
7. **Never punish exploration.** Wrong choices should teach, not fail hard.
8. **Avoid timers unless a mini-game specifically needs urgency.** The current direction is exploratory, not arcade pressure.

## Component Patterns

### Quest Card

- Paper surface, activity color stripe, icon, one active objective, progress chip.
- Example: `Help the patient feel better. Choose the first care tool.`

### Speech Bubble

- Used for guide/NPC hints.
- One or two lines maximum.
- Includes the speaking character nearby.

### Badge Chip

- Circular or sticker-like.
- States: locked, active, earned, reveal-ready.
- Earned state uses stamp motion and stronger outline.

### Door Sign

- Small world sign near a building entrance.
- Uses icon, short label, and activity color.
- On hover/focus, the sign and door pulse together.

### Tool Card

- Large prop icon or sprite, short label, selected state, feedback state.
- Use for Health Hero tools, Logic Court evidence, Design Build pieces.

## Implementation Priorities

### Phase 1 - Stop Looking Like A Menu

1. Route normal play directly from avatar selection to campus.
2. Move multiplayer connection behind a secondary `Multiplayer / Testing` action.
3. Replace full-screen UI panels with HUD cards, speech bubbles, and quest cards.
4. Add a persistent badge meter to campus and activity HUDs.

### Phase 2 - Make The World A Toy Diorama

1. Add outlines/shadows to procedural world shapes.
2. Add door signs and highlighted entrance zones.
3. Add more props: benches, lamps, banners, construction signs, room tools.
4. Make selected avatar visually larger and more recognizable in play.

### Phase 3 - Build The Sprite Kit

1. Generate/import avatar sprites, NPCs, buildings, room backgrounds, props, badges, and UI icons.
2. Route all art through `AssetCatalog` IDs.
3. Keep procedural fallbacks as QA-visible placeholders only.

### Phase 4 - Make Activities Feel Like Games

1. Convert each activity from button rows to interactable room props/cards.
2. Add NPC reactions and speech bubble feedback.
3. Add completion ceremony and badge stamp per room.
4. Make reveal unlock only after three unique badges, with visible badge slots.

## Decisions Log

| Date | Decision | Rationale |
|------|----------|-----------|
| 2026-06-10 | Adopted Future Workshop Diorama + Junior Quest UX | User chose `3+5` from design consultation. This combines a handmade toy campus with a clear quest loop, giving the game a stronger identity and better player guidance. |
| 2026-06-10 | Use world-first UI rules | The game currently feels menu-led; the new direction makes the campus, rooms, avatar, tools, and badges carry the experience. |
| 2026-06-10 | Make multiplayer secondary to normal play | Connection options are important for QA but confuse first-time players when presented before the game starts. |

## Design References

- Khan Academy Kids: character-led, joyful educational activities.
- PBS KIDS Games: safe, kid-first game framing.
- Toca Boca World: open-ended colorful locations, customization, simple controls.
- Minecraft Education: building, agency, collaboration, and immersive learning.
- Skillsville: career exploration through a video-game city, jobs, skills, and badges.
- Nielsen Norman Group children UX guidance: clear instructions, age-aware interaction, and large/simple targets.
