using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U13 pause-menu + HUD-restyle suite. Escape behavior drives the shared
    /// CareerQuestApp.TogglePauseMenu seam (the key handler calls the same
    /// path; synthetic key events are not available in batchmode). Volume
    /// persistence uses the real PlayerPrefs keys with cleanup in SetUp AND
    /// TearDown; fullscreen asserts the property write path (batchmode never
    /// changes the actual display). All assertions are instance-scoped;
    /// AutoTick=false is applied only to objects this suite tears down.
    /// </summary>
    public class PauseMenuPlayModeTests
    {
        private GameObject _appObject;
        private CareerQuestApp _app;

        [SetUp]
        public void SetUp()
        {
            // Stale objects from earlier tests AND stale device prefs from
            // aborted runs both poison assertions — clean before, not just after.
            DestroyLeftovers();
            DeleteVolumePrefs();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyLeftovers();
            DeleteVolumePrefs();
        }

        // ------------------------------------------------------------------
        // Escape open/close + timescale contract
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EscapeToggleOpensAndClosesInHubAndInARoomWithoutTouchingTimescale()
        {
            yield return CreateAppOnCampus();

            Assert.That(Time.timeScale, Is.EqualTo(1f), "Baseline timescale.");

            // Hub: open.
            Assert.That(_app.TogglePauseMenu(), Is.True, "Escape should open the menu in the hub.");
            Assert.That(_app.PauseMenu.IsOpen, Is.True);
            var overlay = GameObject.Find(PauseMenuController.OverlayName);
            Assert.That(overlay, Is.Not.Null, "Modal overlay mounts.");
            Assert.That(overlay.GetComponent<Image>().raycastTarget, Is.True,
                "Pause overlay opts into modal raycast blocking (ceremony-overlay pattern).");
            Assert.That(GameObject.Find(PauseMenuController.CardName), Is.Not.Null, "Paper card mounts.");
            Assert.That(Time.timeScale, Is.EqualTo(1f),
                "UI-overlay only: pause NEVER alters Time.timeScale (networked session; 2P partner unaffected).");

            // Hub: close.
            Assert.That(_app.TogglePauseMenu(), Is.True, "Escape should close the open menu.");
            Assert.That(_app.PauseMenu.IsOpen, Is.False);
            yield return null; // Destroy is deferred
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Null);

            // Room: open + close again.
            _app.ShowDesignBuild(false);
            yield return null;
            Assert.That(_app.TogglePauseMenu(), Is.True, "Escape should open the menu inside a room.");
            Assert.That(_app.PauseMenu.IsOpen, Is.True);
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Not.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));

            Assert.That(_app.TogglePauseMenu(), Is.True);
            yield return null;
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Null);
            Assert.That(Time.timeScale, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator ResumeClosesTheMenuAndReturnsCleanlyToTheScreenBeneath()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            var pause = _app.PauseMenu;
            Assert.That(pause.IsOpen, Is.True);

            // The Resume button and this seam share Close().
            pause.Close();
            yield return null;

            Assert.That(pause.IsOpen, Is.False);
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Null);
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus), "Resume never re-routes.");
            Assert.That(GameObject.Find("CampusHud"), Is.Not.Null, "The screen beneath survives the pause round-trip.");
        }

        // ------------------------------------------------------------------
        // Suppression during the reveal cinematic (ignore, not defer)
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator EscapeDuringRevealCinematicIsIgnoredAndCameraStateIsUnchanged()
        {
            yield return CreateAppOnCampus();
            _app.Session.SeedShowcase(); // 3 badges → unlocked cinematic

            _app.ShowReveal();
            var reveal = _appObject.GetComponent<CareerRevealController>();
            var director = reveal.Director;
            var world = CampusWorldController.Ensure();
            var cameraDirector = world.CameraDirector;
            director.AutoTick = false;
            cameraDirector.AutoTick = false;

            var safety = 0;
            while (world.IsRoomVeilActive && safety++ < 120)
            {
                yield return null;
            }

            director.Tick(0f); // open the latch
            director.Tick(0.6f); // mid camera tween — the cinematic owns the camera
            cameraDirector.Tick(0.6f);
            Assert.That(director.IsRunning, Is.True);
            Assert.That(cameraDirector.ActiveMode, Is.EqualTo(CameraDirectorMode.Tween));

            var modeBefore = cameraDirector.ActiveMode;
            var shotBefore = cameraDirector.CurrentShot;

            Assert.That(_app.IsPauseSuppressed, Is.True, "Cinematic beats suppress Escape.");
            Assert.That(_app.TogglePauseMenu(), Is.False, "Escape is IGNORED (not deferred) mid-cinematic.");
            Assert.That(_app.PauseMenu.IsOpen, Is.False);
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Null, "No menu mounts over the cinematic.");

            // No camera-state corruption: the toggle attempt left the director untouched.
            Assert.That(cameraDirector.ActiveMode, Is.EqualTo(modeBefore), "Camera mode unchanged.");
            Assert.That(cameraDirector.CurrentShot.Approximately(shotBefore), Is.True, "Camera shot unchanged.");

            // Once the cinematic resolves, Escape works again.
            director.FastForwardToEnd();
            Assert.That(_app.IsPauseSuppressed, Is.False);
            Assert.That(_app.TogglePauseMenu(), Is.True, "Escape works after the beats resolve.");
            _app.PauseMenu.Close();
        }

        // ------------------------------------------------------------------
        // Volume persistence + independent tiers + boot application
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator VolumeChangesPersistToPlayerPrefsAndTiersRespondIndependently()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            var pause = _app.PauseMenu;
            var audio = AudioDirector.Instance;
            Assert.That(audio, Is.Not.Null, "App boot attaches the AudioDirector.");

            pause.SetSfxVolume(0.25f);
            pause.SetMusicVolume(0.75f);

            Assert.That(PlayerPrefs.GetFloat(AudioDirector.SfxVolumePrefKey, -1f), Is.EqualTo(0.25f).Within(0.0001f),
                "SFX volume persists as a device pref (R23: settings, never child data).");
            Assert.That(PlayerPrefs.GetFloat(AudioDirector.MusicVolumePrefKey, -1f), Is.EqualTo(0.75f).Within(0.0001f));
            Assert.That(audio.SfxVolume, Is.EqualTo(0.25f).Within(0.0001f), "Slider drives the live SFX tier.");
            Assert.That(audio.MusicVolume, Is.EqualTo(0.75f).Within(0.0001f), "Slider drives the live music tier.");

            // Independent tiers: changing one never moves the other.
            pause.SetSfxVolume(0.9f);
            Assert.That(audio.SfxVolume, Is.EqualTo(0.9f).Within(0.0001f));
            Assert.That(audio.MusicVolume, Is.EqualTo(0.75f).Within(0.0001f), "Music tier unaffected by SFX writes.");
            Assert.That(PlayerPrefs.GetFloat(AudioDirector.MusicVolumePrefKey, -1f), Is.EqualTo(0.75f).Within(0.0001f));
        }

        [UnityTest]
        public IEnumerator PersistedVolumesApplyWhenTheAudioDirectorBoots()
        {
            PlayerPrefs.SetFloat(AudioDirector.SfxVolumePrefKey, 0.4f);
            PlayerPrefs.SetFloat(AudioDirector.MusicVolumePrefKey, 0.6f);
            PlayerPrefs.Save();

            // Fresh, instance-scoped director (the boot path under test).
            var host = new GameObject("pause-menu-audio-boot-test");
            var director = host.AddComponent<AudioDirector>();
            director.AutoTick = false; // own instance only — no AutoTick leaks
            yield return null;

            Assert.That(director.SfxVolume, Is.EqualTo(0.4f).Within(0.0001f), "Persisted SFX volume loads on boot.");
            Assert.That(director.MusicVolume, Is.EqualTo(0.6f).Within(0.0001f), "Persisted music volume loads on boot.");

            Object.DestroyImmediate(host);
        }

        // ------------------------------------------------------------------
        // Fullscreen (property write path — batchmode-safe)
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator FullscreenToggleFlipsTheRequestedStateThroughThePropertyWritePath()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            var pause = _app.PauseMenu;

            pause.SetFullscreen(true);
            Assert.That(pause.FullscreenRequested, Is.True, "Property write path: fullscreen requested.");

            pause.SetFullscreen(false);
            Assert.That(pause.FullscreenRequested, Is.False, "Property write path: windowed requested.");

            pause.ToggleFullscreen();
            Assert.That(pause.FullscreenRequested, Is.True, "The menu button toggles the state.");
            pause.SetFullscreen(false); // leave a windowed request behind
        }

        // ------------------------------------------------------------------
        // Exit to Title (existing teardown path, never a raw scene reload)
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ExitToTitleRoutesThroughTeardownToTheEntryScreen()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            _app.PauseMenu.ExitToTitle();
            yield return null;

            Assert.That(_app.PauseMenu.IsOpen, Is.False);
            Assert.That(GameObject.Find(PauseMenuController.OverlayName), Is.Null);
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Entry), "Exit to Title lands on the entry route.");
            Assert.That(GameObject.Find("EntryPanel"), Is.Not.Null, "The title screen renders through the normal route API.");
            Assert.That(GameObject.Find("CampusHud"), Is.Null, "Campus UI tore down through ResetRoot.");
        }

        // ------------------------------------------------------------------
        // HUD restyle (U9 folded): paper card, identity, badge meter, no debug text
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator CampusHudIsAPaperCardWithIdentityBadgeMeterAndNoModeText()
        {
            yield return CreateAppOnCampus();

            var hud = GameObject.Find("CampusHud");
            Assert.That(hud, Is.Not.Null, "Campus HUD card mounts.");

            var paper = new Color(1f, 0.969f, 0.878f); // DESIGN.md Paper #FFF7E0
            var hudColor = hud.GetComponent<Image>().color;
            Assert.That(hudColor.r, Is.EqualTo(paper.r).Within(0.01f), "HUD card uses the Paper surface.");
            Assert.That(hudColor.g, Is.EqualTo(paper.g).Within(0.01f));
            Assert.That(hudColor.b, Is.EqualTo(paper.b).Within(0.01f));

            // Avatar identity chip.
            var avatarName = GameObject.Find("CampusAvatarName");
            Assert.That(avatarName, Is.Not.Null, "Avatar identity chip present.");
            Assert.That(avatarName.GetComponent<TextMeshProUGUI>().text,
                Is.EqualTo(_app.Session.SelectedAvatar.DisplayName));

            // Badge progress meter.
            var badgeLabel = GameObject.Find("CampusBadgeMeterLabel");
            Assert.That(badgeLabel, Is.Not.Null, "Badge meter present.");
            Assert.That(badgeLabel.GetComponent<TextMeshProUGUI>().text, Does.Contain("0/3"));
            Assert.That(GameObject.Find("CampusBadgeChip0"), Is.Not.Null);
            Assert.That(GameObject.Find("CampusBadgeChip2"), Is.Not.Null);

            // One short controls hint, single line.
            var hint = GameObject.Find("CampusControlsHint");
            Assert.That(hint, Is.Not.Null, "Condensed controls hint present.");
            var hintText = hint.GetComponent<TextMeshProUGUI>().text;
            Assert.That(hintText, Does.Contain("WASD"));
            Assert.That(hintText, Does.Not.Contain("\n"), "Hint stays one short line.");

            // Utility/debug text is gone from the player HUD.
            Assert.That(GameObject.Find("CampusMode"), Is.Null, "'Mode: Play / None' removed from the player HUD.");
            Assert.That(GameObject.Find("FutureLabels"), Is.Null, "Dense future-buildings line removed.");
            Assert.That(GameObject.Find("CampusTitle"), Is.Null, "'Free Campus' utility title removed.");

            foreach (var text in Object.FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
            {
                Assert.That(text.text, Does.Not.Contain("Mode:"),
                    $"Player-facing text '{text.name}' must not carry debug mode info (DemoDebugOverlay owns it).");
            }
        }

        // ------------------------------------------------------------------
        // U9: facilitator controls live inside the pause surface
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator PauseMenuHostsFacilitatorControlsThatToggleQuietAndTearDownOnClose()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            yield return null;

            // The facilitator control row mounts alongside the existing pause
            // card (design doc: controls live in the pause surface, not a
            // separate educator product).
            Assert.That(GameObject.Find(PauseMenuController.CardName), Is.Not.Null, "The pause card still mounts.");
            Assert.That(GameObject.Find(FacilitatorControlsController.PanelName), Is.Not.Null,
                "Facilitator controls mount inside the pause surface.");
            Assert.That(GameObject.Find(FacilitatorControlsController.QuietToggleButtonName), Is.Not.Null);

            // The quiet toggle from the pause surface flips the classroom mode.
            Assert.That(_app.Session.ClassroomAccess.QuietMode, Is.False);
            _app.FacilitatorControls.ToggleQuietMode();
            Assert.That(_app.Session.ClassroomAccess.QuietMode, Is.True,
                "The pause-hosted quiet control toggles reduced-motion/quiet mode.");
            Assert.That(AudioCueCatalog.QuietMode, Is.True);

            // Closing the menu tears the controls down with it; timescale stays 1.
            _app.PauseMenu.Close();
            yield return null;
            Assert.That(GameObject.Find(FacilitatorControlsController.PanelName), Is.Null,
                "Facilitator controls unmount when the pause menu closes.");
            Assert.That(Time.timeScale, Is.EqualTo(1f), "Pause never touches Time.timeScale.");

            // Reset the static gate this test armed.
            _app.SetQuietMode(false);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IEnumerator CreateAppOnCampus()
        {
            _appObject = new GameObject("pause-menu-test");
            _app = _appObject.AddComponent<CareerQuestApp>();
            yield return null; // Start() renders the entry screen

            yield return PlayModeTestBootstrap.EnterPlayCampus(_app);
        }

        private static void DeleteVolumePrefs()
        {
            PlayerPrefs.DeleteKey(AudioDirector.SfxVolumePrefKey);
            PlayerPrefs.DeleteKey(AudioDirector.MusicVolumePrefKey);
            PlayerPrefs.Save();
        }

        private void DestroyLeftovers()
        {
            if (_appObject != null)
            {
                Object.DestroyImmediate(_appObject);
                _appObject = null;
            }

            foreach (var hub in Object.FindObjectsByType<PlayableHubController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(hub.gameObject);
            }

            foreach (var world in Object.FindObjectsByType<CampusWorldController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(world.gameObject);
            }

            // Director-created cameras die with the director (suite pitfall).
            foreach (var director in Object.FindObjectsByType<CameraDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var camera = director.Camera;
                Object.DestroyImmediate(director.gameObject);
                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }

            DestroyAllNamed("CareerQuestCanvas");
            DestroyAllNamed(RevealStageLayout.TokenLayerName);
            DestroyAllNamed("AcceptPoof");
            DestroyAllNamed("CeremonyConfetti");

            _app = null;
        }

        private static void DestroyAllNamed(string name)
        {
            var safety = 0;
            for (var found = GameObject.Find(name); found != null && safety++ < 32; found = GameObject.Find(name))
            {
                Object.DestroyImmediate(found);
            }
        }
    }
}
