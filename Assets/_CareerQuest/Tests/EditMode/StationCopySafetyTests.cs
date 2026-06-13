using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U1 copy safety gate (design doc: Early-reader copy rule, Reveal Ceremony
    /// Target, Pretend-play safety rule). The encoded rules live in
    /// PartyStationValidator: guide lines max 80 chars, rules max 90, result
    /// summaries max 200 with "You practiced" framing, and banned
    /// deterministic/jargon/pretend-play wording.
    /// </summary>
    public class StationCopySafetyTests
    {
        [Test]
        public void EveryStationGuideLinePassesEarlyReaderRules()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.IntroLine, $"{seed.SeedId}.intro"));
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.HintLine, $"{seed.SeedId}.hint"));
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.EscalationHintLine, $"{seed.SeedId}.escalation"));
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.SuccessLine, $"{seed.SeedId}.success"));
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.RewardPreviewLine, $"{seed.SeedId}.reward_preview"));
                    AssertClean(PartyStationValidator.CheckGuideLine(seed.NpcReaction, $"{seed.SeedId}.npc_reaction"));
                    AssertClean(PartyStationValidator.CheckRuleLine(station.ResolvePrompt(seed), $"{seed.SeedId}.prompt"));
                    AssertClean(PartyStationValidator.CheckRuleLine(seed.TargetRule, $"{seed.SeedId}.target_rule"));
                    AssertClean(PartyStationValidator.CheckResultSummary(seed.ResultSummary, $"{seed.SeedId}.result_summary"));
                }
            }
        }

        [Test]
        public void CopyValidationFlagsEmptyCopy()
        {
            Assert.That(PartyStationValidator.CheckGuideLine("", "test"), Has.Some.Contains("copy is empty"));
            Assert.That(PartyStationValidator.CheckGuideLine("   ", "test"), Has.Some.Contains("copy is empty"));
            Assert.That(PartyStationValidator.CheckGuideLine(null, "test"), Has.Some.Contains("copy is empty"));
            Assert.That(PartyStationValidator.CheckResultSummary(null, "test"), Has.Some.Contains("result summary is empty"));
        }

        [Test]
        public void CopyValidationFlagsOverlongEarlyReaderLines()
        {
            var overlong = new string('a', PartyStationValidator.MaxGuideLineLength + 1);

            Assert.That(PartyStationValidator.CheckGuideLine(overlong, "test"), Has.Some.Contains("early-reader max"));
            Assert.That(
                PartyStationValidator.CheckGuideLine("Short and friendly line.", "test"),
                Is.Empty);
        }

        [Test]
        public void CopyValidationFlagsDeterministicCareerPhrases()
        {
            Assert.That(
                PartyStationValidator.CheckGuideLine("You will be a doctor when you grow up.", "test"),
                Has.Some.Contains("deterministic career phrase"));
            Assert.That(
                PartyStationValidator.CheckGuideLine("This is your destiny, little builder.", "test"),
                Has.Some.Contains("deterministic career phrase"));
            Assert.That(
                PartyStationValidator.CheckGuideLine("You practiced building today and might like it.", "test"),
                Is.Empty);
        }

        [Test]
        public void CopyValidationFlagsCareerJargon()
        {
            Assert.That(
                PartyStationValidator.CheckGuideLine("Optimize the algorithm before the demo.", "test"),
                Has.Some.Contains("career jargon"));
        }

        [Test]
        public void CopyValidationFlagsUnsafePretendPlayWording()
        {
            Assert.That(
                PartyStationValidator.CheckGuideLine("The dragon is sick and needs medicine.", "test"),
                Has.Some.Contains("pretend-play-unsafe word"));
            Assert.That(
                PartyStationValidator.CheckGuideLine("The storm is a deadly disaster!", "test"),
                Has.Some.Contains("pretend-play-unsafe word"));
            // Word-boundary matching: "painted" must not trip "pain".
            Assert.That(
                PartyStationValidator.CheckGuideLine("Someone painted a mural overnight.", "test"),
                Is.Empty);
        }

        [Test]
        public void ResultSummariesUseStrengthFraming()
        {
            Assert.That(
                PartyStationValidator.CheckResultSummary("You finished the room. New gear: Tool Belt.", "test"),
                Has.Some.Contains("You practiced"));

            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    Assert.That(seed.ResultSummary, Does.Contain("You practiced"), seed.SeedId);
                }
            }
        }

        [Test]
        public void ComboCardCopyPassesSafetyScan()
        {
            foreach (var combo in CareerComboConfig.All)
            {
                AssertClean(PartyStationValidator.CheckGuideLine(combo.RevealCopy, combo.Id));
                AssertClean(PartyStationValidator.CheckCopySafety(combo.DisplayName, combo.Id));
            }
        }

        [Test]
        public void CareerTaglinesPassSafetyScan()
        {
            foreach (var career in CareerConfig.Careers)
            {
                AssertClean(PartyStationValidator.CheckCopySafety(career.Tagline, career.Id));
            }
        }

        // ------------------------------------------------------------------
        // U7: reveal / family / superpower / combo copy stays strength-based.
        // ------------------------------------------------------------------

        [Test]
        public void FamilyPresentationCopyPassesEarlyReaderAndSafetyScan()
        {
            foreach (var presentation in CareerFamilyConfig.All)
            {
                var context = presentation.Family;
                // Display name + superpower are short labels: copy-safety only.
                AssertClean(PartyStationValidator.CheckCopySafety(presentation.DisplayName, $"{context}.display"));
                AssertClean(PartyStationValidator.CheckCopySafety(presentation.Superpower, $"{context}.superpower"));
                // The blurb is a full guide-style line: early-reader + safety.
                AssertClean(PartyStationValidator.CheckGuideLine(presentation.Blurb, $"{context}.blurb"));
            }
        }

        [Test]
        public void SynthesizedRevealHeadlineAndSubheadCopyStaysSafe()
        {
            // Drive synthesis across every completion bucket and scan the
            // player-facing reveal copy (superpower headline + family subhead +
            // primary combo copy) — the strings the ceremony actually shows.
            var session = new GameSession();
            foreach (var stationId in CareerQuestCatalog.PartyStationIds)
            {
                session.RecordResult(StationResult(stationId));

                var result = RevealSynthesis.Resolve(session);
                AssertClean(PartyStationValidator.CheckCopySafety(result.Superpower, "reveal.superpower"));
                AssertClean(PartyStationValidator.CheckCopySafety(result.FamilySubhead, "reveal.subhead"));
                if (result.HasComboSpotlight)
                {
                    AssertClean(PartyStationValidator.CheckGuideLine(result.PrimaryCombo.RevealCopy, "reveal.combo"));
                }
            }
        }

        [Test]
        public void RevealCopyValidationFailsOnDeterministicCareerPhrases()
        {
            // The reveal must reject "you will be a..." style destiny phrasing.
            Assert.That(
                PartyStationValidator.CheckCopySafety("You will be a robotics engineer.", "reveal"),
                Has.Some.Contains("deterministic career phrase"));
            Assert.That(
                PartyStationValidator.CheckCopySafety("This is your future career path.", "reveal"),
                Has.Some.Contains("deterministic career phrase"));
            // The strength-based voice the reveal actually uses passes.
            AssertClean(PartyStationValidator.CheckCopySafety(
                "You might like these paths — a strength clue from your quest, not a life assignment.", "reveal"));
            AssertClean(PartyStationValidator.CheckCopySafety("You practiced building. Your strengths today lead here.", "reveal"));
        }

        private static MiniGameResult StationResult(string stationId)
        {
            var definition = PartyStationDefinitions.GetById(stationId);
            return PartyStationController.BuildResult(
                definition, definition.DefaultSeed, ResultSource.Solo, complete: true, wrongAttempts: 0, playElapsedSeconds: 12f);
        }

        [Test]
        public void AccessoryAndObjectDisplayNamesPassSafetyScan()
        {
            foreach (var accessory in AccessoryRewardConfig.All)
            {
                AssertClean(PartyStationValidator.CheckCopySafety(accessory.DisplayName, accessory.Id));
            }

            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    foreach (var item in station.ResolveObjects(seed))
                    {
                        AssertClean(PartyStationValidator.CheckCopySafety(item.DisplayName, $"{seed.SeedId}.{item.ObjectId}"));
                    }
                }
            }
        }

        private static void AssertClean(System.Collections.Generic.IReadOnlyList<string> issues)
        {
            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }
    }
}
