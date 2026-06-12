---
date: 2026-06-12
topic: party-campus-pack
---

# Party Campus Pack Requirements

## Summary

Expand Career Quest Campus into a fast, creative, classroom-safe "Mario Party for careers" slice. The first wave adds six quick toy challenges that reuse the existing room/result/reveal spine while making the campus feel much bigger: players drag, sort, match, build, remix, diagnose, rescue, and compose; each challenge awards a badge, Career DNA, and a visible avatar accessory; the reveal later turns those inputs into a personality ceremony.

The priority order is locked:

1. Mini-game party campus.
2. Avatar accessory rewards.
3. More buildings, games, and career end paths.
4. Personality reveal ceremony.

## Design Rules

- Each station must be playable in 30-60 seconds.
- Each station uses 3 primary interactable objects, not a long inventory.
- Every result emits normal `MiniGameResult` data: activity id, display name, tier, source, time/accuracy, summary, and trait deltas.
- Every station unlocks one accessory or wearable identity item.
- The reveal remains strength-based: "you practiced..." and "you might like..." instead of "you should become..."
- The first pass should prefer four existing optional rooms plus two new stations over building a giant new map.

## First-Wave Station Table

| Station | Verb | Kid Prompt | 3 Interactable Objects | Success Condition | Accessory Reward | Trait Deltas | Career Paths Influenced |
|---|---|---|---|---|---|---|---|
| Robotics Rescue | Build + rescue | Build a helper robot, then send it to rescue the stuck rover. | Robot body, battery pack, rescue beacon | Assemble the robot with power + beacon, then choose the safe rescue route. | Tool belt or robot gloves | Building +5, Reasoning +4, Collaboration +3 | Robotics Engineer, AI Engineer, Inventor, Mechanic |
| Music Remix | Remix + compose | Layer a beat that matches the festival mood. | Drum loop, melody card, mood light | Pick three matching layers for the target mood and press Record. | Microphone or headphones | Creativity +5, Communication +4, Focus +3 | Musician, Sound Designer, Artist, Teacher |
| Community Kitchen Match | Match + serve | Match fresh ingredients to what each neighbor needs. | Ingredient basket, neighbor order cards, serving tray | Serve three correct trays, including one allergy/need clue. | Chef hat or apron | Helping +5, Collaboration +4, Creativity +3 | Chef, Food Scientist, Community Organizer, Doctor |
| AI Lab Sort | Sort + test | Sort training examples so the space probe learns what to look for. | Training cards, model console, test probe | Sort examples into correct groups, then launch a successful probe test. | Lab goggles | Reasoning +5, Science +4, Building +3 | AI Engineer, Data Scientist, Scientist, Game Designer |
| Vet Clinic Diagnose | Diagnose + care | Figure out what the pet needs and choose the gentle care tool. | Symptom card, care tool, comfort toy | Match symptom to care tool, then give the comfort item. | Care cape or stethoscope pin | Helping +5, Science +4, Communication +3 | Veterinarian, Doctor, Counselor, Biologist |
| Game Studio Compose | Compose + pitch | Build a tiny game idea from a hero, a rule, and a goal. | Hero card, rule tile, goal token | Choose a matching trio and stamp the game pitch. | Sketchbook or creator cape | Creativity +5, Reasoning +3, Communication +3 | Game Designer, Animator, Entrepreneur, Story Producer |

## Career Path Expansion

Add enough paths for the reveal to feel meaningfully bigger. First expansion target: 12 total career paths.

Existing paths:

- Doctor
- Lawyer
- AI Engineer
- Artist
- Architect

New first-wave paths:

- Robotics Engineer
- Chef or Food Scientist
- Musician or Sound Designer
- Veterinarian
- Game Designer
- Teacher
- Entrepreneur

Stretch paths after the first wave:

- Environmental Scientist
- Pilot or Aerospace Designer
- Counselor
- Journalist or Story Producer
- Animator

## Accessory Reward Set

First pass accessories should be simple layered sprites or badge-like overlays. They do not need a full dress-up editor yet.

| Accessory | Unlock Source | Identity Signal |
|---|---|---|
| Tool belt | Robotics Rescue | Builder/problem solver |
| Microphone | Music Remix | Communicator/performer |
| Chef hat | Community Kitchen Match | Helper/creator |
| Lab goggles | AI Lab Sort | Scientist/future tech |
| Care cape | Vet Clinic Diagnose | Helper/caretaker |
| Sketchbook | Game Studio Compose | Creator/designer |
| Badge sash | Earn any 3 unique stations | Quest progress |
| Star robe | Reach reveal-ready state | Ceremony moment |

## Personality Ceremony Inputs

The reveal should read the same core data but present it with more personality:

- Top 3 Career DNA traits.
- Top 3 to 5 career paths.
- Career family label.
- Career superpower label.
- Hybrid identity when badge combos support it.
- Avatar spotlight with earned accessories visible.

Example superpowers:

- Creative Builder: Creativity + Building.
- Care Captain: Helping + Communication.
- Future Maker: Science + Reasoning + Building.
- Community Spark: Helping + Collaboration.
- Story Inventor: Creativity + Communication.

Example hybrids:

- Robot Chef: Robotics Rescue + Community Kitchen Match.
- Music Doctor: Music Remix + Health/Vet care path.
- Space Architect: AI Lab Sort + Design Build.
- Courtroom Inventor: Logic Court + Robotics Rescue.
- Game Studio Doctor: Game Studio Compose + Health/Vet care path.

## Build Order

1. Convert existing optional rooms from simple step buttons into toy challenges where possible: Robotics, Music, Kitchen, AI Lab.
2. Add two new lightweight station entries: Vet Clinic and Game Studio.
3. Add accessory unlock data tied to best results.
4. Expand career definitions to 12 paths with clear trait weights.
5. Upgrade gallery/reveal copy to mention accessories, superpower, career family, and hybrid identity.
6. Capture a new 90-second demo route: Campus -> Robotics -> Music -> Kitchen -> AI Lab -> Reveal.

## Acceptance Criteria

- A player can complete at least five short stations in under five minutes.
- At least six toy verbs are represented across the first wave.
- Every first-wave station has one visible accessory reward.
- The avatar can display at least three earned accessories by reveal time.
- Career reveal includes a superpower, career family, top paths, and hybrid identity.
- Existing three core rooms still count toward reveal and keep their current behavior.
- Optional-room results still count toward reveal eligibility.
- No new flow asks for accounts, chat, profiles, or persisted child data.

## Out Of Scope For First Wave

- Full dress-up editor.
- Fully animated wardrobe system for every body type.
- More than two brand-new buildings.
- Procedural career text generation.
- Online matchmaking, chat, or shared child profiles.
- Separate Netcode-loaded scenes for each mini-game.
