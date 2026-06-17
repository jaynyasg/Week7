using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U3 pure-rule coverage: every supported toy pattern accepts its golden
    /// action sequence and rejects unknown toys, wrong targets/bins, occupied
    /// toys, out-of-order plays, and locked (completed) submissions — all
    /// without a scene, exactly like the host validation path runs them.
    /// </summary>
    public class ToyPatternRulesTests
    {
        private static ToyPatternRules RulesFor(string stationId, bool alternate = false)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            var seed = alternate ? definition.AlternateSeeds[0] : definition.DefaultSeed;
            return ToyPatternRules.ForSeed(definition, seed);
        }

        private static void DriveGolden(ToyPatternRules rules)
        {
            foreach (var action in rules.BuildGoldenActionSequence())
            {
                var result = rules.Submit(action);
                Assert.That(result.Kind, Is.EqualTo(ToySubmissionKind.Accepted),
                    $"Golden action {action.ObjectId} -> {action.TargetId} should be accepted.");
            }
        }

        [Test]
        public void GoldenSequenceCompletesEveryStationSeed()
        {
            foreach (var definition in PartyStationDefinitions.All)
            {
                foreach (var seed in definition.Seeds)
                {
                    var rules = ToyPatternRules.ForSeed(definition, seed);
                    Assert.That(rules.RequiredCount, Is.GreaterThanOrEqualTo(3),
                        $"{seed.SeedId} should have a playable chain.");

                    DriveGolden(rules);

                    Assert.That(rules.Complete, Is.True,
                        $"{seed.SeedId} ({definition.Pattern}) should complete on its golden sequence.");
                }
            }
        }

        [Test]
        public void ShootTargetRejectsWrongGoalUnknownAndEmptySubmissions()
        {
            // Robotics is the ShootTarget proof (design-review #3): every shot
            // lands on ONE shared goal (the rescue spot), so a shot aimed at any
            // other target bounces gently as a wrong target — the spatial "did it
            // reach the goal?" miss lives in the launcher, not the rules.
            var rules = RulesFor(CareerQuestCatalog.RoboticsGarageId);

            Assert.That(rules.ExpectedTargetFor("battery_toast"), Is.EqualTo(ToyPatternRules.GoalTargetId));

            // A shot at anything but the shared goal -> wrong target.
            var wrongGoal = rules.Submit(new ToyAction("battery_toast", ToyPatternRules.SlotTargetPrefix + "wheel_sandwich"));
            Assert.That(wrongGoal.RejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));

            // Unknown and empty toys bounce as unknown.
            Assert.That(rules.Submit(new ToyAction("mystery_widget", ToyPatternRules.GoalTargetId)).RejectReason,
                Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(rules.Submit(new ToyAction(null, null)).RejectReason,
                Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(rules.Submit(default).RejectReason, Is.EqualTo(ToyRejectReason.UnknownObject));

            Assert.That(rules.AcceptedCount, Is.EqualTo(0), "Rejects never advance progress.");
        }

        [Test]
        public void ShootTargetLandsShotsInGoalInAnyOrder()
        {
            // The ShootTarget signature (design-review #3): unlike SequenceCards/
            // TracePath (strict order) or DragToSlot (one slot per toy), every
            // chain toy launches onto the SAME shared goal in ANY order, and the
            // station completes once they have all landed.
            var rules = RulesFor(CareerQuestCatalog.RoboticsGarageId);

            // Every chain toy resolves to the one shared goal target.
            foreach (var objectId in rules.DraggableObjectIds)
            {
                Assert.That(rules.ExpectedTargetFor(objectId), Is.EqualTo(ToyPatternRules.GoalTargetId), objectId);
            }

            // Launch out of authored order — any order is accepted (no OutOfOrder).
            Assert.That(rules.Submit(new ToyAction("sensor_sticker", ToyPatternRules.GoalTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("battery_toast", ToyPatternRules.GoalTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("route_cards", ToyPatternRules.GoalTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Complete, Is.False, "Still one shot short of the goal.");

            // The last shot lands and completes the rescue.
            Assert.That(rules.Submit(new ToyAction("wheel_sandwich", ToyPatternRules.GoalTargetId)).StationCompleted, Is.True);

            // The rescue flag is a reaction poke — it acknowledges, never advances.
            var freshRules = RulesFor(CareerQuestCatalog.RoboticsGarageId);
            Assert.That(freshRules.Submit(new ToyAction("rescue_flag", ToyPatternRules.GoalTargetId)).Kind,
                Is.EqualTo(ToySubmissionKind.ReactionOnly));
        }

        [Test]
        public void DeduceAnswerCrossesOutFalseCandidatesAndProtectsTheAnswer()
        {
            // AI Lab is now the DeduceAnswer proof: the wrong sort rules
            // (CoreTask) are the eliminate-chain crossed out by tapping; the one
            // right rule (Clue) is OUT of the chain - tapping it bounces.
            var rules = RulesFor(CareerQuestCatalog.AiLabId);

            Assert.That(rules.DraggableObjectIds,
                Is.EquivalentTo(new[] { "size_rule", "loud_rule", "random_rule" }));
            Assert.That(rules.ExpectedTargetFor("size_rule"),
                Is.EqualTo(ToyPatternRules.CrossTargetPrefix + "size_rule"));
            Assert.That(rules.ExpectedTargetFor("color_rule"), Is.Null,
                "The right rule (Clue answer) has no cross zone - it survives.");

            var protectAnswer = rules.Submit(new ToyAction("color_rule", ToyPatternRules.CrossTargetPrefix + "color_rule"));
            Assert.That(protectAnswer.RejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));
            Assert.That(rules.IsAccepted("color_rule"), Is.False);

            Assert.That(rules.Submit(new ToyAction("random_rule", rules.ExpectedTargetFor("random_rule"))).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("size_rule", rules.ExpectedTargetFor("size_rule"))).IsAccepted, Is.True);
            Assert.That(rules.Complete, Is.False, "One wrong rule still stands.");
            Assert.That(rules.Submit(new ToyAction("loud_rule", rules.ExpectedTargetFor("loud_rule"))).StationCompleted, Is.True);

            var fresh = RulesFor(CareerQuestCatalog.AiLabId);
            Assert.That(fresh.Submit(new ToyAction("test_button", ToyPatternRules.CrossTargetPrefix + "test_button")).Kind,
                Is.EqualTo(ToySubmissionKind.ReactionOnly));
        }

        [Test]
        public void OccupiedToyRejectsAndCompletionLocksFurtherSubmissions()
        {
            var rules = RulesFor(CareerQuestCatalog.RoboticsGarageId);

            var first = rules.Submit(new ToyAction("battery_toast", rules.ExpectedTargetFor("battery_toast")));
            Assert.That(first.IsAccepted, Is.True);

            var duplicate = rules.Submit(new ToyAction("battery_toast", rules.ExpectedTargetFor("battery_toast")));
            Assert.That(duplicate.RejectReason, Is.EqualTo(ToyRejectReason.AlreadyAccepted));

            DriveGoldenRemainder(rules);
            Assert.That(rules.Complete, Is.True);

            // Completion idempotence: locked submissions change nothing.
            var acceptedBefore = rules.AcceptedCount;
            var locked = rules.Submit(new ToyAction("sensor_sticker", rules.ExpectedTargetFor("sensor_sticker")));
            Assert.That(locked.RejectReason, Is.EqualTo(ToyRejectReason.Locked));
            Assert.That(rules.AcceptedCount, Is.EqualTo(acceptedBefore));
            Assert.That(rules.Complete, Is.True);
        }

        [Test]
        public void SortToBinDerivesTraitBinsAndRejectsWrongBin()
        {
            // SortToBin is a supported pattern with no shipped station after AI
            // Lab moved to DeduceAnswer, so this drives a synthetic seed directly
            // to keep the trait-bin derivation + wrong-bin reject logic covered.
            var rules = new ToyPatternRules(ToyPatternId.SortToBin, new[]
            {
                new PartyStationObjectDefinition("blue_fact_bubbles", "Blue Fact Bubbles", PartyStationObjectRole.CoreTask, "", "", "react.pop", "Reasoning"),
                new PartyStationObjectDefinition("pink_guess_bubbles", "Pink Guess Bubbles", PartyStationObjectRole.CoreTask, "", "", "react.pop", "Creativity"),
                new PartyStationObjectDefinition("training_tray", "Training Tray", PartyStationObjectRole.CoreTask, "", "", "react.glow", "Science")
            });

            Assert.That(rules.ExpectedTargetFor("blue_fact_bubbles"), Is.EqualTo("bin.reasoning"));
            Assert.That(rules.ExpectedTargetFor("training_tray"), Is.EqualTo("bin.science"));

            // Wrong bin bounces gently; the right bin accepts.
            Assert.That(rules.Submit(new ToyAction("blue_fact_bubbles", "bin.science")).RejectReason,
                Is.EqualTo(ToyRejectReason.WrongTarget));
            Assert.That(rules.Submit(new ToyAction("blue_fact_bubbles", "bin.reasoning")).IsAccepted, Is.True);
        }

        [Test]
        public void PickMatchingTrioReadsCluesBeforeTheCoreTrio()
        {
            // Community Kitchen moved to PourToLine, so this drives a synthetic
            // PickMatchingTrio seed directly to keep the clue-first trio logic
            // covered (no shipped station uses PickMatchingTrio now).
            var rules = new ToyPatternRules(ToyPatternId.PickMatchingTrio, new[]
            {
                new PartyStationObjectDefinition("recipe_card", "Recipe Card", PartyStationObjectRole.Clue, "", "", "react.glow", "Reasoning"),
                new PartyStationObjectDefinition("veggie_clue", "Veggie Clue", PartyStationObjectRole.CoreTask, "", "", "react.pop", "Helping"),
                new PartyStationObjectDefinition("spice_jar", "Spice Jar", PartyStationObjectRole.CoreTask, "", "", "react.sparkle", "Creativity"),
                new PartyStationObjectDefinition("serving_bowl", "Serving Bowl", PartyStationObjectRole.CoreTask, "", "", "react.bounce", "Helping")
            });

            // Core toy before the recipe clue is read -> gentle out-of-order bounce.
            var early = rules.Submit(new ToyAction("veggie_clue", ToyPatternRules.TrioTrayTargetId));
            Assert.That(early.RejectReason, Is.EqualTo(ToyRejectReason.OutOfOrder));

            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("recipe_card"));
            Assert.That(rules.Submit(new ToyAction("recipe_card", ToyPatternRules.TrioTrayTargetId)).IsAccepted, Is.True);

            // Core trio lands in any order once the clue is read.
            Assert.That(rules.Submit(new ToyAction("serving_bowl", ToyPatternRules.TrioTrayTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("veggie_clue", ToyPatternRules.TrioTrayTargetId)).IsAccepted, Is.True);
            var last = rules.Submit(new ToyAction("spice_jar", ToyPatternRules.TrioTrayTargetId));
            Assert.That(last.IsAccepted, Is.True);
            Assert.That(last.StationCompleted, Is.True);
        }

        [Test]
        public void TracePathTracesWaypointsInOrder()
        {
            // Weather Lab is now the TracePath proof: the chain is traced in
            // strict order, each waypoint on its OWN positioned zone.
            var rules = RulesFor(CareerQuestCatalog.WeatherLabId);

            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("forecast_tiles"));
            Assert.That(rules.ExpectedTargetFor("umbrella_sign"),
                Is.EqualTo(ToyPatternRules.WaypointTargetPrefix + "umbrella_sign"));

            var early = rules.Submit(new ToyAction("umbrella_sign", rules.ExpectedTargetFor("umbrella_sign")));
            Assert.That(early.RejectReason, Is.EqualTo(ToyRejectReason.OutOfOrder));

            var wrongZone = rules.Submit(new ToyAction("forecast_tiles", rules.ExpectedTargetFor("umbrella_sign")));
            Assert.That(wrongZone.RejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));

            foreach (var stepId in new[] { "forecast_tiles", "umbrella_sign", "route_cones" })
            {
                Assert.That(rules.NextExpectedObjectId, Is.EqualTo(stepId));
                Assert.That(rules.Submit(new ToyAction(stepId, rules.ExpectedTargetFor(stepId))).IsAccepted, Is.True);
            }

            Assert.That(rules.Submit(new ToyAction("shelter_flag", rules.ExpectedTargetFor("shelter_flag"))).StationCompleted, Is.True);
        }

        [Test]
        public void RhythmTapAcceptsAnyOrderButGatesCompletionOnTheMeter()
        {
            var rules = RulesFor(CareerQuestCatalog.MusicStudioId);

            // Beats land on the shared beat target in any order.
            Assert.That(rules.Submit(new ToyAction("horn_burst", ToyPatternRules.BeatTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("drum_cloud", ToyPatternRules.BeatTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("rain_shaker", ToyPatternRules.BeatTargetId)).IsAccepted, Is.True);

            // The tempo dial starts outside the green band and gates completion.
            Assert.That(rules.Complete, Is.False);
            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("tempo_dial"));

            var lockTempo = rules.Submit(new ToyAction(
                "tempo_dial",
                ToyPatternRules.MeterTargetPrefix + "tempo_dial",
                ToyPatternRules.MeterGreenTarget));
            Assert.That(lockTempo.IsAccepted, Is.True);
            Assert.That(lockTempo.StationCompleted, Is.True);
        }

        [Test]
        public void ComposeSetAcceptsChainInAnyOrder()
        {
            // Game Studio is now the ComposeSet proof: every chain toy lands on
            // the shared compose target in any order.
            var rules = RulesFor(CareerQuestCatalog.GameStudioId);

            foreach (var objectId in rules.DraggableObjectIds)
            {
                Assert.That(rules.ExpectedTargetFor(objectId), Is.EqualTo(ToyPatternRules.ComposeTargetId), objectId);
            }

            for (var i = rules.DraggableObjectIds.Count - 1; i >= 0; i--)
            {
                var objectId = rules.DraggableObjectIds[i];
                Assert.That(rules.Submit(new ToyAction(objectId, ToyPatternRules.ComposeTargetId)).IsAccepted, Is.True, objectId);
            }
            Assert.That(rules.Complete, Is.True);
        }

        [Test]
        public void MatchAndCareMatchesCluesToTheirMarksBeforeCare()
        {
            var rules = RulesFor(CareerQuestCatalog.VetClinicId);

            // The clue card points at the care tool (TargetId-driven mark zone).
            Assert.That(rules.ExpectedTargetFor("symptom_cards"), Is.EqualTo("mark.care_tool"));

            // Care toy before the clue is matched -> out of order.
            var early = rules.Submit(new ToyAction("comfort_blanket", ToyPatternRules.CareTargetId));
            Assert.That(early.RejectReason, Is.EqualTo(ToyRejectReason.OutOfOrder));

            // Clue on the wrong mark -> wrong target.
            Assert.That(rules.Submit(new ToyAction("symptom_cards", ToyPatternRules.CareTargetId)).RejectReason,
                Is.EqualTo(ToyRejectReason.WrongTarget));

            Assert.That(rules.Submit(new ToyAction("symptom_cards", "mark.care_tool")).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("comfort_blanket", ToyPatternRules.CareTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("temperature_sticker", ToyPatternRules.CareTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("care_tool", ToyPatternRules.CareTargetId)).StationCompleted, Is.True);
        }

        [Test]
        public void BalanceMetersShiftsOnPlacementAndChecksGreenBoundaries()
        {
            var rules = RulesFor(CareerQuestCatalog.GreenCityId);

            foreach (var objectId in new[] { "solar_tile", "garden_block", "bike_path", "water_wheel" })
            {
                Assert.That(rules.Submit(new ToyAction(objectId, ToyPatternRules.BuildTargetId)).IsAccepted, Is.True);
            }

            // Placements pulled the meters down — completion stays gated.
            Assert.That(rules.Complete, Is.False);
            Assert.That(rules.MeterValue("budget_meter"), Is.LessThan(ToyPatternRules.MeterGreenMin));

            // Exactly the low boundary is green.
            rules.Submit(new ToyAction("budget_meter", "meter.budget_meter", ToyPatternRules.MeterGreenMin));
            Assert.That(rules.IsMeterInGreen("budget_meter"), Is.True);

            // One under the low boundary is not green; one over the high boundary is not green.
            rules.Submit(new ToyAction("happy_meter", "meter.happy_meter", ToyPatternRules.MeterGreenMin - 1));
            Assert.That(rules.IsMeterInGreen("happy_meter"), Is.False);
            Assert.That(rules.Complete, Is.False);

            rules.Submit(new ToyAction("happy_meter", "meter.happy_meter", ToyPatternRules.MeterGreenMax + 1));
            Assert.That(rules.IsMeterInGreen("happy_meter"), Is.False);
            Assert.That(rules.Complete, Is.False);

            // Exactly the high boundary is green — and completes the station.
            var balanced = rules.Submit(new ToyAction("happy_meter", "meter.happy_meter", ToyPatternRules.MeterGreenMax));
            Assert.That(balanced.IsAccepted, Is.True);
            Assert.That(balanced.StationCompleted, Is.True);
        }

        [Test]
        public void MeterValuesClampAndStayReAdjustable()
        {
            var rules = RulesFor(CareerQuestCatalog.GreenCityId);

            rules.Submit(new ToyAction("budget_meter", "meter.budget_meter", -50));
            Assert.That(rules.MeterValue("budget_meter"), Is.EqualTo(ToyPatternRules.MeterMin));

            rules.Submit(new ToyAction("budget_meter", "meter.budget_meter", 999));
            Assert.That(rules.MeterValue("budget_meter"), Is.EqualTo(ToyPatternRules.MeterMax));

            // Re-adjustable: never occupied, never a fail state.
            Assert.That(rules.Submit(new ToyAction("budget_meter", "meter.budget_meter", 50)).IsAccepted, Is.True);
            Assert.That(rules.MeterValue("budget_meter"), Is.EqualTo(50));

            // Meter on the wrong target bounces.
            Assert.That(rules.Submit(new ToyAction("budget_meter", ToyPatternRules.BuildTargetId, 50)).RejectReason,
                Is.EqualTo(ToyRejectReason.WrongTarget));
        }

        [Test]
        public void NonChainToysReactWithoutProgressOrRejects()
        {
            var rules = RulesFor(CareerQuestCatalog.RoboticsGarageId);

            // The rescue flag is a Reaction role: it pokes, never progresses.
            var poke = rules.Submit(new ToyAction("rescue_flag", "anywhere"));
            Assert.That(poke.Kind, Is.EqualTo(ToySubmissionKind.ReactionOnly));
            Assert.That(rules.AcceptedCount, Is.EqualTo(0));

            // Pokeable repeatedly — no occupied state for reaction toys.
            Assert.That(rules.Submit(new ToyAction("rescue_flag", "anywhere")).Kind,
                Is.EqualTo(ToySubmissionKind.ReactionOnly));
        }

        [Test]
        public void ResetClearsProgressAndReturnsMetersToStart()
        {
            var rules = RulesFor(CareerQuestCatalog.MusicStudioId);
            DriveGolden(rules);
            Assert.That(rules.Complete, Is.True);

            rules.Reset();

            Assert.That(rules.AcceptedCount, Is.EqualTo(0));
            Assert.That(rules.Complete, Is.False);
            Assert.That(rules.MeterValue("tempo_dial"), Is.EqualTo(ToyPatternRules.MeterStartValue));
            DriveGolden(rules);
            Assert.That(rules.Complete, Is.True, "A reset attempt replays cleanly.");
        }

        [Test]
        public void ForceAcceptMirrorsHostProgressWithoutValidation()
        {
            var rules = RulesFor(CareerQuestCatalog.SpaceportId);

            // Clients mirror accepted shared state out of order without bouncing.
            rules.ForceAccept("moon_rover");
            Assert.That(rules.IsAccepted("moon_rover"), Is.True);

            // Unknown and meter ids are ignored safely.
            rules.ForceAccept("not_a_toy");
            Assert.That(rules.AcceptedCount, Is.EqualTo(1));
        }

        [Test]
        public void PourToLineCompletesWhenEveryPourReachesTheLine()
        {
            var rules = RulesFor(CareerQuestCatalog.CommunityKitchenId);

            foreach (var pourId in rules.DraggableObjectIds)
            {
                Assert.That(rules.ExpectedTargetFor(pourId), Is.EqualTo(ToyPatternRules.PourTargetId), pourId);
            }

            for (var i = 0; i < rules.DraggableObjectIds.Count; i++)
            {
                var pourId = rules.DraggableObjectIds[i];
                Assert.That(rules.Submit(new ToyAction(pourId, ToyPatternRules.PourTargetId)).IsAccepted, Is.True, pourId);
            }
            Assert.That(rules.Complete, Is.True);

            var fresh = RulesFor(CareerQuestCatalog.CommunityKitchenId);
            Assert.That(fresh.Submit(new ToyAction(fresh.DraggableObjectIds[0], "slot.nope")).RejectReason,
                Is.EqualTo(ToyRejectReason.WrongTarget));
        }

        [Test]
        public void WireUpConnectsEachNodeToItsPartnerInAnyOrder()
        {
            var rules = RulesFor(CareerQuestCatalog.SpaceportId);

            Assert.That(rules.ExpectedTargetFor("moon_rover"),
                Is.EqualTo(ToyPatternRules.WireTargetPrefix + "rover_dock"));
            Assert.That(rules.ExpectedTargetFor("signal_beam"),
                Is.EqualTo(ToyPatternRules.WireTargetPrefix + "dish_array"));

            Assert.That(rules.Submit(new ToyAction("moon_rover", ToyPatternRules.WireTargetPrefix + "dish_array")).RejectReason,
                Is.EqualTo(ToyRejectReason.WrongTarget));

            foreach (var nodeId in new[] { "dish_array", "moon_rover", "signal_beam", "rover_dock" })
            {
                Assert.That(rules.Submit(new ToyAction(nodeId, rules.ExpectedTargetFor(nodeId))).IsAccepted, Is.True, nodeId);
            }
            Assert.That(rules.Complete, Is.True);
        }

        [Test]
        public void ScanRevealConfirmsEachHiddenItemInAnyOrder()
        {
            var rules = RulesFor(CareerQuestCatalog.NewsroomId);

            foreach (var itemId in rules.DraggableObjectIds)
            {
                Assert.That(rules.ExpectedTargetFor(itemId),
                    Is.EqualTo(ToyPatternRules.RevealTargetPrefix + itemId), itemId);
            }

            foreach (var itemId in new[] { "hidden_label", "smudged_print", "faint_footprint", "torn_note" })
            {
                Assert.That(rules.Submit(new ToyAction(itemId, rules.ExpectedTargetFor(itemId))).IsAccepted, Is.True, itemId);
            }
            Assert.That(rules.Complete, Is.True);

            var fresh = RulesFor(CareerQuestCatalog.NewsroomId);
            Assert.That(fresh.Submit(new ToyAction("headline_stamp", ToyPatternRules.RevealTargetPrefix + "headline_stamp")).Kind,
                Is.EqualTo(ToySubmissionKind.ReactionOnly));
        }

        private static void DriveGoldenRemainder(ToyPatternRules rules)
        {
            foreach (var action in rules.BuildGoldenActionSequence())
            {
                if (!rules.IsAccepted(action.ObjectId))
                {
                    rules.Submit(action);
                }
            }
        }
    }
}
