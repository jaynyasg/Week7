# Deploy Guide — Career Quest Campus

How to produce a distributable build and publish it. Distribution target is
**itch.io** with a downloadable Windows build (WebGL is preview-only and
optional). The game is a single persistent Unity scene; there is no server to
host — multiplayer is peer host/client over Unity Transport, so "deploy" means
**ship the player build**, not stand up a backend.

## Prerequisites

- Unity `6000.4.10f1` (via Unity Hub) — must match `ProjectSettings/ProjectVersion.txt`.
- The repo checked out with `Assets/`, `Packages/`, `ProjectSettings/` intact.
- Packages restored from `Packages/manifest.json` (Netcode for GameObjects, Unity Transport).
- For automated/headless builds: a closed Unity Editor (Unity is single-instance — a running Editor blocks `-batchmode`).

## 1. Build the Windows player

The build is scripted at `Assets/_CareerQuest/Editor/CareerQuestBuild.cs`
(`CareerQuest.Editor.CareerQuestBuild.BuildWindowsPlayer`). It builds the single
gameplay scene (`Assets/_CareerQuest/Scenes/CareerQuestCampus.unity`) to
`Builds/Windows/CareerQuestCampus.exe` and applies the packaged identity
(product name, window title, icon, splash) via `CareerQuestPackaging.ApplyIdentity()`.

**Option A — from the Editor:** menu `Career Quest ▸ Build Windows Player`.

**Option B — headless (CI / reproducible):**

```bash
"C:/Program Files/Unity/Hub/Editor/6000.4.10f1/Editor/Unity.exe" \
  -batchmode -nographics -quit \
  -projectPath "<repo-root>" \
  -executeMethod CareerQuest.Editor.CareerQuestBuild.BuildWindowsPlayer \
  -logFile build.log
```

Output lands in `Builds/Windows/`:

- `CareerQuestCampus.exe` — the launcher stub (small; its mtime can stay constant across rebuilds since the Unity version is unchanged).
- `CareerQuestCampus_Data/` — the actual game data. **Verify a build is fresh by the mtime of `CareerQuestCampus_Data/Managed/CareerQuest.Runtime.dll`, not the `.exe`.**

`Builds/` is gitignored — it is a build artifact, never committed.

> The build throws `InvalidOperationException` if `BuildResult` is not `Succeeded`,
> so a `-quit` exit code of `0` plus `DisplayProgressNotification: Build Successful`
> in the log means the build is good.

## 2. Smoke-test the build before publishing

Drive the built player headless to confirm states render (no Editor needed — the
player reads its own baked data). States are parsed in `CareerQuestApp` (`-cq-visual-state`):

```bash
Builds/Windows/CareerQuestCampus.exe \
  -cq-visual-state robotics -cq-screenshot proof/robotics.png \
  -screen-width 1280 -screen-height 720 -screen-fullscreen 0
```

Valid states include `campus`, `robotics`, `spaceport`, `weather`, `newsroom`,
`ai-lab`, `kitchen`, `music`, `gallery`, `reveal-unlocked`, and the multiplayer
smoke modes (`host`, `client`, `2p-host`). The player screenshots then self-quits.
Capture from a machine with a warm graphics path; a cold checkout on a
cloud-synced (OneDrive) path may not converge.

Record evidence under `docs/qa/` per the QA template before publishing.

## 3. Package

Zip the `Builds/Windows/` folder (the `.exe` **and** the `_Data/` folder together):

```bash
# from repo root, after a successful build
powershell Compress-Archive -Path Builds/Windows/* -DestinationPath CareerQuestCampus-Windows.zip
```

Ship the whole folder — the `.exe` will not run without its `_Data/` sibling.

## 4. Publish to itch.io

1. Create (or open) the game page on itch.io.
2. Upload `CareerQuestCampus-Windows.zip`; set the upload to **"This file will be played in the browser" = off** and tag it **Windows**, marked as a downloadable executable.
3. Set the page to **Public** (or **Restricted** with the share link for evaluators).
4. Add screenshots (use the `-cq-screenshot` captures from step 2), a short description, and **fallback notes**: the game runs solo from the start (clearly labeled "Solo Fallback"); same-computer two-player uses split keyboard presets; LAN is manual IP-join and is experimental unless tested.
5. (Optional) WebGL preview: only attempt if a WebGL build is produced and networking is not required for the preview — WebGL is preview-only per the locked scope.

### itch.io via Butler (optional, scriptable)

```bash
butler push Builds/Windows <user>/career-quest-campus:windows
```

## 5. Post-publish verification ("accessible for testing")

- Download the published zip on a clean Windows machine, unzip, run `CareerQuestCampus.exe`.
- Confirm `Play` enters the campus and a solo mini-game completes end-to-end.
- Confirm same-computer host + join-localhost shows two avatars moving.
- Link the live itch.io page in the README and the submission bundle.

> The live two-player playtest, the concurrent-player stress pass, and the
> "accessible for testing" sign-off are manual steps — they confirm the build
> behaves on real hardware, which a headless build cannot prove on its own.
