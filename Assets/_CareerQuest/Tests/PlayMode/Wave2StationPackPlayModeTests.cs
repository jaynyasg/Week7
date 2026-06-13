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
    /// U10 Wave 2 station pack: Weather Lab Rescue, Spaceport Pilot, Newsroom
    /// Story Sprint, and Green City Builder play end to end through the SAME
    /// shared PartyStationController and ToyInteractionKit the first six use —
    /// no bespoke per-station path. Proves each Wave 2 pattern in the real scene
    /// lifecycle (the rule-level golden/boundary coverage already lives in the
    /// U3 EditMode ToyPatternRulesTests, so these stay lean and scene-focused):
    ///
    ///   - Weather Lab Rescue: SequenceCards "predict + protect" — order the
    ///     forecast clue, then place the shelter tools; out-of-order is gentle.
    ///   - Spaceport Pilot:    SequenceCards launch/orbit/deliver/land order.
    ///   - Newsroom Story Sprint: ComposeSet fact-check (any-order verified
    ///     facts to the story), with source-safe copy.
    ///   - Green City Builder: BalanceMeters with TWO meters — both meter dials
    ///     must tap pointer-first into the green band; one out = not complete,
    ///     and there is no harsh fail (dials stay re-adjustable).
    ///
    /// Plus the breadth loop: every Wave 2 station completes its default seed,
    /// emits exactly one normal result + reward event, returns to campus, and
    /// offers default + alternate seeds on replay (gallery/reveal compatible).
    /// </summary>
    public class Wave2StationPackPlayModeTests
    {
        /// <summary>The Wave 2 pack with each station's pattern proof (plan U10).</summary>
        private static readonly (string StationId, ToyPatternId Pattern)[] Wave2Stations =
        {
            (CareerQuestCatalog.WeatherLabId, ToyPatternId.SequenceCards),
            (CareerQuestCatalog.SpaceportId, ToyPatternId.SequenceCards),
            (CareerQuestCatalog.NewsroomId, ToyPatternId.ComposeSet),
            (CareerQuestCatalog.GreenCityId, ToyPatternId.BalanceMeters)
        };

        [SetUp]
        public void SetUp()
        {
            // Test isolation (SceneWipe leak history): stale roots from earlier
            // suites must not pollute object lookups or the result counts.
            PlayModeSceneScrubber.DestroyStaleAppRoots();
        }

        [UnityTest]
        public IEnumerator EveryWave2StationCompletesEmitsOneResultReturnsAndOffersReplaySeeds()
        {
            var appObject = new GameObject("wave2-pack-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            var rewardEvents = new List<StationRewardEvent>();
            controller.RewardEventEmitted += rewardEvents.Add;

            for (var index = 0; index < Wave2Stations.Length; index++)
            {
                var (stationId, expectedPattern) = Wave2Stations[index];
                var definition = PartyStationDefinitions.GetById(stationId);

                // Generic station-id routing into the shared controller, with the
                // station's authored Wave 2 pattern — never a bespoke room.
                Assert.That(app.ShowPartyStation(stationId), Is.True, stationId);
                yield return MountFrames();
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.PartyStation), stationId);
                Assert.That(app.CurrentStationId, Is.EqualTo(stationId), stationId);
                Assert.That(controller.Pattern.Pattern, Is.EqualTo(expectedPattern), stationId);
                Assert.That(controller.Seed.SeedId, Is.EqualTo(definition.DefaultSeed.SeedId),
                    $"{stationId}: first play uses the default seed.");

                // Complete through the REAL shared seam (golden actions ride
                // TrySubmitDrop; the 2-meter Green City build included).
                Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True, stationId);

                var result = app.Session.GetBestResult(stationId);
                Assert.That(result, Is.Not.Null, stationId);
                Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree), stationId);
                Assert.That(result.Summary, Is.EqualTo(definition.DefaultSeed.ResultSummary), stationId);
                Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(index + 1),
                    $"{stationId}: each station emits exactly one normal result.");
                Assert.That(rewardEvents.Count, Is.EqualTo(index + 1), stationId);
                Assert.That(rewardEvents[index].StationId, Is.EqualTo(stationId));
                Assert.That(rewardEvents[index].AccessoryRewardId, Is.EqualTo(definition.AccessoryRewardId),
                    $"{stationId}: one core accessory per station (gallery/reveal compatible).");

                // The room lifecycle owns the ceremony (reveal compatibility);
                // skip it and return to campus through the normal path.
                Assert.That(GameObject.Find("CeremonyOverlay"), Is.Not.Null, stationId);
                yield return new WaitForSecondsRealtime(CeremonyController.SkipDelaySeconds + 0.25f);
                Assert.That(app.TrySkipCeremony(), Is.True, stationId);
                yield return null;

                app.ShowCampus();
                yield return null;
                Assert.That(app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus), stationId);

                // Replay: re-entry offers default AND alternate seeds.
                Assert.That(app.ShowPartyStation(stationId), Is.True, $"{stationId}: re-enter for replay");
                yield return null;
                Assert.That(controller.IsSeedChoiceOpen, Is.True, $"{stationId}: replay offers a seed choice");
                Assert.That(
                    GameObject.Find($"{PartyStationController.SeedChoiceButtonPrefix}{definition.AlternateSeeds[0].SeedId}"),
                    Is.Not.Null,
                    $"{stationId}: replay exposes the alternate seed.");

                app.ShowCampus();
                yield return null;
            }

            // Every Wave 2 station completed once; the count never inflates.
            Assert.That(app.Session.UniqueCompletedGames, Is.EqualTo(Wave2Stations.Length));

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// Weather Lab Rescue proves "predict + protect": the forecast clue is
        /// sequenced first, then the shelter tools follow in authored order. A
        /// shelter tool dropped before its turn bounces as a gentle hint, never
        /// a harsh fail — and the safe weather/emergency copy drives the beat.
        /// </summary>
        [UnityTest]
        public IEnumerator WeatherLabRescueProvesSequenceThenProtect()
        {
            var appObject = new GameObject("wave2-weather-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.WeatherLabId);
            yield return MountFrames();

            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.WeatherLabId);
            var seed = definition.DefaultSeed;
            var rules = controller.Pattern.Rules;

            // Predict first: the forecast clue is the head of the sequence.
            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("forecast_tiles"));
            Assert.That(rules.ExpectedTargetFor("forecast_tiles"), Is.EqualTo(ToyPatternRules.SequenceTargetId));

            // Protect before predicting -> gentle bounce + hint copy (the drop
            // seam maps both wrong-target and out-of-order rejects to the same
            // gentle RejectedWrongSlot outcome — never a harsh fail).
            Assert.That(controller.TrySubmitDrop("shelter_flag", ToyPatternRules.SequenceTargetId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.HintLine));
            Assert.That(controller.IsToyAccepted("shelter_flag"), Is.False);

            // Order the forecast, then place the shelter tools in turn — the
            // whole protect chain rides the real drop seam to completion.
            Assert.That(controller.TrySubmitDrop("forecast_tiles", ToyPatternRules.SequenceTargetId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);

            Assert.That(TmpText(StationGuideView.LineTextName), Is.EqualTo(seed.SuccessLine));
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.WeatherLabId), Is.Not.Null);

            // Pretend-play safety: every weather/emergency line on both seeds
            // passes the shared safety scan (no disaster/danger/fear words).
            foreach (var weatherSeed in definition.Seeds)
            {
                foreach (var line in CopyLines(weatherSeed))
                {
                    Assert.That(PartyStationValidator.CheckCopySafety(line, weatherSeed.SeedId), Is.Empty, line);
                }
            }

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// Spaceport Pilot proves SequenceCards: launch, orbit, deliver, and
        /// land must arrive in the authored mission order through the real drop
        /// seam — a step out of turn bounces gently and the next step is named.
        /// </summary>
        [UnityTest]
        public IEnumerator SpaceportPilotProvesSequenceCardsNavigation()
        {
            var appObject = new GameObject("wave2-spaceport-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.SpaceportId);
            yield return MountFrames();

            var rules = controller.Pattern.Rules;

            // The mission sequence is launch_checklist -> fuel_bead ->
            // snack_crate -> orbit_arrow, all onto the one sequence target.
            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("launch_checklist"));

            // Right target, wrong time -> gentle bounce (out-of-order maps to
            // the same RejectedWrongSlot outcome through the drop seam).
            Assert.That(controller.TrySubmitDrop("orbit_arrow", ToyPatternRules.SequenceTargetId),
                Is.EqualTo(DropSubmitResult.RejectedWrongSlot));
            Assert.That(controller.IsToyAccepted("orbit_arrow"), Is.False);

            // Step the mission in order — each step accepts as it comes up.
            foreach (var stepId in new[] { "launch_checklist", "fuel_bead", "snack_crate", "orbit_arrow" })
            {
                Assert.That(rules.NextExpectedObjectId, Is.EqualTo(stepId), $"next step is {stepId}");
                Assert.That(controller.TrySubmitDrop(stepId, ToyPatternRules.SequenceTargetId),
                    Is.EqualTo(DropSubmitResult.Accepted), stepId);
            }

            var result = app.Session.GetBestResult(CareerQuestCatalog.SpaceportId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// Newsroom Story Sprint proves fact-check compose/match: verified facts
        /// (who/what/where + the quote clue) compose onto the story in any order,
        /// the fact-check stamp is a reaction poke, and the source-safe copy
        /// frames it as checking facts, never anything pressuring or unsafe.
        /// </summary>
        [UnityTest]
        public IEnumerator NewsroomStorySprintProvesFactCheckCompose()
        {
            var appObject = new GameObject("wave2-newsroom-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.NewsroomId);
            yield return MountFrames();

            var definition = PartyStationDefinitions.GetById(CareerQuestCatalog.NewsroomId);
            var rules = controller.Pattern.Rules;

            // ComposeSet: every verified fact lands on the one compose target.
            foreach (var factId in new[] { "who_card", "what_photo", "where_map", "quote_recorder" })
            {
                Assert.That(rules.ExpectedTargetFor(factId), Is.EqualTo(ToyPatternRules.ComposeTargetId), factId);
            }

            // Facts compose in ANY order (a fact-check sprint, not a fixed line).
            Assert.That(controller.TrySubmitDrop("where_map", ToyPatternRules.ComposeTargetId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.TrySubmitDrop("who_card", ToyPatternRules.ComposeTargetId),
                Is.EqualTo(DropSubmitResult.Accepted));

            // The fact-check stamp is a reaction toy: it pokes, never progresses.
            Assert.That(controller.TrySubmitDrop("fact_check_stamp", ToyPatternRules.ComposeTargetId),
                Is.EqualTo(DropSubmitResult.Accepted));
            Assert.That(controller.IsToyAccepted("fact_check_stamp"), Is.False,
                "The fact-check stamp reacts but is not part of the verified-fact chain.");

            // Finish the remaining verified facts -> one safe headline result.
            Assert.That(controller.TryCompleteWithGoldenSequence(), Is.True);
            var result = app.Session.GetBestResult(CareerQuestCatalog.NewsroomId);
            Assert.That(result, Is.Not.Null);
            Assert.That(result.Summary, Is.EqualTo(definition.DefaultSeed.ResultSummary));

            // Source-safe copy: both seeds pass the shared safety scan.
            foreach (var newsSeed in definition.Seeds)
            {
                foreach (var line in CopyLines(newsSeed))
                {
                    Assert.That(PartyStationValidator.CheckCopySafety(line, newsSeed.SeedId), Is.Empty, line);
                }
            }

            Object.DestroyImmediate(appObject);
        }

        /// <summary>
        /// Green City Builder proves BalanceMeters with TWO meter constraints
        /// and no harsh failure state, pointer-first: place the four city pieces
        /// (which pull both meters down), then tap EACH of the two meter dials
        /// into the green band with no keyboard. One meter out of green leaves
        /// the station incomplete; both in green completes it; the dials stay
        /// re-adjustable throughout (never a fail state).
        /// </summary>
        [UnityTest]
        public IEnumerator GreenCityBuilderProvesTwoMeterBalancePointerFirstWithNoHarshFail()
        {
            var appObject = new GameObject("wave2-greencity-test");
            var app = appObject.AddComponent<CareerQuestApp>();
            yield return null;

            var controller = PrepareController(appObject);
            app.ShowPartyStation(CareerQuestCatalog.GreenCityId);
            yield return MountFrames();

            var rules = controller.Pattern.Rules;

            // Two meters, two pointer-first dials — the headline 2-meter case.
            Assert.That(rules.MeterObjectIds, Is.EquivalentTo(new[] { "budget_meter", "happy_meter" }));

            // Place the four city pieces onto the shared build spot (pointer
            // path: drag the piece onto the build zone through the drag shell).
            var buildZone = controller.ZoneFor(ToyPatternRules.BuildTargetId);
            Assert.That(buildZone, Is.Not.Null);
            foreach (var pieceId in new[] { "solar_tile", "garden_block", "bike_path", "water_wheel" })
            {
                var piece = controller.PieceFor(pieceId);
                Assert.That(piece.BeginDragProgrammatic(), Is.True, pieceId);
                piece.DragTo(buildZone.transform.position);
                piece.EndDragAt(buildZone.transform.position);
                Assert.That(controller.IsToyAccepted(pieceId), Is.True, pieceId);
            }

            // All pieces placed pulled BOTH meters below the green band, so the
            // station is not complete and the status hands off to the meters.
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.GreenCityId), Is.Null,
                "Both meters start below green after placement — completion is gated.");

            var budgetWidget = MeterWidget(controller, "budget_meter");
            var happyWidget = MeterWidget(controller, "happy_meter");
            Assert.That(budgetWidget, Is.Not.Null, "Each meter zone mounts a pointer-first widget (R19).");
            Assert.That(happyWidget, Is.Not.Null, "Each meter zone mounts a pointer-first widget (R19).");
            Assert.That(budgetWidget.IsInGreen, Is.False);
            Assert.That(happyWidget.IsInGreen, Is.False);

            // Tap ONLY the budget dial into green first — one meter in, one out
            // must NOT complete the station (the two-meter constraint).
            TapIntoGreen(budgetWidget);
            Assert.That(budgetWidget.IsInGreen, Is.True);
            Assert.That(happyWidget.IsInGreen, Is.False);
            Assert.That(app.Session.GetBestResult(CareerQuestCatalog.GreenCityId), Is.Null,
                "One meter green and one out leaves the station incomplete (no harsh fail).");

            // No harsh fail: the budget dial stays re-adjustable even while
            // green — tapping past the top wraps back and is still accepted.
            Assert.That(budgetWidget.Tap(), Is.True, "Dials stay re-adjustable — never a locked fail state.");

            // Bring the budget dial back into green, then the happy dial too —
            // both in green completes the station, all pointer-first.
            TapIntoGreen(budgetWidget);
            TapIntoGreen(happyWidget);
            Assert.That(budgetWidget.IsInGreen, Is.True);
            Assert.That(happyWidget.IsInGreen, Is.True);

            var result = app.Session.GetBestResult(CareerQuestCatalog.GreenCityId);
            Assert.That(result, Is.Not.Null, "Both meters in the green band completes the station.");
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));

            Object.DestroyImmediate(appObject);
        }

        // ------------------------------------------------------------------
        // Helpers (FirstSixStationPackPlayModeTests conventions)
        // ------------------------------------------------------------------

        private static PartyStationController PrepareController(GameObject appObject)
        {
            var controller = appObject.GetComponent<PartyStationController>()
                ?? appObject.AddComponent<PartyStationController>();
            controller.AutoTick = false; // deterministic clock
            controller.QuickPacing = true; // skip the intro hold; scoring unchanged
            return controller;
        }

        private static StationMeterWidget MeterWidget(PartyStationController controller, string meterId)
        {
            var zone = controller.ZoneFor(ToyPatternRules.MeterTargetPrefix + meterId);
            return zone != null ? zone.GetComponent<StationMeterWidget>() : null;
        }

        /// <summary>Taps a meter dial until it sits in the green band (always reachable by wrap).</summary>
        private static void TapIntoGreen(StationMeterWidget widget)
        {
            for (var safety = 0; safety < 16 && !widget.IsInGreen; safety++)
            {
                Assert.That(widget.Tap(), Is.True, "A meter dial tap is never rejected (no fail state).");
            }

            Assert.That(widget.IsInGreen, Is.True, "The green band is always reachable by tapping.");
        }

        private static IEnumerable<string> CopyLines(PartyStationSeedDefinition seed)
        {
            return new[]
            {
                seed.IntroLine, seed.HintLine, seed.EscalationHintLine, seed.SuccessLine,
                seed.RewardPreviewLine, seed.ResultSummary, seed.NpcReaction
            };
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
