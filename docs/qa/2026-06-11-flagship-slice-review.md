# QA Report — Flagship Slice Review (Wow Quality Pass U9)

## Build

- Commit: U8 head (`c915ef4`) + InstructionStrip copy fix + generator fallback-only fix
- Unity version: `6000.4.10f1`
- Build target: StandaloneWindows64
- Build path: `Builds/Windows/CareerQuestCampus.exe` (built green this pass)
- Test ladder: sprite-kit fallback gen (0 overwrites, 47 untouched) → EditMode 64/64 → PlayMode 92/92 → Windows build — **all green**

## Evidence set (`docs/qa/evidence-flagship/`, 1280x720, captured from the built player)

| Capture | State | Reads against references? |
|---|---|---|
| `avatar.png` | Avatar selection | Yes — Kenney Toon characters on passport cards, Fredoka display + Lexend body, per-card career color stripe, selected state. Khan-Kids-adjacent. |
| `campus.png` | Campus hub | Yes — authored toy-diorama: parallax sky/hills, Kenney trees/fences, upgraded buildings with activity-color roofs + sign bands, animated avatars, Robot guide speaking the first-run beat ("Hi Sky Builder! Try the Health Hero room first!"), TMP door signs. Same genre as Toca Boca World captures. |
| `design-build.png` | Design Build drag room | Yes — workshop diorama: blueprint table with 5 color slot pads, draggable city-piece tray, player + builder NPC presence, quest HUD card, Practice-tone copy. The drag-tray pattern mirrors the Toca Boca reference directly. |
| `reveal-unlocked.png` | Reveal cinematic (post-resolve) | Yes — in-world stage with gold/blue light beams (P7 faked lighting), three badge tokens landed in glow slots, strength-based result copy ("Very strong match", "not a life assignment"). |
| `reveal-locked.png` | Reveal locked branch | Yes — stage visible behind locked card, 3 "?" slots, 0/3 progress bar, no Skip control. |
| `gallery.png` | Quest Passport | Acceptable for this phase — passport-book layout with locked sticker slots; the sticker/badge art polish is U11 scope. |

Motion/audio (verified by automated suites; clips need a by-ear pass):
walk/idle frame animation, parallax drift, ambient motion (clouds/flag/butterflies),
paper-wipe transitions, drag lift/ghost/snap-back/poof, cinematic beat sequence
(camera tween → token travel → sweep → burst ≤12s, skip ≥3s), 26-cue audio set
with crossfading ambience.

## Fix loop (issues found during this review)

1. **FIXED** — stale Design Build footer copy "Tap Complete when your blueprint is
   ready" (the Complete button was retired in U6; drag auto-completes). Now:
   "Drag each city piece onto its matching lot to finish your blueprint."
2. **FIXED (pipeline)** — `CareerQuestSpriteKitGenerator.Generate` overwrote every
   catalog PNG, which would have clobbered curated art on the next ShipLadder run.
   Now fallback-only: fills missing files, never overwrites.
3. **Noted for owner** (candidate redirects, not yet changed):
   - Campus top HUD is utility-flavored ("Free Campus", "Mode: Play / None",
     dense controls text) — functional but not at the toy bar. U13 (shell) or a
     redirect here could restyle it.
   - Campus props: the large brown tree silhouette (left) reads ambiguous; the
     cactus reads desert rather than campus. Curation swap is cheap.
   - Locked reveal "0/3 quest badges collected" label is low-contrast
     (pale blue on cream).
   - Optional-room row (bottom) crowds the walk band; acceptable, U11 dresses it.

## Same-computer 2P

- Automated: host-authority seam suite (accept/reject/attempt lifecycle/locks),
  reveal latch + announce state tests — green.
- **Manual matrix outstanding (owner action, standing QA debt + U6/U7 downgrades):**
  1. Two clients: B's duplicate-piece submit → B sees snap-back + gentle copy; A unaffected.
  2. B renders A's placements live; B cannot pick up accepted pieces (P22).
  3. Client re-entry after completed attempt resets via RPC (fresh slots).
  4. Both clients on reveal route: each starts at own latch; A skips at ~3.5s, B
     watches to completion uncorrupted; B-in-room variant unaffected.

## Verdict

Slice meets AE1/AE2(automated half)/AE3/AE5/AE6 surfaces at the committed
reference bar. Gate decision (affirm / redirect) belongs to the owner — see
checkpoint question.
