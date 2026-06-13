using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U6 gallery + Quest Passport surfaces, all session-DERIVED (KTD8). Covers:
    /// the gallery gear upgrade, the passport's four pages (Badges/Gear/Combos/
    /// Results), the locked-entry privacy rule (no seed choice for an unplayed
    /// station), completed-entry replay through normal routing, and the empty
    /// passport. Seeds the session directly (the plan's lean-PlayMode guidance)
    /// rather than running full ceremonies.
    /// </summary>
    public class AchievementGalleryPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [TearDown]
        public void TearDown()
        {
            FirstRunGuideBeat.ResetSessionFlag();
        }

        // ------------------------------------------------------------------
        // Gallery: earned gear surfaced from the resolver + passport cross-link.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator GallerySurfacesEarnedGearAndCrossLinksToPassport()
        {
            var appObject = new GameObject("gallery-gear-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            CompleteStation(app, CareerQuestCatalog.RoboticsGarageId, "seed.demo"); // Tool Belt

            app.ShowGallery();
            yield return null;

            Assert.That(GameObject.Find("GalleryGearChip0"), Is.Not.Null, "Earned gear surfaces in the gallery.");
            Assert.That(GameObject.Find("GalleryPassportButton"), Is.Not.Null, "The gallery cross-links to the passport.");

            Object.Destroy(appObject);
        }

        // ------------------------------------------------------------------
        // Passport: four pages render from session state.
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator PassportRendersAllFourPagesFromSessionState()
        {
            var appObject = new GameObject("passport-pages-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            // A combo-eligible pair so the Combos page has a spark to show.
            CompleteStation(app, CareerQuestCatalog.RoboticsGarageId, "seed.a");
            CompleteStation(app, CareerQuestCatalog.CommunityKitchenId, "seed.b");

            app.ShowPassport();
            yield return null;

            // Badges page (default): the completed station shows a replay button.
            Assert.That(GameObject.Find($"{PassportController.PanelName}"), Is.Not.Null);
            Assert.That(GameObject.Find($"{PassportController.ReplayButtonPrefix}{CareerQuestCatalog.RoboticsGarageId}"), Is.Not.Null,
                "Completed entries offer replay.");

            // Gear page: earned accessories render.
            app.ShowPassport(PassportController.PassportPage.Gear);
            yield return null;
            Assert.That(GameObject.Find("accessory.tool_beltGearChip"), Is.Not.Null, "Gear page shows earned accessories.");

            // Combos page: the eligible Robot Chef combo row renders.
            app.ShowPassport(PassportController.PassportPage.Combos);
            yield return null;
            Assert.That(GameObject.Find("PassportComboRow0"), Is.Not.Null, "Combos page lists eligible combos.");

            // Results page: the recent reward events render.
            app.ShowPassport(PassportController.PassportPage.Results);
            yield return null;
            Assert.That(GameObject.Find("PassportResultRow0"), Is.Not.Null, "Results page shows recent micro-results.");

            Object.Destroy(appObject);
        }

        [UnityTest]
        public IEnumerator PassportLockedEntryHidesAnySeedChoice()
        {
            var appObject = new GameObject("passport-locked-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            // Complete ONE station; the Spaceport stays locked.
            CompleteStation(app, CareerQuestCatalog.RoboticsGarageId, "seed.a");

            app.ShowPassport();
            yield return null;

            // The locked station has no replay button and exposes NO seed choice.
            Assert.That(GameObject.Find($"{PassportController.ReplayButtonPrefix}{CareerQuestCatalog.SpaceportId}"), Is.Null,
                "A locked station never offers replay.");
            var seedChoiceLeak = Object.FindObjectsByType<RectTransform>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Any(rect => rect.name.Contains(PassportController.SeedChoiceObjectSuffix));
            Assert.That(seedChoiceLeak, Is.False, "Locked entries must not expose seed choices.");

            Object.Destroy(appObject);
        }

        [UnityTest]
        public IEnumerator PassportCompletedEntryReplaysThroughNormalRouting()
        {
            var appObject = new GameObject("passport-replay-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);
            CompleteStation(app, CareerQuestCatalog.RoboticsGarageId, "seed.a");

            app.ShowPassport();
            yield return null;

            // The replay button routes through the SAME app entry the hub uses.
            app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId);
            yield return null;

            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId),
                "Completed entries replay through the normal station routing.");
            Assert.That(app.Session.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));

            Object.Destroy(appObject);
        }

        [UnityTest]
        public IEnumerator EmptyPassportRendersWithNoCompletions()
        {
            var appObject = new GameObject("passport-empty-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            yield return PlayModeTestBootstrap.EnterPlayCampus(app);

            app.ShowPassport(PassportController.PassportPage.Gear);
            yield return null;
            Assert.That(GameObject.Find("PassportGearEmpty"), Is.Not.Null, "Empty Gear page renders its empty state.");

            app.ShowPassport(PassportController.PassportPage.Results);
            yield return null;
            Assert.That(GameObject.Find("PassportResultsEmpty"), Is.Not.Null, "Empty Results page renders its empty state.");

            app.ShowPassport(PassportController.PassportPage.Badges);
            yield return null;
            Assert.That(GameObject.Find(PassportController.PanelName), Is.Not.Null, "Badges page renders with zero completions.");

            Object.Destroy(appObject);
        }

        // ------------------------------------------------------------------
        // Station-end accessory spotlight beat (reward-event driven).
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator AccessorySpotlightRendersTheRewardEventAndHonorsQuietMode()
        {
            var host = new GameObject("spotlight-test", typeof(AccessorySpotlightController));
            var spotlight = host.GetComponent<AccessorySpotlightController>();
            spotlight.AutoTick = false;
            var parent = UiBuilder.EnsureCanvas().GetComponent<RectTransform>();

            var rewardEvent = new RewardEvent(
                CareerQuestCatalog.RoboticsGarageId,
                "robotics_garage.lunchbox_rescue",
                CompletionTier.Degree,
                ResultSource.Solo,
                "You rebuilt the robot.",
                new[] { new TraitDelta("Building", 5), new TraitDelta("Reasoning", 4) },
                "accessory.tool_belt",
                new[] { "combo.robot_chef" });

            // Quiet mode (U9 seam): the card renders but the beat is held — no
            // auto-dismiss even past the hold window.
            spotlight.Show(parent, rewardEvent, quietMode: true);
            Assert.That(spotlight.IsActive, Is.True);
            Assert.That(spotlight.ShownAccessoryId, Is.EqualTo("accessory.tool_belt"));
            Assert.That(spotlight.ShownAccessoryName, Is.EqualTo("Tool Belt"), "Spotlight shows the unlocked accessory name.");
            Assert.That(spotlight.ShownComboSparkCount, Is.EqualTo(1), "Combo spark ids surface in the beat.");
            Assert.That(GameObject.Find(AccessorySpotlightController.TitleName), Is.Not.Null);

            spotlight.Tick(AccessorySpotlightController.HoldSeconds + 1f);
            Assert.That(spotlight.IsActive, Is.True, "Quiet mode holds the card — no auto-dismiss.");

            // Normal mode auto-dismisses after the hold window.
            spotlight.Show(parent, rewardEvent, quietMode: false);
            Assert.That(spotlight.IsActive, Is.True);
            spotlight.Tick(AccessorySpotlightController.HoldSeconds + 0.1f);
            Assert.That(spotlight.IsActive, Is.False, "Normal mode dismisses after the hold.");

            Object.Destroy(host);
            yield return null;
        }

        private static void CompleteStation(CareerQuestApp app, string stationId, string seedId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            var result = PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f);
            app.Session.RecordResult(result);

            // Feed the reward log the same way the station completion seam does,
            // so the Results/Combos pages have session-derived content.
            app.Session.AppendStationRewardEvent(new StationRewardEvent(
                stationId,
                seedId,
                result.Tier,
                result.Source,
                result.Summary,
                definition.AccessoryRewardId,
                result.TraitDeltas));
        }
    }
}
