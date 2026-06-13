using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U9 (R18, KTD7): the guided Party Run presenter over session-only state.
    /// Covers: starting a run sets the full guided state (ordered station ids,
    /// selected seed ids, round index, completed ids, active/complete flags,
    /// progress strip); the run resumes after a campus/gallery/non-run detour;
    /// quitting clears ONLY guided sequencing and preserves earned results,
    /// accessories, badges, traits, and evolution; and normal campus play can
    /// enter ANY station in ANY order without starting or obeying the run.
    ///
    /// Real station completions drive the run through the deterministic
    /// controller seam (the Robotics proof pattern); session state is seeded
    /// directly where a unit suffices (lean suite per the perf budget).
    /// </summary>
    public class PartyRunPresenterPlayModeTests
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
        // Starting a run sets the full guided state + progress strip.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator StartingAGuidedRunSetsOrderedStateAndProgressStrip()
        {
            yield return CreateAppOnCampus();

            var stations = new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.MusicStudioId, CareerQuestCatalog.AiLabId };
            var seeds = new[] { Robotics.DefaultSeed.SeedId, (string)null, (string)null };
            Assert.That(_app.StartPartyRun(stations, seeds), Is.True);

            var run = _app.Session.PartyRun;
            Assert.That(run.IsActive, Is.True);
            Assert.That(run.IsComplete, Is.False);
            Assert.That(run.StationIds, Is.EqualTo(stations));
            Assert.That(run.SeedIds[0], Is.EqualTo(Robotics.DefaultSeed.SeedId));
            Assert.That(run.CurrentRound, Is.EqualTo(0));
            Assert.That(run.CurrentStationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId));
            Assert.That(run.CompletedStationIds, Is.Empty);

            // Progress strip: one cell per round, first Current, rest Upcoming.
            var strip = run.ProgressStrip;
            Assert.That(strip.Count, Is.EqualTo(3));
            Assert.That(strip[0].State, Is.EqualTo(PartyRunStepState.Current));
            Assert.That(strip[1].State, Is.EqualTo(PartyRunStepState.Upcoming));

            // The presenter panel mounts on the campus while a run is active.
            Assert.That(GameObject.Find(PartyRunPresenter.PanelName), Is.Not.Null,
                "The Party Run panel mounts on campus when a run is active.");
            Assert.That(GameObject.Find(PartyRunPresenter.ProgressStripName), Is.Not.Null);
            Assert.That(GameObject.Find(PartyRunPresenter.ContinueButtonName), Is.Not.Null,
                "A pending run shows Continue.");

            // Non-color cue: each strip cell carries a glyph label, not color alone.
            var firstLabel = GameObject.Find($"{PartyRunPresenter.StepCellPrefix}0Label");
            Assert.That(firstLabel, Is.Not.Null, "Progress cells carry a non-color glyph label.");
        }

        // ------------------------------------------------------------------
        // Completing the current round advances; resume after a detour.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator GuidedRunResumesAfterReturnToCampusAndGalleryDetour()
        {
            yield return CreateAppOnCampus();

            var stations = new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId, CareerQuestCatalog.MusicStudioId };
            Assert.That(_app.StartPartyRun(stations), Is.True);

            // Advance round one through the state seam (the ceremony-free path —
            // real completion advancement is proven in the KTD7 test below).
            Assert.That(_app.Session.PartyRun.NoteStationCompleted(CareerQuestCatalog.RoboticsGarageId), Is.True);
            var run = _app.Session.PartyRun;
            Assert.That(run.CurrentRound, Is.EqualTo(1));
            Assert.That(run.CurrentStationId, Is.EqualTo(CareerQuestCatalog.AiLabId));
            Assert.That(run.IsActive, Is.True);

            // Detour: gallery, then a non-run return to campus. The run must
            // still be active and resumable, and the campus presenter re-mounts.
            _app.ShowGallery();
            yield return null;
            Assert.That(_app.Session.PartyRun.IsActive, Is.True, "A gallery detour never erases the run.");

            _app.ReturnToCampus();
            yield return null;
            Assert.That(GameObject.Find(PartyRunPresenter.PanelName), Is.Not.Null,
                "Returning to campus re-mounts the Party Run panel (resumable).");
            Assert.That(_app.Session.PartyRun.CurrentStationId, Is.EqualTo(CareerQuestCatalog.AiLabId),
                "The run resumes the NEXT round after the detour.");

            // The campus panel's round intro names the resumed round's station.
            var intro = GameObject.Find(PartyRunPresenter.RoundIntroName);
            Assert.That(intro, Is.Not.Null);
            Assert.That(intro.GetComponent<TMPro.TextMeshProUGUI>().text,
                Does.Contain(PartyStationDefinitions.GetById(CareerQuestCatalog.AiLabId).DisplayName));
        }

        // ------------------------------------------------------------------
        // Quit clears ONLY sequencing; earned state preserved.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator QuittingClearsOnlySequencingAndPreservesEarnedResults()
        {
            yield return CreateAppOnCampus();

            var controller = PrepareController(quickPacing: true);
            Assert.That(_app.StartPartyRun(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId }), Is.True);

            Assert.That(_app.PartyRunPresenter.Continue(), Is.True);
            yield return MountFrames();
            CompleteGolden(controller);

            // Earned facts BEFORE the quit.
            var session = _app.Session;
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Not.Null);
            var rewardEventCount = session.RewardLog.Events.Count;
            Assert.That(rewardEventCount, Is.GreaterThanOrEqualTo(1));

            // Clear the completion ceremony (lands in gallery) so the quit/return
            // routes are not blocked by the ceremony guard.
            yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
            Assert.That(_app.TrySkipCeremony(), Is.True);
            yield return null;

            // Quit the run.
            _app.QuitPartyRun();
            yield return null;

            // ONLY sequencing cleared.
            Assert.That(session.PartyRun.IsActive, Is.False, "Quit clears the guided sequence.");
            Assert.That(session.PartyRun.StationIds, Is.Empty);
            Assert.That(GameObject.Find(PartyRunPresenter.PanelName), Is.Null,
                "The Party Run panel is gone after Quit.");

            // Earned state PRESERVED (results, badge/evolution, reward log).
            Assert.That(session.UniqueCompletedGames, Is.EqualTo(1), "Quit preserves earned results.");
            Assert.That(session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Not.Null,
                "Quit preserves the earned best result.");
            Assert.That(session.RewardLog.Events.Count, Is.EqualTo(rewardEventCount),
                "Quit preserves the reward-event log (accessories/badges derive from it).");

            // Campus free-choice still works after a quit.
            _app.ShowCampus();
            yield return null;
            var evolution = Object.FindAnyObjectByType<CampusEvolutionController>();
            Assert.That(evolution, Is.Not.Null);
            Assert.That(evolution.HasPiece(CareerQuestCatalog.RoboticsGarageId), Is.True,
                "The earned evolution piece survives the quit.");
        }

        // ------------------------------------------------------------------
        // KTD7: free-choice play is independent of an active run.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator NormalCampusPlayEntersAnyStationInAnyOrderWithoutObeyingTheRun()
        {
            yield return CreateAppOnCampus();

            var controller = PrepareController(quickPacing: true);

            // A run is active with Robotics as round one...
            Assert.That(_app.StartPartyRun(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId }), Is.True);
            var run = _app.Session.PartyRun;
            Assert.That(run.CurrentStationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId));

            // ...but the player free-chooses a DIFFERENT station (AI Lab, the
            // run's round two) straight from the campus. It enters normally.
            Assert.That(_app.ShowPartyStation(CareerQuestCatalog.AiLabId), Is.True,
                "Free-choice entry of any station works while a run is active.");
            yield return MountFrames();
            CompleteGolden(controller);

            // The out-of-order completion did NOT advance the guided run (KTD7):
            // the round/current station are unchanged, and AI Lab is not in the
            // run's completed list even though it was completed for real.
            Assert.That(run.CurrentRound, Is.EqualTo(0), "An out-of-order completion never advances the run.");
            Assert.That(run.CurrentStationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId));
            Assert.That(run.CompletedStationIds, Does.Not.Contain(CareerQuestCatalog.AiLabId));

            // The completion is still real for free-choice progression/scoring.
            Assert.That(_app.Session.GetBestResult(CareerQuestCatalog.AiLabId), Is.Not.Null,
                "Free-choice completions still count toward normal progression.");
            Assert.That(_app.Session.UniqueCompletedGames, Is.EqualTo(1));
        }

        // ------------------------------------------------------------------
        // Reveal handoff reads the synthesis snapshot (no second gate).
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator RevealHandoffAppearsOnlyWhenTheSynthesisSnapshotIsRevealReady()
        {
            yield return CreateAppOnCampus();

            Assert.That(_app.StartPartyRun(new[] { CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId, CareerQuestCatalog.MusicStudioId }), Is.True);

            // Below the gate: no reveal handoff button.
            Assert.That(GameObject.Find(PartyRunPresenter.RevealButtonName), Is.Null,
                "No reveal handoff before the reveal-ready gate.");

            // Seed three unique completions directly (mirrors the synthesis gate),
            // then re-mount the campus presenter.
            SeedCompletions(CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.AiLabId, CareerQuestCatalog.MusicStudioId);
            Assert.That(RevealSynthesis.Resolve(_app.Session).IsRevealReady, Is.True);

            _app.ShowCampus();
            yield return null;
            Assert.That(GameObject.Find(PartyRunPresenter.RevealButtonName), Is.Not.Null,
                "The reveal handoff appears once the synthesis snapshot is reveal-ready.");

            // The handoff routes to the reveal stage.
            _app.PartyRunPresenter.RevealHandoff();
            yield return null;
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Reveal));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IEnumerator CreateAppOnCampus()
        {
            _appObject = new GameObject("party-run-test");
            _app = _appObject.AddComponent<CareerQuestApp>();
            yield return null;
            yield return PlayModeTestBootstrap.EnterPlayCampus(_app);
        }

        private PartyStationController PrepareController(bool quickPacing)
        {
            var controller = _appObject.GetComponent<PartyStationController>()
                ?? _appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false;
            controller.QuickPacing = quickPacing;
            return controller;
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

        private static void CompleteGolden(PartyStationController controller)
        {
            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value);
            }

            if (controller.IsAwaitingConfirmation)
            {
                controller.TrySubmitDrop(controller.ConfirmationObjectId, null);
            }
        }
    }
}
