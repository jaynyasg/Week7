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
        public void DragToSlotRejectsWrongSlotUnknownAndEmptySubmissions()
        {
            var rules = RulesFor(CareerQuestCatalog.RoboticsGarageId);

            // Wrong slot: a chain toy only lands on its own slot.
            var wrongSlot = rules.Submit(new ToyAction("battery_toast", ToyPatternRules.SlotTargetPrefix + "wheel_sandwich"));
            Assert.That(wrongSlot.RejectReason, Is.EqualTo(ToyRejectReason.WrongTarget));

            // Unknown and empty toys bounce as unknown.
            Assert.That(rules.Submit(new ToyAction("mystery_widget", "slot.mystery_widget")).RejectReason,
                Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(rules.Submit(new ToyAction(null, null)).RejectReason,
                Is.EqualTo(ToyRejectReason.UnknownObject));
            Assert.That(rules.Submit(default).RejectReason, Is.EqualTo(ToyRejectReason.UnknownObject));

            Assert.That(rules.AcceptedCount, Is.EqualTo(0), "Rejects never advance progress.");
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
            var rules = RulesFor(CareerQuestCatalog.AiLabId);

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
            var rules = RulesFor(CareerQuestCatalog.CommunityKitchenId);

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
        public void SequenceCardsEnforcesAuthoredOrder()
        {
            var rules = RulesFor(CareerQuestCatalog.SpaceportId);

            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("launch_checklist"));

            // Right target, wrong time -> out of order, never harsh.
            var early = rules.Submit(new ToyAction("fuel_bead", ToyPatternRules.SequenceTargetId));
            Assert.That(early.RejectReason, Is.EqualTo(ToyRejectReason.OutOfOrder));

            Assert.That(rules.Submit(new ToyAction("launch_checklist", ToyPatternRules.SequenceTargetId)).IsAccepted, Is.True);
            Assert.That(rules.NextExpectedObjectId, Is.EqualTo("fuel_bead"));
            Assert.That(rules.Submit(new ToyAction("fuel_bead", ToyPatternRules.SequenceTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("snack_crate", ToyPatternRules.SequenceTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("orbit_arrow", ToyPatternRules.SequenceTargetId)).StationCompleted, Is.True);
        }

        [Test]
        public void ComposeSetAcceptsAnyOrderButGatesCompletionOnTheMeter()
        {
            var rules = RulesFor(CareerQuestCatalog.MusicStudioId);

            // Layers land in any order.
            Assert.That(rules.Submit(new ToyAction("horn_burst", ToyPatternRules.ComposeTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("drum_cloud", ToyPatternRules.ComposeTargetId)).IsAccepted, Is.True);
            Assert.That(rules.Submit(new ToyAction("rain_shaker", ToyPatternRules.ComposeTargetId)).IsAccepted, Is.True);

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
            rules.ForceAccept("orbit_arrow");
            Assert.That(rules.IsAccepted("orbit_arrow"), Is.True);

            // Unknown and meter ids are ignored safely.
            rules.ForceAccept("not_a_toy");
            Assert.That(rules.AcceptedCount, Is.EqualTo(1));
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
