using System.Collections;
using System.Collections.Generic;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U5 first six station pack: every station plays end to end through the
    /// ONE shared PartyStationController — pattern proof through the real drop
    /// seams (TrySubmitDrop / golden sequence), exactly one normal
    /// MiniGameResult each, campus return, and alternate-seed replay. Plus the
    /// U5 interaction beats: the pointer-first meter widget (Music Remix), the
    /// Kitchen serving confirmation, the Game Studio pitch, and AE2 seed
    /// identity preservation.
    /// </summary>
    public class FirstSixStationPackPlayModeTests
    {
        /// <summary>The first-six pack with each station's pattern proof (plan U5).</summary>
        private static readonly (string StationId, ToyPatternId Pattern)[] FirstSixStations =
        {
            (CareerQuestCatalog.RoboticsGarageId, ToyPatternId.DragToSlot),
            (CareerQuestCatalog.AiLabId, ToyPatternId.SortToBin),
            (CareerQuestCatalog.CommunityKitchenId, ToyPatternId.PickMatchingTrio),
            (CareerQuestCatalog.MusicStudioId, ToyPatternId.ComposeSet),
            (CareerQuestCatalog.VetClinicId, ToyPatternId.MatchAndCare),
            (CareerQuestCatalog.GameStudioId, ToyPatternId.ComposeSet)
        };

        [SetUp]
        public void SetUp()
        {
            // Test isolation (SceneWipe leak history): stale roots from earlier
            // suites must not pollute object lookups or the result counts.
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [UnityTest]
        public IEnumerator EveryFirstSixStationCompletesEmitsOneResultReturnsAndOffersReplaySeeds()
        {
            var appObject = new GameObject("first-six-pack-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            var rewardEvents = new List<StationRewardEvent>();
            controller.RewardEventEmitted += rewardEvents.Add;

            for (var index = 0; index < FirstSixStations.Length; index++)
            {
                var (stationId, expectedPattern) = FirstSixStations[index];
                var definition = PartyStationDefinitions.GetById(stationId);

                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return MountFrames();

                // Generic station-id routing into the shared controller, with
                // the station's authored pattern — never a bespoke room.
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation), stationId);
                Assert.That(app.CurrentStationId, Is.EqualTo(stationId), stationId);
                Assert.That(controller.Pattern.Pattern, Is.EqualTo(expectedPattern), stationId);
                Assert.That(controller.Seed.SeedId, Is.EqualTo(definition.DefaultSeed.SeedId),
                    $"{stationId}: first play uses the default seed.");

                // Complete through the REAL shared seam (golden actions ride
                // TrySubmitDrop; serving/pitch beats included).
                Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True, stationId);

                var result = app.Session.GetBestResult(stationId);
                Assert.That(result, Is.Not.Null, stationId);
                Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree), stationId);
                Assert.That(result.Summary, Is.EqualTo(definition.DefaultSeed.ResultSummary), stationId);
                Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(index + 1),
                    $"{stationId}: each station emits exactly one normal result.");
                Assert.That(rewardEvents.Count, Is.EqualTo(index + 1), stationId);
                Assert.That(rewardEvents[index].StationId, Is.EqualTo(stationId));
                Assert.That(rewardEvents[index].AccessoryRewardId, Is.EqualTo(definition.AccessoryRewardId));

                // The room lifecycle owns the ceremony; skip it and return to
                // campus through the normal path.
                Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null, stationId);
                yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
                Assert.That(app.TrySkipCeremony(), Is.True, stationId);
                yield return null;

                app.ShowCampus();
                yield return null;
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus), stationId);

                // Replay: re-entry offers default AND alternate seeds.
                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return null;
                Assert.That(controller.IsSeedChoiceOpen, Is.True, stationId);
                Assert.That(
                    GameObject.Find($"{PartyStationController.SeedChoiceButtonPrefix}{definition.AlternateSeeds[0].SeedId}"),
                    Is.Not.Null,
                    $"{stationId}: replay offers the alternate seed.");

                app.ShowCampus();
                yield return null;
            }

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator AiLabSortsFactsAndGuessesIntoDifferentBinsThroughTheRealSeam()
        {
            var appObject = new GameObject("first-six-ailab-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.AiLabId);
            yield return MountFrames();

            var seed = PartyStationDefinitions.GetById(CareerQuestCatalog.AiLabId).DefaultSeed;
            var rules = controller.Pattern.Rules;

            // The sort is a real decision: facts and guesses land in
            // DIFFERENT bins (a shared bin would collapse the pattern).
            Assert.That(rules.ExpectedTargetFor("blue_fact_bubbles"), Is.EqualTo("bin.reasoning"));
            Assert.That(rules.ExpectedTargetFor("pink_guess_bubbles"), Is.EqualTo("bin.creativity"));
            Assert.That(controller.ZoneFor("bin.reasoning"), Is.Not.Null);
            Assert.That(controller.ZoneFor("bin.creativity"), Is.Not.Null);

            // Wrong bin bounces gently and speaks the seed's hint copy.
            Assert.That(controller.TrySubmitDrop("blue_fact_bubbles", "bin.science"),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.HintLine));
            Assert.That(controller.IsToyAccepted("blue_fact_bubbles"), Is.False);

            // The right bin accepts through the same seam.
            Assert.That(controller.TrySubmitDrop("blue_fact_bubbles", "bin.reasoning"),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.IsToyAccepted("blue_fact_bubbles"), Is.True);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator MusicRemixMeterWidgetCompletesPointerFirstWithoutKeyboard()
        {
            var appObject = new GameObject("first-six-music-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.MusicStudioId);
            yield return MountFrames();

            // Pointer path one: drag every sound layer onto the mix spot
            // through the drag shell's programmatic seam (the exact code path
            // the pointer handlers wrap).
            var composeZone = controller.ZoneFor(ToyPatternRules.ComposeTargetId);
            Assert.That(composeZone, Is.Not.Null);
            foreach (var layerId in new[] { "drum_cloud", "rain_shaker", "horn_burst" })
            {
                var piece = controller.PieceFor(layerId);
                Assert.That(piece.BeginDragProgrammatic(), Is.True, layerId);
                piece.DragTo(composeZone.transform.position);
                piece.EndDragAt(composeZone.transform.position);
                Assert.That(controller.IsToyAccepted(layerId), Is.True, layerId);
            }

            // All layers in: the generic status hands off to the meter.
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.MusicStudioId), Is.Null,
                "The tempo dial gates completion until it sits in the green band.");
            Assert.That(TmpText("PartyStationStatus"), Does.Contain("Tempo Dial"));

            // Pointer path two: the meter widget — tap/click steps the dial.
            var widget = controller.ZoneFor(ToyPatternRules.MeterTargetPrefix + "tempo_dial")
                .GetComponent<StationMeterWidget>();
            Assert.That(widget, Is.Not.Null, "Meter zones mount the pointer-first widget (R19).");
            Assert.That(widget.IsInGreen, Is.False);

            Assert.That(widget.Tap(), Is.True, "First tap: 20 -> 35.");
            Assert.That(widget.IsInGreen, Is.False);
            Assert.That(widget.Tap(), Is.True, "Second tap: 35 -> 50, inside the green band.");
            Assert.That(widget.IsInGreen, Is.True);

            // Tapping into the green band completed the station — one normal
            // result, no keyboard anywhere in the path.
            var result = app.Session.GetBestResult(CareerQuestCatalog.MusicStudioId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(GameObject.Find(StationMeterWidget.CheckLabelName), Is.Not.Null,
                "The in-green stamp is a text cue, never color-only.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator KitchenServingConfirmationHoldsTheResultUntilTheSwapIsServed()
        {
            var appObject = new GameObject("first-six-kitchen-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.CommunityKitchenId);
            yield return MountFrames();

            // Solve the soup clues (clue first, then the trio) — the chain
            // completes, but the serving confirmation holds the result.
            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                Assert.That(controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value),
                    Is.EqualTo(DropSubmitResult.Accepted), action.ObjectId);
            }

            Assert.That(controller.IsAwaitingConfirmation, Is.True,
                "Kitchen holds completion for the serving confirmation beat.");
            Assert.That(controller.ConfirmationObjectId, Is.EqualTo("kindness_swap"));
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.CommunityKitchenId), Is.Null,
                "No result emits until the bowl is served.");
            Assert.That(TmpText("PartyStationStatus"), Does.Contain("Kindness Swap"),
                "The status names the serving toy (text cue).");
            Assert.That(ToyHintPulse.IsShownOn(controller.PieceFor("kindness_swap").gameObject), Is.True,
                "The serving toy pulses (non-color cue).");

            // Only the serving toy stays live behind the completion lock.
            Assert.That(controller.CanBeginDrag("kindness_swap"), Is.True);
            Assert.That(controller.CanBeginDrag("veggie_clue"), Is.False);
            Assert.That(controller.TrySubmitDrop("veggie_clue", ToyPatternRules.TrioTrayTargetId),
                Is.EqualTo(DropSubmitResult.RejectedLocked));

            // Serving the kindness swap releases the one normal result.
            Assert.That(controller.TrySubmitDrop("kindness_swap", null), Is.EqualTo(DropSubmitResult.Accepted));
            var seed = PartyStationDefinitions.GetById(CareerQuestCatalog.CommunityKitchenId).DefaultSeed;
            var result = app.Session.GetBestResult(CareerQuestCatalog.CommunityKitchenId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Summary, Is.EqualTo(seed.ResultSummary));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.SuccessLine));
            Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null);

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator GameStudioPitchBeatConfirmsThroughThePlaytestButtonDrop()
        {
            var appObject = new GameObject("first-six-gamestudio-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.GameStudioId);
            yield return MountFrames();

            foreach (var action in controller.Pattern.Rules.BuildGoldenActionSequence())
            {
                controller.TrySubmitDrop(action.ObjectId, action.TargetId, action.Value);
            }

            Assert.That(controller.IsAwaitingConfirmation, Is.True,
                "Game Studio holds completion for the pitch beat.");
            Assert.That(controller.ConfirmationObjectId, Is.EqualTo("playtest_button"));
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.GameStudioId), Is.Null);

            // Pitch through the POINTER shell: pick up the playtest button and
            // release it anywhere — the drop confirms.
            var button = controller.PieceFor("playtest_button");
            Assert.That(button.BeginDragProgrammatic(), Is.True,
                "The pitch toy stays draggable behind the completion lock.");
            button.EndDragAt(button.HomePosition);

            Assert.That(controller.IsAwaitingConfirmation, Is.False);
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.GameStudioId), Is.Not.Null,
                "Running the playtest pitches the quest and emits the result.");

            Object.DestroyImmediate(appObject);
        }

        [UnityTest]
        public IEnumerator VetClinicMatchAndCareUsesPretendPlaySafeCareCopy()
        {
            var appObject = new GameObject("first-six-vet-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.VetClinicId);
            yield return MountFrames();

            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.VetClinicId);
            var seed = definition.DefaultSeed;

            // Care toys wait for the care-clue match (MatchAndCare order rule);
            // the reject is a gentle hint, never harsh.
            Assert.That(controller.TrySubmitDrop("comfort_blanket", ToyPatternRules.CareTargetId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.HintLine));

            // The clue card matches onto the toy it illuminates, then care flows.
            Assert.That(controller.TrySubmitDrop("symptom_cards", "mark.care_tool"),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.SuccessLine));
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.VetClinicId), Is.Not.Null);

            // Pretend-play safety: every care line on both seeds passes the
            // shared safety scan (no medical/fear/shame pressure).
            foreach (var careSeed in definition.Seeds)
            {
                foreach (var line in new[]
                {
                    careSeed.IntroLine, careSeed.HintLine, careSeed.EscalationHintLine,
                    careSeed.SuccessLine, careSeed.RewardPreviewLine, careSeed.ResultSummary, careSeed.NpcReaction
                })
                {
                    Assert.That(PartyStationValidator.CheckCopySafety(line, careSeed.SeedId), Is.Empty, line);
                }
            }

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// AE2: replaying with the alternate seed changes the copy and objects
        /// while station id, badge identity, accessory, and the unique
        /// completion count stay exactly the same.
        /// </summary>
        [UnityTest]
        public IEnumerator AlternateSeedReplayPreservesStationIdentity()
        {
            var appObject = new GameObject("first-six-ae2-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            var rewardEvents = new List<StationRewardEvent>();
            controller.RewardEventEmitted += rewardEvents.Add;

            var aiLab = PartyStationDefinitions.GetById(CareerQuestCatalog.AiLabId);
            app.ShowPartyStation(CareerQuestCatalog.AiLabId);
            yield return MountFrames();
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);
            yield return null;

            yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
            Assert.That(app.TrySkipCeremony(), Is.True);
            yield return null;

            app.ShowCampus();
            yield return null;

            // Replay on the alternate seed: new copy, new toys.
            Assert.That(app.ShowPartyStation(CareerQuestCatalog.AiLabId), Is.True);
            yield return null;
            var alternate = aiLab.AlternateSeeds[0];
            Assert.That(controller.ChooseSeed(alternate.SeedId), Is.True);
            yield return MountFrames();

            Assert.That(TmpText("PartyStationPrompt"), Is.EqualTo(aiLab.ResolvePrompt(alternate)));
            Assert.That(controller.PieceFor("striped_sock_signals"), Is.Not.Null);
            Assert.That(controller.PieceFor("blue_fact_bubbles"), Is.Null);

            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);

            // Identity preserved: same station id, badge display name, and
            // accessory on BOTH reward events; completion count unchanged.
            Assert.That(rewardEvents.Count, Is.EqualTo(2));
            Assert.That(rewardEvents[0].SeedId, Is.EqualTo(aiLab.DefaultSeed.SeedId));
            Assert.That(rewardEvents[1].SeedId, Is.EqualTo(alternate.SeedId));
            foreach (var rewardEvent in rewardEvents)
            {
                Assert.That(rewardEvent.StationId, Is.EqualTo(CareerQuestCatalog.AiLabId));
                Assert.That(rewardEvent.AccessoryRewardId, Is.EqualTo(aiLab.AccessoryRewardId));
            }

            var best = app.Session.GetBestResult(CareerQuestCatalog.AiLabId);
            Assert.That(best.ActivityId, Is.EqualTo(CareerQuestCatalog.AiLabId));
            Assert.That(best.DisplayName, Is.EqualTo(aiLab.DisplayName));
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(1),
                "Replay never inflates the unique completion count (AE2).");

            Object.DestroyImmediate(appObject);
        }

        // ------------------------------------------------------------------
        // Helpers (PartyStationRoboticsPlayModeTests conventions)
        // ------------------------------------------------------------------

        private static PartyStationController PrepareController(GameObject appObject)
        {
            var controller = appObject.GetComponent<PartyStationController>()
                ?? appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false; // deterministic clock
            controller.QuickPacing = true;
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

        private static string TmpText(string objectName)
        {
            var gameObject = GameObject.Find(objectName);
            Assert.That(gameObject, Is.Not.Null, $"{objectName} should exist.");
            return gameObject.GetComponent<TextMeshProUGUI>().text;
        }
    }
}
