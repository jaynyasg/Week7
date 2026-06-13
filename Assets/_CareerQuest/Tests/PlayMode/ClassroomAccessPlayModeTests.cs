using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U9 (R19): classroom access — quiet/reduced-motion mode and facilitator
    /// controls. Quiet mode must SOFTEN flourish (camera tween, scene-wipe lift,
    /// spotlight pulse, looping audio intensity, flavor cues) AND preserve
    /// completion clarity (the shot still lands, the card still renders, the
    /// room still reveals, completion cues still pass the gate). Facilitator
    /// controls reset-run / return / quiet / restart-demo without clearing
    /// earned results, and only "start over" clears them. Pointer-first
    /// completion and non-color cues are asserted as contracts.
    /// </summary>
    public class ClassroomAccessPlayModeTests
    {
        private GameObject _appObject;
        private CareerQuestApp _app;

        private static PartyStationDefinition Robotics =>
            PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);

        [SetUp]
        public void SetUp()
        {
            PlayModeSceneScrubber.DestroyStaleAppRoots();
            ClassroomAccessSettings.ResetStatics();
            AudioCueCatalog.ResetQuietMode();
        }

        [TearDown]
        public void TearDown()
        {
            if (_appObject != null)
            {
                Object.DestroyImmediate(_appObject);
                _appObject = null;
                _app = null;
            }

            ClassroomAccessSettings.ResetStatics();
            AudioCueCatalog.ResetQuietMode();
        }

        // ------------------------------------------------------------------
        // Quiet mode softens flourish AND preserves completion clarity.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator QuietModeReducesCameraFlourishWhileTheShotStillLands()
        {
            yield return CreateAppOnCampus();
            _app.SetQuietMode(true);

            var camera = CampusWorldController.Ensure().CameraDirector;
            camera.AutoTick = false;
            Assert.That(camera.ReducedMotion, Is.True, "Quiet mode threads reduced motion to the camera.");

            // A flourish tween SNAPS to its target instead of easing (no motion)…
            var target = new CameraShot(new Vector3(3f, 1f, -10f), 6f);
            camera.TweenToShot(target, 0.8f);
            Assert.That(camera.ActiveMode, Is.EqualTo(CameraDirectorMode.FixedShot),
                "Reduced motion suppresses the cinematic tween (it snaps, never eases).");

            // …but the shot still LANDS on target — completion clarity preserved.
            Assert.That(camera.CurrentShot.Approximately(target), Is.True,
                "The camera still reaches the target shot (the move reads, the swoop does not).");
        }

        [UnityTest]
        public IEnumerator QuietModeCollapsesTheSceneWipeLiftButStillCoversTheTransition()
        {
            yield return CreateAppOnCampus();
            _app.SetQuietMode(true);
            Assert.That(ClassroomAccessSettings.ReducedMotionActive, Is.True,
                "Quiet mode arms the static reduced-motion gate the wipe reads.");

            var world = CampusWorldController.Ensure();
            var cover = SceneWipe.CreateCover(world.WorldRoot);
            cover.AutoTick = false;

            // The cover still mounts (the transition reads — completion clarity).
            Assert.That(cover.gameObject.name, Is.EqualTo(SceneWipe.CoverName));

            // The lift collapses to a single tick (the swoosh is suppressed): one
            // small tick fully opens and destroys the curtain.
            cover.BeginOpen(0.3f);
            cover.Tick(0.02f);
            yield return null;
            Assert.That(cover == null, Is.True, "Reduced-motion wipe finishes in one tick (no visible lift).");
        }

        [UnityTest]
        public IEnumerator QuietModeHoldsTheSpotlightPulseButStillShowsTheUnlockCard()
        {
            yield return CreateAppOnCampus();
            _app.SetQuietMode(true);

            var spotlight = _appObject.GetComponent<AccessorySpotlightController>()
                ?? _appObject.AddComponent<AccessorySpotlightController>();
            spotlight.AutoTick = false;

            var rewardEvent = new RewardEvent(
                CareerQuestCatalog.RoboticsGarageId,
                Robotics.DefaultSeed.SeedId,
                CompletionTier.Degree,
                ResultSource.Solo,
                "You rebuilt the robot.",
                new[] { new TraitDelta("Building", 3) },
                "accessory.tool_belt",
                System.Array.Empty<string>());

            spotlight.Show(_app.transform, rewardEvent, quietMode: true);

            // Completion clarity: the unlock card still renders with the gear name.
            Assert.That(spotlight.IsActive, Is.True, "The spotlight card still shows in quiet mode.");
            Assert.That(spotlight.ShownAccessoryName, Is.EqualTo("Tool Belt"));
            Assert.That(GameObject.Find(AccessorySpotlightController.AccessoryNameName), Is.Not.Null);

            // Flourish held: ticking past the hold neither pulses nor auto-dismisses.
            spotlight.Tick(AccessorySpotlightController.HoldSeconds + 1f);
            Assert.That(spotlight.IsActive, Is.True,
                "Quiet mode holds the pulse + auto-dismiss (no motion); the card stays put.");

            spotlight.Dismiss();
        }

        [UnityTest]
        public IEnumerator QuietModeGatesFlavorAudioButLetsCompletionCuesThroughAndDucksLoops()
        {
            yield return CreateAppOnCampus();

            var audio = AudioDirector.Instance;
            Assert.That(audio, Is.Not.Null);
            audio.MusicVolume = 0.8f; // a player-chosen level to duck + restore

            _app.SetQuietMode(true);
            Assert.That(AudioCueCatalog.QuietMode, Is.True);

            // Looping intensity ducked to the soft floor (snapshot kept).
            Assert.That(audio.MusicVolume, Is.LessThanOrEqualTo(AudioCueCatalog.QuietMusicFloor + 0.001f),
                "Quiet mode ducks the looping music/ambience tier.");

            // Completion-clarity allowlist: flavor cues suppressed, completion
            // cues still audible-gated so a finishing action is confirmed.
            Assert.That(AudioCueCatalog.IsAudibleUnderQuietMode(AudioCueIds.ToyBell), Is.False,
                "Flavor cues are suppressed in quiet mode.");
            Assert.That(AudioCueCatalog.IsAudibleUnderQuietMode(AudioCueIds.RoomWipe), Is.False);
            Assert.That(AudioCueCatalog.IsAudibleUnderQuietMode(AudioCueIds.DropAccept), Is.True,
                "Completion cues still pass the quiet gate (the action reads).");
            Assert.That(AudioCueCatalog.IsAudibleUnderQuietMode(AudioCueIds.BadgeStamp), Is.True);

            // Lifting quiet mode restores the player's chosen music level.
            _app.SetQuietMode(false);
            Assert.That(audio.MusicVolume, Is.EqualTo(0.8f).Within(0.001f),
                "Lifting quiet mode restores the snapshotted music volume.");
            Assert.That(AudioCueCatalog.IsAudibleUnderQuietMode(AudioCueIds.ToyBell), Is.True);
        }

        // ------------------------------------------------------------------
        // Facilitator controls: reset run / return / quiet / restart / start over.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator FacilitatorControlsResetRunAndRestartDemoWithoutClearingEarnedResults()
        {
            yield return CreateAppOnCampus();

            // Earn a real result, then start a guided run.
            SeedCompletions(CareerQuestCatalog.RoboticsGarageId);
            Assert.That(_app.StartPartyRun(new[] { CareerQuestCatalog.AiLabId, CareerQuestCatalog.MusicStudioId }), Is.True);
            Assert.That(_app.Session.PartyRun.IsActive, Is.True);

            var controls = _app.FacilitatorControls;

            // Reset current run: clears ONLY sequencing; the earned result stays.
            controls.ResetCurrentRun();
            yield return null;
            Assert.That(_app.Session.PartyRun.IsActive, Is.False, "Reset run clears the guided sequence.");
            Assert.That(_app.Session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Not.Null,
                "Reset run preserves the earned result.");
            Assert.That(_app.Session.UniqueCompletedGames, Is.EqualTo(1));

            // Restart demo route: re-seeds a run from round one, results intact.
            controls.RestartDemoRoute();
            yield return null;
            Assert.That(_app.Session.PartyRun.IsActive, Is.True, "Restart demo seeds a fresh run.");
            Assert.That(_app.Session.PartyRun.StationIds, Is.EqualTo(CareerQuestApp.DefaultDemoRouteStationIds));
            Assert.That(_app.Session.UniqueCompletedGames, Is.EqualTo(1),
                "Restart demo never clears earned results.");

            // Return to campus: clears nothing.
            controls.ReturnToCampus();
            yield return null;
            Assert.That(_app.Session.PartyRun.IsActive, Is.True, "Return to campus never clears the run.");
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
        }

        [UnityTest]
        public IEnumerator FacilitatorQuietToggleAndStartOverHaveDistinctEffects()
        {
            yield return CreateAppOnCampus();
            SeedCompletions(CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId);
            var controls = _app.FacilitatorControls;

            // Quiet toggle flips the classroom mode (and the audio gate).
            Assert.That(_app.Session.ClassroomAccess.QuietMode, Is.False);
            controls.ToggleQuietMode();
            Assert.That(_app.Session.ClassroomAccess.QuietMode, Is.True);
            Assert.That(AudioCueCatalog.QuietMode, Is.True, "The quiet toggle reaches the audio gate.");
            controls.ToggleQuietMode();
            Assert.That(_app.Session.ClassroomAccess.QuietMode, Is.False);

            // Start over: the ONLY control that clears earned results.
            Assert.That(_app.Session.UniqueCompletedGames, Is.EqualTo(2));
            controls.StartOver();
            yield return null;
            Assert.That(_app.Session.UniqueCompletedGames, Is.EqualTo(0),
                "Start over explicitly clears session-earned results.");
            Assert.That(_app.Session.PartyRun.IsActive, Is.False, "Start over also clears any run.");
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
        }

        [UnityTest]
        public IEnumerator FacilitatorControlsMountInsideThePauseSurface()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.TogglePauseMenu(), Is.True);
            yield return null;

            Assert.That(GameObject.Find(FacilitatorControlsController.PanelName), Is.Not.Null,
                "Facilitator controls live inside the pause card (not a separate product).");
            Assert.That(GameObject.Find(FacilitatorControlsController.ResetRunButtonName), Is.Not.Null);
            Assert.That(GameObject.Find(FacilitatorControlsController.QuietToggleButtonName), Is.Not.Null);
            Assert.That(GameObject.Find(FacilitatorControlsController.StartOverButtonName), Is.Not.Null);

            // Closing the pause menu unmounts the controls too.
            _app.PauseMenu.Close();
            yield return null;
            Assert.That(GameObject.Find(FacilitatorControlsController.PanelName), Is.Null);
        }

        // ------------------------------------------------------------------
        // Pointer-first completion + non-color cues.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator PointerFirstStationCompletesThroughTheDropSeamWithNonColorCues()
        {
            yield return CreateAppOnCampus();

            // Pointer-first is the default access contract.
            Assert.That(_app.Session.ClassroomAccess.PointerFirst, Is.True,
                "Pointer-first completion is on by default (no keyboard-only precision).");

            var controller = _appObject.GetComponent<PartyStationController>()
                ?? _appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = true;

            Assert.That(_app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            yield return MountFrames();

            // Pointer path: the station completes purely through the pointer/drop
            // seam (TrySubmitDrop / HandleDrop) — no keyboard input at all.
            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                Assert.That(controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value),
                    Is.EqualTo(DropSubmitResult.Accepted), "Each toy lands through the pointer drop seam.");
            }

            Assert.That(_app.Session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Not.Null,
                "The station completes via pointer-first interaction alone.");

            // Non-color cue for the sort/route decision: the hint ladder names a
            // SPECIFIC toy by id (shape/position signal), not a color alone.
            var seedHighlightObject = controller.Pattern.Rules.BuildGoldenActionSequence().First().ObjectId;
            Assert.That(seedHighlightObject, Is.Not.Null.And.Not.Empty,
                "Toy decisions reference an object id (shape/position cue), not color alone.");
        }

        [UnityTest]
        public IEnumerator PartyRunProgressStripCarriesNonColorGlyphCues()
        {
            yield return CreateAppOnCampus();
            Assert.That(_app.StartPartyRun(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId }), Is.True);

            // The strip cells carry a glyph label alongside the color fill, so a
            // current/done/upcoming cell reads without distinguishing hue.
            var label0 = GameObject.Find($"{PartyRunPresenter.StepCellPrefix}0Label");
            var label1 = GameObject.Find($"{PartyRunPresenter.StepCellPrefix}1Label");
            Assert.That(label0, Is.Not.Null, "Each progress cell has a non-color glyph label.");
            Assert.That(label1, Is.Not.Null);
            Assert.That(label0.GetComponent<TMPro.TextMeshProUGUI>().text, Is.Not.Empty);
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IEnumerator CreateAppOnCampus()
        {
            _appObject = new GameObject("classroom-access-test");
            _app = _appObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(_app);
        }

        private void SeedCompletions(params string[] stationIds)
        {
            foreach (var stationId in stationIds)
            {
                var definition = PartyStationDefinitions.GetById(stationId);
                _app.Session.RecordResult(PartyStationController.BuildResult(
                    definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f));
            }
        }

        private static IEnumerator MountFrames()
        {
            yield return null;
            yield return null;
            yield return null;
        }
    }
}
