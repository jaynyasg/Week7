using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U4 proof gate (KTD6): Robotics Rescue through the REAL scene lifecycle —
    /// route in by station id, definition-driven render, default seed first
    /// play, guide intro + reward preview, hint ladder + gentle rejects from
    /// seed copy, exactly ONE MiniGameResult through the duplicate gate,
    /// ceremony, gallery/evolution/reveal compatibility, alternate-seed replay
    /// without completion-count inflation, and the 2P submit seam (mirrors the
    /// *NetworkSeamPlayModeTests single-process pattern).
    /// </summary>
    public class PartyStationRoboticsPlayModeTests
    {
        private const ulong SimulatedPartnerClientId = 2UL;

        private static PartyStationDefinition Robotics =>
            PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);

        [SetUp]
        public void SetUp()
        {
            // Test isolation (SceneWipe leak history): stale roots from earlier
            // suites must not pollute object lookups or the result counts.
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [UnityTest]
        public IEnumerator RoboticsRendersFromDefinitionWithDefaultSeedGuideAndRewardPreview()
        {
            var appObject = new GameObject("party-robotics-render-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject, quickPacing: false);
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True,
                "Robotics routes through the generic station branch (converted legacy station).");
            yield return MountFrames();

            var seed = Robotics.DefaultSeed;
            Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation));
            Assert.That(app.CurrentStationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId));
            Assert.That(GameObject.Find(PartyStationController.SeedChoicePanelName), Is.Null,
                "First play enters the default seed directly — no choice panel.");

            // Definition-driven HUD + guide + reward preview (data, not code).
            Assert.That(TmpText("PartyStationTitle"), Is.EqualTo(Robotics.DisplayName));
            Assert.That(TmpText("PartyStationPrompt"), Is.EqualTo(Robotics.ResolvePrompt(seed)));
            Assert.That(TmpText(StationGuideView.NameTextName), Is.EqualTo(Robotics.GuideName));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.IntroLine));
            Assert.That(TmpText(StationRewardPreview.LineTextName), Is.EqualTo(seed.RewardPreviewLine));
            Assert.That(TmpText(StationRewardPreview.AccessoryTextName), Is.EqualTo("Tool Belt"));

            // The toy playfield mounts from the seed's rules: every chain toy
            // plus the reaction toy, one zone per slot target.
            Assert.That(controller.PieceFor("battery_toast"), Is.Not.Null);
            Assert.That(controller.PieceFor("rescue_flag"), Is.Not.Null, "Reaction toys mount too (no dead toys).");
            Assert.That(controller.ZoneFor("slot.battery_toast"), Is.Not.Null);
            Assert.That(GameObject.Find(PartyStationRenderer.SetRootName), Is.Not.Null,
                "The station set dressing mounts with the room scene.");

            // Normal pacing: the 3-5s intro beat holds play, then hands off.
            Assert.That(controller.IsIntroComplete, Is.False);
            Assert.That(controller.CanBeginDrag("battery_toast"), Is.False, "Drag locks during the intro beat.");
            controller.Tick(PartyStationController.IntroHoldSeconds);
            Assert.That(controller.IsIntroComplete, Is.True);
            Assert.That(controller.CanBeginDrag("battery_toast"), Is.True, "The intro hands off into the toy challenge.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator HintLadderAndGentleRejectsSpeakSeedCopy()
        {
            var appObject = new GameObject("party-robotics-hints-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject, quickPacing: true);
            app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId);
            yield return MountFrames();

            var seed = Robotics.DefaultSeed;

            // Wrong attempt -> gentle bounce + the seed's level-1 hint line.
            Assert.That(controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich"),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.HintLine));
            Assert.That(controller.IsToyAccepted("battery_toast"), Is.False, "Rejects never advance progress.");

            // Second wrong attempt -> escalation line + toy highlight pulse.
            controller.TrySubmitDrop("battery_toast", "slot.wheel_sandwich");
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.EscalationHintLine));
            Assert.That(controller.HighlightObjectId, Is.EqualTo("battery_toast"));
            Assert.That(ToyHintPulse.IsShownOn(controller.PieceFor("battery_toast").gameObject), Is.True,
                "The level-2 hint pulses the next expected toy.");

            // An accepted action recovers the ladder back to the intro line.
            Assert.That(controller.TrySubmitDrop("battery_toast", "slot.battery_toast"),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.IntroLine));
            Assert.That(ToyHintPulse.IsShownOn(controller.PieceFor("battery_toast").gameObject), Is.False);

            // Idle time raises the first hint again (deterministic clock).
            controller.Tick(ToyPatternController.IdleHintSeconds);
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.HintLine));

            // Reaction toys answer pokes without bouncing or progressing.
            Assert.That(controller.TrySubmitDrop("rescue_flag", "slot.battery_toast"),
                Is.EqualTo(DropSubmitResult.Accepted), "Reaction toys acknowledge, never bounce.");
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.RoboticsGarageId), Is.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator CompletingRoboticsEmitsExactlyOneResultThroughTheDuplicateGate()
        {
            var appObject = new GameObject("party-robotics-complete-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject, quickPacing: true);
            var rewardEvents = new List<StationRewardEvent>();
            controller.RewardEventEmitted += rewardEvents.Add;

            app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId);
            yield return MountFrames();

            CompleteGolden(controller);

            // Exactly one normal MiniGameResult with the full station contract.
            var seed = Robotics.DefaultSeed;
            var result = app.Session.GetBestResult(CareerQuestCatalog.RoboticsGarageId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.DisplayName, Is.EqualTo(Robotics.DisplayName));
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(result.Source, Is.EqualTo(ResultSource.Solo));
            Assert.That(result.Summary, Is.EqualTo(seed.ResultSummary));
            Assert.That(result.Accuracy, Is.EqualTo(1f).Within(0.001f));
            Assert.That(result.TimeRemaining, Is.GreaterThan(0f));
            Assert.That(result.TraitDeltas, Is.EqualTo(Robotics.TraitDeltas.ToList()));
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1));
            Assert.That(app.Session.GamesNeededForReveal, Is.EqualTo(2), "Stations count toward reveal readiness.");

            // Success copy + reward beat come from the seed data.
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.SuccessLine));
            Assert.That(TmpText(StationGuideView.ReactionTextName), Is.EqualTo(seed.NpcReaction));
            Assert.That(GameObject.Find(StationRewardPreview.EarnedStampName), Is.Not.Null,
                "The reward preview stamps earned on completion.");

            // U6 reward seam: exactly one event, carrying seed + accessory facts.
            Assert.That(rewardEvents.Count, Is.EqualTo(1));
            Assert.That(rewardEvents[0].StationId, Is.EqualTo(CareerQuestCatalog.RoboticsGarageId));
            Assert.That(rewardEvents[0].SeedId, Is.EqualTo(seed.SeedId));
            Assert.That(rewardEvents[0].AccessoryRewardId, Is.EqualTo("accessory.tool_belt"));
            Assert.That(rewardEvents[0].Tier, Is.EqualTo(CompletionTier.Degree));

            // Ceremony mounts; the room lifecycle owns the handoff to gallery.
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);

            // Duplicate gate: double-submit bounces gently, no second result,
            // no second reward event.
            Assert.That(controller.TrySubmitDrop("battery_toast", "slot.battery_toast"),
                Is.EqualTo(DropSubmitResult.RejectedLocked));
            Assert.That(rewardEvents.Count, Is.EqualTo(1));
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1));

            // Route race: re-entering the station mid-ceremony is refused.
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.False,
                "The ceremony guard blocks a rerender/route race.");
            Assert.That(rewardEvents.Count, Is.EqualTo(1));

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator ReplayOffersAlternateSeedWithoutInflatingCompletionOrBadges()
        {
            var appObject = new GameObject("party-robotics-replay-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject, quickPacing: true);
            var rewardEvents = 0;
            controller.RewardEventEmitted += _ => rewardEvents++;

            app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId);
            yield return MountFrames();
            CompleteGolden(controller);
            yield return null;

            // Skip the ceremony through the guarded seam, landing in gallery.
            yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
            Assert.That(app.TrySkipCeremony(), Is.True);
            yield return null;

            // Gallery/passport state: the robotics badge sticker is earned.
            Assert.That(GameObject.Find("AchievementGalleryPanel"), Is.Not.Null);
            Assert.That(GameObject.Find($"{CareerQuestCatalog.RoboticsGarageId}ChipStamp"), Is.Not.Null,
                "Completion appears in the gallery passport as an earned sticker.");

            // Campus evolution: the robotics city piece joins the skyline
            // without any direct station-controller side effect.
            app.ShowCampus();
            yield return null;
            var evolution = Object.FindAnyObjectByType<CampusEvolutionController>();
            Assert.That(evolution, Is.Not.Null);
            Assert.That(evolution.HasPiece(CareerQuestCatalog.RoboticsGarageId), Is.True,
                "Completing Robotics unlocks its campus evolution piece.");

            // Replay: re-entry now offers the seed choice (default + alternate).
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.RoboticsGarageId), Is.True);
            yield return null;
            Assert.That(controller.IsSeedChoiceOpen, Is.True);
            Assert.That(GameObject.Find(PartyStationController.SeedChoicePanelName), Is.Not.Null);
            var alternate = Robotics.AlternateSeeds[0];
            Assert.That(GameObject.Find($"{PartyStationController.SeedChoiceButtonPrefix}{Robotics.DefaultSeed.SeedId}"), Is.Not.Null);
            Assert.That(GameObject.Find($"{PartyStationController.SeedChoiceButtonPrefix}{alternate.SeedId}"), Is.Not.Null);

            // Choosing the alternate mounts ITS prompt, copy, and objects while
            // the station id/badge identity stay stable.
            Assert.That(controller.ChooseSeed(alternate.SeedId), Is.True);
            yield return MountFrames();
            Assert.That(TmpText("PartyStationPrompt"), Is.EqualTo(Robotics.ResolvePrompt(alternate)));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(alternate.IntroLine));
            Assert.That(controller.PieceFor("moon_wheel"), Is.Not.Null, "Alternate-seed objects replace the defaults.");
            Assert.That(controller.PieceFor("battery_toast"), Is.Null);

            // Completing the replay appends a reward event but never inflates
            // the unique completion count or awards a second badge.
            CompleteGolden(controller);
            Assert.That(rewardEvents, Is.EqualTo(2), "Replays append reward events (R11).");
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1),
                "Replay never inflates the unique completion count.");

            Object.DestroyImmediate(appObject);
        }

        // ------------------------------------------------------------------
        // 2P seam (single-process host pattern, like the room network suites)
        // ------------------------------------------------------------------

        [UnityTest]
        public IEnumerator ClientWrongThenRightSubmissionRejectsOnlySubmitterAndSharesProgress()
        {
            yield return NetcodePlayModeHarness.LoadCampusScene();
            var bootstrap = NetcodePlayModeHarness.FindBootstrap();
            yield return NetcodePlayModeHarness.StartHostAndWait(bootstrap);

            var state = CampusSessionState.Instance.StationProgress;
            state.ServerBeginStation(Robotics.Id, Robotics.DefaultSeed.SeedId);

            var batteryIndex = state.ObjectIndexFor("battery_toast");

            // Wrong object/target from the partner: rejected, targeted at the
            // SUBMITTER only, echoing the submission id — shared progress is
            // untouched on every peer's read model.
            Assert.That(state.ApplySubmission(
                    batteryIndex, state.TargetIndexFor("slot.wheel_sandwich"), 0, 31, SimulatedPartnerClientId),
                Is.EqualTo(ToyActionSubmissionResult.Rejected));
            Assert.That(state.LastRejectClientId, Is.EqualTo(SimulatedPartnerClientId),
                "Only the submitting client receives the reject.");
            Assert.That(state.LastRejectSubmissionId, Is.EqualTo(31));
            Assert.That(state.LastRejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));
            Assert.That(state.AcceptedCount, Is.EqualTo(0));

            // Right object next: accepted into the host-validated shared state
            // both players render from.
            Assert.That(state.ApplySubmission(
                    batteryIndex, state.TargetIndexFor("slot.battery_toast"), 0, 32, SimulatedPartnerClientId),
                Is.EqualTo(ToyActionSubmissionResult.Accepted));
            Assert.That(state.IsObjectAccepted("battery_toast"), Is.True,
                "Both players see accepted progress from the shared read model.");
            Assert.That(state.AcceptedCount, Is.EqualTo(1));
            Assert.That(state.Complete, Is.False);

            yield return NetcodePlayModeHarness.ShutdownNetwork();
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static PartyStationController PrepareController(GameObject appObject, bool quickPacing)
        {
            var controller = appObject.GetComponent<PartyStationController>()
                ?? appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false; // deterministic clock
            controller.QuickPacing = quickPacing;
            return controller;
        }

        private static IEnumerator MountFrames()
        {
            // Frame 1: room veil reveals + room builds; frame 2: the station
            // playfield coroutine mounts pieces/zones; frame 3: settle.
            yield return null;
            yield return null;
            yield return null;
        }

        private static void CompleteGolden(PartyStationController controller)
        {
            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                Assert.That(controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value),
                    Is.EqualTo(DropSubmitResult.Accepted),
                    $"Golden action '{action.ObjectId}' should be accepted.");
            }
        }

        private static string TmpText(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, $"{objectName} should exist.");
            return gameObject.GetComponent<TextMeshProUGUI>().text;
        }
    }
}
