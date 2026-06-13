using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U9 observability + privacy (R18/R19, KTD12): the demo debug overlay
    /// surfaces guided-run + classroom-access state for demo diagnosis, and its
    /// output must contain NO child names, rosters, free-text personal data,
    /// telemetry, or persistent identifiers. The overlay text is the proof
    /// surface — these tests assert both the added fields and the privacy floor.
    /// </summary>
    public class DemoDebugOverlayTests
    {
        private GameObject _appObject;
        private CareerQuestApp _app;
        private string _overlayText;

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

        [UnityTest]
        public IEnumerator OverlayShowsGuidedRunAndClassroomAccessState()
        {
            yield return CreateAppOnCampus();

            // Seed a run + a completion + quiet mode so every field has content.
            SeedCompletions(CareerQuestCatalog.RoboticsGarageId);
            _app.StartPartyRun(new[] { CareerQuestCatalog.AiLabId, CareerQuestCatalog.MusicStudioId });
            _app.SetQuietMode(true);

            yield return ShowOverlayAndReadText();
            var text = _overlayText;

            // Guided-run observability fields.
            Assert.That(text, Does.Contain("Run:"), "Overlay shows the run flags.");
            Assert.That(text, Does.Contain("active=True"));
            Assert.That(text, Does.Contain("Run stations:"));
            Assert.That(text, Does.Contain(CareerQuestCatalog.AiLabId), "Ordered run station ids are shown (content ids).");
            Assert.That(text, Does.Contain("Run current:"));

            // Classroom-access observability fields.
            Assert.That(text, Does.Contain("Quiet: True"), "Overlay shows the quiet-mode flag.");
            Assert.That(text, Does.Contain("Pointer-first:"));
            Assert.That(text, Does.Contain("Non-color cues:"));
        }

        [UnityTest]
        public IEnumerator OverlayOutputExcludesNamesRostersFreeTextAndPersistentIds()
        {
            yield return CreateAppOnCampus();

            // Drive real session state: a completion (with seed-aware summary in
            // the reward log), a run, and quiet mode.
            SeedCompletions(CareerQuestCatalog.RoboticsGarageId, CareerQuestCatalog.CommunityKitchenId, CareerQuestCatalog.AiLabId);
            _app.StartPartyRun(CareerQuestApp.DefaultDemoRouteStationIds);

            yield return ShowOverlayAndReadText();
            var text = _overlayText;

            // No child name / avatar identity leaks into the proof output.
            Assert.That(text, Does.Not.Contain(_app.Session.SelectedAvatar.DisplayName),
                "The debug output must not contain the avatar/child display name.");

            // No free-text seed summary copy leaks (the overlay prints content
            // ids only, never the authored micro-result prose).
            var recent = _app.Session.RewardLog.Recent(1);
            if (recent.Count > 0 && !string.IsNullOrEmpty(recent[0].Summary))
            {
                Assert.That(text, Does.Not.Contain(recent[0].Summary),
                    "Free-text micro-result copy must not appear in the debug output.");
            }

            // No telemetry / persistent-identifier vocabulary (none exists in the
            // session model, and the proof surface must never imply one).
            foreach (var banned in new[] { "roster", "telemetry", "analytics", "userId", "deviceId", "guid", "@" })
            {
                Assert.That(text.ToLowerInvariant(), Does.Not.Contain(banned.ToLowerInvariant()),
                    $"Debug output must not contain '{banned}' (privacy floor, KTD12).");
            }
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IEnumerator CreateAppOnCampus()
        {
            _appObject = new GameObject("debug-overlay-test");
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

        private IEnumerator ShowOverlayAndReadText()
        {
            // Re-show the campus so the overlay re-attaches with current state,
            // then toggle it visible and let an Update frame refresh the text.
            _app.ShowCampus();
            yield return null;

            var overlay = _appObject.GetComponent<DemoDebugOverlay>();
            Assert.That(overlay, Is.Not.Null);
            overlay.Toggle(); // make visible — Update refreshes while visible

            // Two frames: the overlay's own Update runs (it is on the active app
            // object) and writes the live state into the text component.
            yield return null;
            yield return null;

            var textObject = GameObject.Find("DemoDebugOverlay");
            Assert.That(textObject, Is.Not.Null, "The debug overlay text mounts.");
            _overlayText = textObject.GetComponent<TextMeshProUGUI>().text;
        }
    }
}
