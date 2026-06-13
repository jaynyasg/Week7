using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U7 primary combo selection (R15, KTD8). Combos add NO score — the
    /// resolver only decides which eligible combo leads the ceremony. Selection
    /// order: strongest traits → most recently completed station → authored
    /// priority. Eligibility is the pure pair check; empty/no-pair inputs yield
    /// no primary. All comparisons are deterministic.
    /// </summary>
    public class CareerComboResolverTests
    {
        // ------------------------------------------------------------------
        // Eligibility: a combo unlocks only when BOTH stations are completed
        // ------------------------------------------------------------------

        [Test]
        public void NoEligiblePairSelectsNoPrimaryCombo()
        {
            // Robotics alone: no combo pair is complete.
            var completed = new List<string> { CareerQuestCatalog.RoboticsGarageId };

            var primary = CareerComboResolver.SelectPrimary(NeutralTraits(), completed);

            Assert.That(primary, Is.Null);
            Assert.That(CareerComboResolver.RankEligible(NeutralTraits(), completed), Is.Empty);
        }

        [Test]
        public void EmptyCompletionListSelectsNoPrimaryCombo()
        {
            Assert.That(CareerComboResolver.SelectPrimary(NeutralTraits(), new List<string>()), Is.Null);
            Assert.That(CareerComboResolver.SelectPrimary(NeutralTraits(), null), Is.Null);
            Assert.That(CareerComboResolver.SelectPrimary(null, null), Is.Null);
        }

        [Test]
        public void CompletedStationPairUnlocksItsComboAsPrimary()
        {
            // Robotics + Community Kitchen = Robot Chef.
            var completed = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId
            };

            var primary = CareerComboResolver.SelectPrimary(NeutralTraits(), completed);

            Assert.That(primary, Is.Not.Null);
            Assert.That(primary.Id, Is.EqualTo("combo.robot_chef"));
        }

        [Test]
        public void ComboSelectionAddsNoScore()
        {
            // Resolving the primary combo must not require or expose any score
            // effect — it consumes derived facts and returns a definition only.
            var completed = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId
            };

            var primary = CareerComboResolver.SelectPrimary(NeutralTraits(), completed);

            // The combo definition carries copy/ids/priority — never a score field.
            Assert.That(primary, Is.Not.Null);
            Assert.That(primary.GetType().GetProperty("Score"), Is.Null, "Combos add ceremony flavor, not score (KTD8).");
        }

        // ------------------------------------------------------------------
        // Primary selection: strongest traits FIRST
        // ------------------------------------------------------------------

        [Test]
        public void StrongestTraitsPickTheBestFitComboEvenOverBetterAuthoredPriority()
        {
            // Completed {AI Lab, Music, Logic Court} makes two combos eligible:
            //   ai_music_producer  (FutureTech + Story&Stage, priority 7)
            //   data_detective     (FutureTech + Justice,     priority 10)
            // A Justice/Leadership-heavy profile fits data_detective's blend
            // better, so it leads DESPITE its worse authored priority — proving
            // trait fit is the first key, ahead of priority.
            var completed = new List<string>
            {
                CareerQuestCatalog.AiLabId,
                CareerQuestCatalog.MusicStudioId,
                CareerConfig.LogicCourtId
            };
            var justiceHeavy = Traits(("Leadership", 20), ("Reasoning", 14), ("Communication", 8));

            var ranked = CareerComboResolver.RankEligible(justiceHeavy, completed);

            Assert.That(ranked.Select(combo => combo.Id),
                Is.EqualTo(new[] { "combo.data_detective", "combo.ai_music_producer" }));
            Assert.That(CareerComboResolver.SelectPrimary(justiceHeavy, completed).Id, Is.EqualTo("combo.data_detective"));
        }

        [Test]
        public void StoryHeavyProfileFlipsTheSameEligibleSetTheOtherWay()
        {
            // Same eligible set; a Story-heavy profile now fits ai_music_producer
            // best, so the trait-fit key flips the winner.
            var completed = new List<string>
            {
                CareerQuestCatalog.AiLabId,
                CareerQuestCatalog.MusicStudioId,
                CareerConfig.LogicCourtId
            };
            var storyHeavy = Traits(("Creativity", 18), ("Communication", 12));

            Assert.That(
                CareerComboResolver.SelectPrimary(storyHeavy, completed).Id,
                Is.EqualTo("combo.ai_music_producer"));
        }

        // ------------------------------------------------------------------
        // Tie-break: most recent station, THEN authored priority
        // ------------------------------------------------------------------

        [Test]
        public void EqualTraitFitBreaksByMostRecentlyCompletedStation()
        {
            // robot_chef (Robotics + Kitchen, pri 1) and robot_care_engineer
            // (Robotics + Vet, pri 8) share the SAME family blend (FutureTech +
            // Care&Community) → identical trait fit for any profile. Completion
            // order puts Vet last, so robot_care_engineer's later station is more
            // recent and it leads DESPITE its worse authored priority — the
            // recency key beats priority.
            var completedVetLast = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId,
                CareerQuestCatalog.VetClinicId
            };

            Assert.That(
                CareerComboResolver.SelectPrimary(NeutralTraits(), completedVetLast).Id,
                Is.EqualTo("combo.robot_care_engineer"));
        }

        [Test]
        public void MostRecentStationFlipsWhenTheOtherStationCompletesLast()
        {
            // Same equal-blend pair; now Kitchen is most recent, so robot_chef
            // leads (it also has the better priority, but recency already decides).
            var completedKitchenLast = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.VetClinicId,
                CareerQuestCatalog.CommunityKitchenId
            };

            Assert.That(
                CareerComboResolver.SelectPrimary(NeutralTraits(), completedKitchenLast).Id,
                Is.EqualTo("combo.robot_chef"));
        }

        [Test]
        public void EqualTraitFitAndEqualRecencyBreakByAuthoredPriority()
        {
            // music_doctor (Music + HealthHero, pri 2) and game_studio_doctor
            // (GameStudio + HealthHero, pri 9) share the blend (Story&Stage +
            // Care&Community) → equal trait fit. HealthHero completes LAST, so it
            // is the shared most-recent station for BOTH combos → equal recency.
            // Authored priority is the final tiebreak → music_doctor leads.
            var completedHealthLast = new List<string>
            {
                CareerQuestCatalog.MusicStudioId,
                CareerQuestCatalog.GameStudioId,
                CareerConfig.HealthHeroId
            };

            var ranked = CareerComboResolver.RankEligible(NeutralTraits(), completedHealthLast);

            Assert.That(ranked.Select(combo => combo.Id),
                Is.EqualTo(new[] { "combo.music_doctor", "combo.game_studio_doctor" }));
        }

        // ------------------------------------------------------------------
        // Determinism + additional-combo listing
        // ------------------------------------------------------------------

        [Test]
        public void RankingIsDeterministicForTheSameInputs()
        {
            var completed = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId,
                CareerQuestCatalog.VetClinicId
            };
            var traits = Traits(("Building", 8), ("Helping", 6));

            var first = CareerComboResolver.RankEligible(traits, completed).Select(c => c.Id).ToArray();
            var second = CareerComboResolver.RankEligible(traits, completed).Select(c => c.Id).ToArray();
            var third = CareerComboResolver.RankEligible(traits, completed).Select(c => c.Id).ToArray();

            Assert.That(second, Is.EqualTo(first));
            Assert.That(third, Is.EqualTo(first));
        }

        [Test]
        public void RankEligibleListsEveryEligibleComboPrimaryFirst()
        {
            // Both robot combos eligible; the primary is index 0 and the rest are
            // the "also unlocked" list for the gallery/passport.
            var completed = new List<string>
            {
                CareerQuestCatalog.RoboticsGarageId,
                CareerQuestCatalog.CommunityKitchenId,
                CareerQuestCatalog.VetClinicId
            };

            var ranked = CareerComboResolver.RankEligible(NeutralTraits(), completed);
            var eligibleIds = RewardEventLog.EligibleComboIds(completed);

            Assert.That(ranked.Count, Is.EqualTo(eligibleIds.Count));
            Assert.That(ranked.Select(c => c.Id).OrderBy(id => id),
                Is.EqualTo(eligibleIds.OrderBy(id => id)));
            Assert.That(ranked.First().Id, Is.EqualTo(CareerComboResolver.SelectPrimary(NeutralTraits(), completed).Id));
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private static IReadOnlyDictionary<string, int> NeutralTraits()
        {
            // Small, even spread: no family is favored, so trait fit ties for
            // equal-blend combos and the recency/priority keys are isolated.
            return Traits(("Helping", 4), ("Building", 4), ("Creativity", 4), ("Reasoning", 4));
        }

        private static IReadOnlyDictionary<string, int> Traits(params (string trait, int total)[] totals)
        {
            return totals.ToDictionary(pair => pair.trait, pair => pair.total);
        }
    }
}
