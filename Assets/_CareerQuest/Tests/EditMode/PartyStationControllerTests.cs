using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U4 pure-logic coverage for the reusable station surface: seed
    /// selection/replay memory (PartyStationRoomState), the result contract
    /// (PartyStationController.BuildResult — station id, display name, tier,
    /// source, time/accuracy, seed summary, trait deltas), the quick-pacing
    /// scoring invariance, and the definition-driven renderer helpers
    /// (placeholder token policy, layout spacing, labels, theming). The full
    /// scene loop lives in PartyStationRoboticsPlayModeTests.
    /// </summary>
    public class PartyStationControllerTests
    {
        private static PartyStationDefinition Robotics =>
            PartyStationDefinitions.GetById(CareerQuestCatalog.RoboticsGarageId);

        // ------------------------------------------------------------------
        // Conversion boundary (U5: all four legacy optional rooms converted)
        // ------------------------------------------------------------------

        [Test]
        public void AllFourLegacyOptionalRoomsAreConvertedStations()
        {
            Assert.That(PartyStationController.ConvertedLegacyStationIds, Is.EquivalentTo(new[]
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.AiLabId,
                CareerQuestCatalog.MusicStudioId,
                CareerQuestCatalog.CommunityKitchenId
            }));
            Assert.That(PartyStationController.IsConvertedLegacyStation(CareerQuestCatalog.AiLabId), Is.True);

            // The six station-id entries never needed the legacy conversion
            // list — they route by UsesStationIdRouting.
            Assert.That(PartyStationController.IsConvertedLegacyStation(CareerQuestCatalog.VetClinicId), Is.False);
            Assert.That(CareerQuestCatalog.GetById(CareerQuestCatalog.VetClinicId).UsesStationIdRouting, Is.True);
        }

        // ------------------------------------------------------------------
        // Serving/pitch confirmation beat (U5: kitchen + game studio)
        // ------------------------------------------------------------------

        [Test]
        public void ConfirmationBeatDerivesFromServeAndPitchStationsOnly()
        {
            var kitchen = PartyStationDefinitions.GetById(CareerQuestCatalog.CommunityKitchenId);
            Assert.That(PartyStationController.ConfirmationVerbFor(kitchen), Is.EqualTo("serve"));
            Assert.That(PartyStationController.ConfirmationObjectIdFor(kitchen, kitchen.DefaultSeed),
                Is.EqualTo("kindness_swap"), "The kindness swap is the default-seed serving confirmation.");
            Assert.That(PartyStationController.ConfirmationObjectIdFor(kitchen, kitchen.AlternateSeeds[0]),
                Is.EqualTo("thank_you_stamp"), "The thank-you stamp serves the picnic seed.");

            var gameStudio = PartyStationDefinitions.GetById(CareerQuestCatalog.GameStudioId);
            Assert.That(PartyStationController.ConfirmationVerbFor(gameStudio), Is.EqualTo("pitch"));
            Assert.That(PartyStationController.ConfirmationObjectIdFor(gameStudio, gameStudio.DefaultSeed),
                Is.EqualTo("playtest_button"), "The playtest button is the default-seed pitch beat.");
            Assert.That(PartyStationController.ConfirmationObjectIdFor(gameStudio, gameStudio.AlternateSeeds[0]),
                Is.EqualTo("pitch_mic"), "The pitch mic pitches the boss-battle seed.");

            // No other station gains an accidental confirmation hold.
            foreach (var station in PartyStationDefinitions.All)
            {
                if (station.Id == kitchen.Id || station.Id == gameStudio.Id)
                {
                    continue;
                }

                Assert.That(PartyStationController.ConfirmationObjectIdFor(station, station.DefaultSeed),
                    Is.Null, station.Id);
            }
        }

        // ------------------------------------------------------------------
        // Pointer-first meter tap rule (U5 widget; R19)
        // ------------------------------------------------------------------

        [Test]
        public void MeterTapStepsWrapAndAlwaysReachTheGreenBand()
        {
            // Step from the authored start: 20 -> 35 -> 50 (green in two taps).
            Assert.That(StationMeterWidget.NextTapValue(ToyPatternRules.MeterStartValue), Is.EqualTo(35));
            Assert.That(StationMeterWidget.NextTapValue(35), Is.EqualTo(50));

            // Past the top the dial wraps to the bottom — overshooting is
            // always recoverable, never a fail state.
            Assert.That(StationMeterWidget.NextTapValue(ToyPatternRules.MeterMax), Is.EqualTo(ToyPatternRules.MeterMin));

            // From ANY value, repeated taps land inside the green band.
            for (var start = ToyPatternRules.MeterMin; start <= ToyPatternRules.MeterMax; start++)
            {
                var value = start;
                var taps = 0;
                while ((value < ToyPatternRules.MeterGreenMin || value > ToyPatternRules.MeterGreenMax) && taps < 20)
                {
                    value = StationMeterWidget.NextTapValue(value);
                    taps++;
                }

                Assert.That(value, Is.InRange(ToyPatternRules.MeterGreenMin, ToyPatternRules.MeterGreenMax),
                    $"start {start} should tap into the green band (stopped at {value}).");
            }
        }

        // ------------------------------------------------------------------
        // Seed selection + replay memory (session-scoped room state)
        // ------------------------------------------------------------------

        [Test]
        public void FirstPlayUsesTheDefaultSeedWithoutOfferingAChoice()
        {
            var state = new PartyStationRoomState();
            var session = new GameSession();

            Assert.That(state.ShouldOfferSeedChoice(Robotics, session), Is.False,
                "First play enters the default seed directly.");

            // Entering (and even abandoning) records the seed but is NOT a
            // completion — re-entry still goes straight to the default seed.
            state.RecordSeedChoice(Robotics.Id, Robotics.DefaultSeed.SeedId);
            Assert.That(state.ShouldOfferSeedChoice(Robotics, session), Is.False,
                "An abandoned attempt never unlocks the replay seed choice.");

            state.MarkCompleted(Robotics.Id);
            Assert.That(state.ShouldOfferSeedChoice(Robotics, session), Is.True,
                "A completed station offers default or alternate on replay.");
        }

        [Test]
        public void ExistingBestResultCountsAsReplay()
        {
            var state = new PartyStationRoomState();
            var session = new GameSession();
            session.RecordResult(PartyStationController.BuildResult(
                Robotics, Robotics.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 30f));

            Assert.That(state.IsReplay(Robotics.Id, session), Is.True);
            Assert.That(state.ShouldOfferSeedChoice(Robotics, session), Is.True,
                "A best result from earlier play opens the seed choice even on a fresh surface.");
        }

        [Test]
        public void SeedChoiceMemoryTracksTheLastChosenSeed()
        {
            var state = new PartyStationRoomState();
            var alternate = Robotics.AlternateSeeds[0];

            Assert.That(state.SelectedSeedId(Robotics.Id), Is.Null);

            state.RecordSeedChoice(Robotics.Id, Robotics.DefaultSeed.SeedId);
            Assert.That(state.SelectedSeedId(Robotics.Id), Is.EqualTo(Robotics.DefaultSeed.SeedId));

            state.RecordSeedChoice(Robotics.Id, alternate.SeedId);
            Assert.That(state.SelectedSeedId(Robotics.Id), Is.EqualTo(alternate.SeedId));
        }

        [Test]
        public void NetworkSeedHelpersAreNullSafeOffline()
        {
            Assert.That(PartyStationRoomState.AdoptNetworkSeed(Robotics, null), Is.Null);
            Assert.DoesNotThrow(() => PartyStationRoomState.HostBeginOrJoin(null, Robotics.Id, Robotics.DefaultSeed.SeedId));
        }

        // ------------------------------------------------------------------
        // Result contract (R9/R10)
        // ------------------------------------------------------------------

        [Test]
        public void BuildResultCarriesTheFullStationContract()
        {
            var seed = Robotics.DefaultSeed;
            var result = PartyStationController.BuildResult(
                Robotics, seed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f);

            Assert.That(result.ActivityId, Is.EqualTo(Robotics.Id), "Station id IS the activity id.");
            Assert.That(result.DisplayName, Is.EqualTo(Robotics.DisplayName));
            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Degree));
            Assert.That(result.Source, Is.EqualTo(ResultSource.Solo));
            Assert.That(result.Summary, Is.EqualTo(seed.ResultSummary), "Summary comes from the seed data, never authored code.");
            Assert.That(result.Accuracy, Is.EqualTo(1f).Within(0.001f));
            Assert.That(result.TimeRemaining, Is.EqualTo(PartyStationController.PacingBudgetSeconds - 12f).Within(0.001f));
            Assert.That(result.TraitDeltas, Is.EqualTo(Robotics.TraitDeltas.ToList()),
                "Trait deltas come straight from the station definition.");
        }

        [Test]
        public void WrongAttemptsLowerAccuracyButTheFloorStaysGentle()
        {
            // Robotics default seed: 4 chain toys, no meters.
            var clean = PartyStationController.BuildResult(
                Robotics, Robotics.DefaultSeed, ResultSource.Solo, true, wrongAttempts: 0, playElapsedSeconds: 0f);
            var messy = PartyStationController.BuildResult(
                Robotics, Robotics.DefaultSeed, ResultSource.Solo, true, wrongAttempts: 4, playElapsedSeconds: 0f);

            Assert.That(clean.Accuracy, Is.EqualTo(1f).Within(0.001f));
            Assert.That(messy.Accuracy, Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(messy.Accuracy, Is.GreaterThan(0f), "Exploration never zeroes the result.");
        }

        [Test]
        public void PracticeResultStaysStrengthFramedAndCopySafe()
        {
            var result = PartyStationController.BuildResult(
                Robotics, Robotics.DefaultSeed, ResultSource.SoloFallback, complete: false, wrongAttempts: 1, playElapsedSeconds: 90f);

            Assert.That(result.Tier, Is.EqualTo(CompletionTier.Practice));
            Assert.That(result.TimeRemaining, Is.EqualTo(0f), "Overtime clamps to zero, never negative.");
            Assert.That(PartyStationValidator.CheckResultSummary(result.Summary, "practice summary"), Is.Empty,
                "Practice summaries pass the same strength-framing + safety rules as seed copy.");
        }

        [Test]
        public void QuickPacingCanNeverChangeScoring()
        {
            // The pacing flag is presentation-only: BuildResult has no pacing
            // input, so identical play produces identical results regardless of
            // quick/normal mode. The intro hold is the ONLY thing quick skips.
            var seed = Robotics.DefaultSeed;
            var normal = PartyStationController.BuildResult(Robotics, seed, ResultSource.Solo, true, 1, 20f);
            var quick = PartyStationController.BuildResult(Robotics, seed, ResultSource.Solo, true, 1, 20f);

            Assert.That(quick.Tier, Is.EqualTo(normal.Tier));
            Assert.That(quick.Accuracy, Is.EqualTo(normal.Accuracy));
            Assert.That(quick.TimeRemaining, Is.EqualTo(normal.TimeRemaining));
            Assert.That(quick.Summary, Is.EqualTo(normal.Summary));
            Assert.That(PartyStationController.IntroHoldSeconds, Is.EqualTo(3f).Within(0.001f),
                "Normal pacing keeps the 3-5s intro beat from the design doc.");
        }

        [Test]
        public void AlternateSeedKeepsStationIdentityStable()
        {
            var alternate = Robotics.AlternateSeeds[0];
            var result = PartyStationController.BuildResult(
                Robotics, alternate, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 10f);

            // Same station id + badge identity; only the story copy changes.
            Assert.That(result.ActivityId, Is.EqualTo(Robotics.Id));
            Assert.That(result.DisplayName, Is.EqualTo(Robotics.DisplayName));
            Assert.That(result.Summary, Is.EqualTo(alternate.ResultSummary));
        }

        // ------------------------------------------------------------------
        // Renderer: placeholder policy, layout, labels, theming
        // ------------------------------------------------------------------

        [Test]
        public void PartyToyKeysRenderFinalArtNotFallback()
        {
            // Part B (#4): prop.party.* keys now resolve to final Resources art
            // (CareerQuestPartyToyArtBuilder), not the runtime token fallback —
            // so toys read as distinct objects, never the missing-checker or a
            // generated ".fallback" sprite. (PartyToyArtTests polices the full set.)
            foreach (var objectDefinition in Robotics.ResolveObjects(Robotics.DefaultSeed))
            {
                var sprite = PartyStationRenderer.ResolveToySprite(objectDefinition.SpriteKey);
                Assert.That(sprite, Is.Not.Null, objectDefinition.ObjectId);
                Assert.That(sprite.name, Does.Not.EndWith(SpriteFallbackFactory.FallbackSpriteSuffix),
                    $"'{objectDefinition.ObjectId}' must not render generated fallback art.");
                Assert.That(sprite.name, Does.Not.StartWith("missing."),
                    $"'{objectDefinition.ObjectId}' must not render the missing-definition checker.");
                Assert.That(PartyStationRenderer.IsPlaceholderToySprite(objectDefinition.SpriteKey), Is.False,
                    "prop.party.* keys ship final toy art after the Part B art pass.");
            }
        }

        [Test]
        public void TrayAndTargetLayoutKeepsToysSpacedAndBanded()
        {
            for (var count = 4; count <= 6; count++)
            {
                for (var first = 0; first < count; first++)
                {
                    for (var second = first + 1; second < count; second++)
                    {
                        var trayGap = Vector3.Distance(
                            PartyStationRenderer.TrayPosition(first, count),
                            PartyStationRenderer.TrayPosition(second, count));
                        var targetGap = Vector3.Distance(
                            PartyStationRenderer.TargetPosition(first, count),
                            PartyStationRenderer.TargetPosition(second, count));
                        Assert.That(trayGap, Is.GreaterThanOrEqualTo(1.2f), $"tray {first}/{second} of {count}");
                        Assert.That(targetGap, Is.GreaterThanOrEqualTo(1.2f), $"target {first}/{second} of {count}");
                    }
                }
            }

            Assert.That(PartyStationRenderer.TrayPosition(0, 5).y, Is.LessThan(PartyStationRenderer.TargetPosition(0, 5).y),
                "Tray sits along the bottom band, targets across the middle.");
        }

        [Test]
        public void TargetLabelsDeriveFromSeedObjects()
        {
            var roboticsRules = ToyPatternRules.ForSeed(Robotics, Robotics.DefaultSeed);
            // Robotics is ShootTarget: every shot lands on the one shared goal,
            // labeled the rescue spot.
            Assert.That(PartyStationRenderer.TargetLabelFor(roboticsRules, ToyPatternRules.GoalTargetId), Is.EqualTo("Rescue Spot"));

            var kitchen = PartyStationDefinitions.GetById(CareerQuestCatalog.CommunityKitchenId);
            var kitchenRules = ToyPatternRules.ForSeed(kitchen, kitchen.DefaultSeed);
            Assert.That(PartyStationRenderer.TargetLabelFor(kitchenRules, ToyPatternRules.PourTargetId), Is.Not.Empty);

            var greenCity = PartyStationDefinitions.GetById(CareerQuestCatalog.GreenCityId);
            var greenRules = ToyPatternRules.ForSeed(greenCity, greenCity.DefaultSeed);
            Assert.That(PartyStationRenderer.TargetLabelFor(greenRules, "meter.budget_meter"), Is.EqualTo("Budget Meter"));
        }

        [Test]
        public void AccentForUsesTheBadgeIdentityColor()
        {
            var expected = AssetCatalog.GetDefinition(Robotics.BadgeArtKey).PrimaryColor;
            Assert.That(PartyStationRenderer.AccentFor(Robotics), Is.EqualTo(expected));
        }

        [Test]
        public void TokenColorsAreDeterministicAndDistinct()
        {
            var accent = PartyStationRenderer.AccentFor(Robotics);
            var colors = Enumerable.Range(0, 5)
                .Select(index => PartyStationRenderer.TokenColorFor(accent, index))
                .ToArray();

            Assert.That(colors.Distinct().Count(), Is.EqualTo(colors.Length),
                "Each placeholder toy gets its own readable tint.");
            Assert.That(PartyStationRenderer.TokenColorFor(accent, 2), Is.EqualTo(colors[2]),
                "Token tints are deterministic per object index.");
        }
    }
}
