using System.Collections.Generic;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class PartyStationDefinitionTests
    {
        private static readonly string[] ExpectedStationIds =
        {
            CareerQuestCatalog.RoboticsGarageId,
            CareerQuestCatalog.AiLabId,
            CareerQuestCatalog.CommunityKitchenId,
            CareerQuestCatalog.MusicStudioId,
            CareerQuestCatalog.VetClinicId,
            CareerQuestCatalog.GameStudioId,
            CareerQuestCatalog.WeatherLabId,
            CareerQuestCatalog.SpaceportId,
            CareerQuestCatalog.NewsroomId,
            CareerQuestCatalog.GreenCityId
        };

        [Test]
        public void AllTenStationIdsAreUniqueAndExpected()
        {
            var ids = PartyStationDefinitions.All.Select(station => station.Id).ToArray();

            Assert.That(ids.Length, Is.EqualTo(10));
            Assert.That(ids.Distinct().Count(), Is.EqualTo(10));
            Assert.That(ids, Is.EquivalentTo(ExpectedStationIds));
            Assert.That(ids, Is.EquivalentTo(CareerQuestCatalog.PartyStationIds));
        }

        [Test]
        public void ValidateAllReportsNoIssuesForShippedStationData()
        {
            var issues = PartyStationValidator.ValidateAll();

            Assert.That(issues, Is.Empty, string.Join("\n", issues));
        }

        [Test]
        public void EveryStationSharesItsIdWithCatalogBadgeCampusAndEvolutionMetadata()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(CareerQuestCatalog.TryGetById(station.Id, out var entry), Is.True, station.Id);
                Assert.That(entry.BadgeArtKey, Is.EqualTo(station.BadgeArtKey), station.Id);
                Assert.That(entry.CampusAssetId, Is.EqualTo(station.CampusArtKey), station.Id);
                Assert.That(station.CareerTags, Does.Contain(entry.CareerTag), station.Id);

                Assert.That(CampusEvolutionController.TryGetEvolutionPropAssetId(station.Id, out var evolutionProp), Is.True, station.Id);
                Assert.That(evolutionProp, Is.EqualTo(station.EvolutionPropAssetId), station.Id);
            }
        }

        [Test]
        public void EveryStationHasExactlyOneDefaultAndOneAlternateSeedWithUniqueIds()
        {
            var allSeedIds = new List<string>();

            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(station.Seeds.Count, Is.EqualTo(2), station.Id);
                Assert.That(station.Seeds.Count(seed => seed.IsDefault), Is.EqualTo(1), station.Id);
                Assert.That(station.DefaultSeed, Is.Not.Null, station.Id);
                Assert.That(station.AlternateSeeds.Count, Is.EqualTo(1), station.Id);

                foreach (var seed in station.Seeds)
                {
                    Assert.That(seed.SeedId, Does.StartWith($"{station.Id}."), station.Id);
                    allSeedIds.Add(seed.SeedId);
                }
            }

            Assert.That(allSeedIds.Distinct().Count(), Is.EqualTo(allSeedIds.Count));
        }

        [Test]
        public void EverySeedHasFourToSixActiveObjectsWithChainMinimum()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    var objects = station.ResolveObjects(seed);

                    Assert.That(objects.Count, Is.InRange(4, 6), seed.SeedId);
                    Assert.That(objects.Count(item => item.IsChainRole), Is.GreaterThanOrEqualTo(4), seed.SeedId);
                    Assert.That(
                        objects.Count(item => item.Role == PartyStationObjectRole.CoreTask),
                        Is.GreaterThanOrEqualTo(2),
                        seed.SeedId);
                    Assert.That(objects.Select(item => item.ObjectId).Distinct().Count(), Is.EqualTo(objects.Count), seed.SeedId);
                }
            }
        }

        [Test]
        public void EverySeedObjectDeclaresAVisibleReactionAndKnownReferences()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    var objects = station.ResolveObjects(seed);
                    var knownIds = objects.Select(item => item.ObjectId).ToArray();

                    foreach (var item in objects)
                    {
                        Assert.That(PartyStationValidator.KnownReactionKeys, Does.Contain(item.ReactionKey), $"{seed.SeedId}.{item.ObjectId}");

                        if (!string.IsNullOrEmpty(item.TargetId))
                        {
                            Assert.That(knownIds, Does.Contain(item.TargetId), $"{seed.SeedId}.{item.ObjectId}");
                        }

                        if (!string.IsNullOrEmpty(item.TraitHint))
                        {
                            Assert.That(CareerConfig.AllTraits, Does.Contain(item.TraitHint), $"{seed.SeedId}.{item.ObjectId}");
                        }
                    }
                }
            }
        }

        [Test]
        public void EveryStationUsesASupportedToyPattern()
        {
            var supported = new[]
            {
                ToyPatternId.DragToSlot,
                ToyPatternId.SortToBin,
                ToyPatternId.PickMatchingTrio,
                ToyPatternId.SequenceCards,
                ToyPatternId.ComposeSet,
                ToyPatternId.MatchAndCare,
                ToyPatternId.BalanceMeters,
                ToyPatternId.TracePath,
                ToyPatternId.ShootTarget,
                ToyPatternId.DeduceAnswer
            };

            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(supported, Does.Contain(station.Pattern), station.Id);
            }
        }

        [Test]
        public void EveryStationHasGuideIdentityAndFullCopySurface()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(station.GuideName, Is.Not.Empty, station.Id);
                Assert.That(station.GuideVoice, Is.Not.Empty, station.Id);
                Assert.That(station.Prompt, Is.Not.Empty, station.Id);

                foreach (var seed in station.Seeds)
                {
                    Assert.That(station.ResolvePrompt(seed), Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.TargetRule, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.IntroLine, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.HintLine, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.EscalationHintLine, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.SuccessLine, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.RewardPreviewLine, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.ResultSummary, Is.Not.Empty, seed.SeedId);
                    Assert.That(seed.NpcReaction, Is.Not.Empty, seed.SeedId);
                }
            }
        }

        [Test]
        public void EveryStationCareerTagAndTraitDeltaResolvesToKnownConfig()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(station.CareerTags, Is.Not.Empty, station.Id);
                foreach (var tag in station.CareerTags)
                {
                    Assert.That(CareerConfig.TryGetCareer(tag, out _), Is.True, $"{station.Id}: {tag}");
                }

                Assert.That(station.TraitDeltas, Is.Not.Empty, station.Id);
                foreach (var delta in station.TraitDeltas)
                {
                    Assert.That(CareerConfig.AllTraits, Does.Contain(delta.Trait), station.Id);
                    Assert.That(delta.Delta, Is.GreaterThan(0), station.Id);
                }
            }
        }

        [Test]
        public void EveryStationAccessoryRewardResolvesAndMapsBackToItsStation()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(AccessoryRewardConfig.TryGetById(station.AccessoryRewardId, out var accessory), Is.True, station.Id);
                Assert.That(accessory.StationId, Is.EqualTo(station.Id), station.Id);
                Assert.That(accessory.IsMilestone, Is.False, station.Id);
            }
        }

        [Test]
        public void MilestoneAccessoriesCoverEveryThresholdAndTheFlourishIsCeremonyOnly()
        {
            foreach (var threshold in AccessoryRewardConfig.MilestoneThresholds)
            {
                Assert.That(AccessoryRewardConfig.TryGetForMilestone(threshold, out _), Is.True, threshold.ToString());
            }

            Assert.That(AccessoryRewardConfig.TryGetForMilestone(10, out var flourish), Is.True);
            Assert.That(flourish.CeremonyOnly, Is.True);
        }

        [Test]
        public void ComboCardsPairKnownStationsWithUniquePriorities()
        {
            Assert.That(CareerComboConfig.All.Count, Is.GreaterThanOrEqualTo(12));
            Assert.That(CareerComboConfig.All.Select(combo => combo.Id).Distinct().Count(), Is.EqualTo(CareerComboConfig.All.Count));
            Assert.That(
                CareerComboConfig.All.Select(combo => combo.AuthoredPriority).Distinct().Count(),
                Is.EqualTo(CareerComboConfig.All.Count));

            foreach (var combo in CareerComboConfig.All)
            {
                Assert.That(combo.RequiredStationIds.Count, Is.EqualTo(2), combo.Id);
                Assert.That(combo.RequiredStationIds.Distinct().Count(), Is.EqualTo(2), combo.Id);
                foreach (var stationId in combo.RequiredStationIds)
                {
                    Assert.That(CareerQuestCatalog.TryGetById(stationId, out _), Is.True, $"{combo.Id}: {stationId}");
                }

                foreach (var family in combo.FamilyBlend)
                {
                    Assert.That(CareerFamilies.All, Does.Contain(family), combo.Id);
                }
            }
        }

        [Test]
        public void ValidatorReportsNullDefinition()
        {
            var issues = PartyStationValidator.Validate(null);

            Assert.That(issues, Has.Some.Contains("definition is null"));
        }

        [Test]
        public void ValidatorRejectsUnknownToyPattern()
        {
            var issues = PartyStationValidator.Validate(Synthetic(pattern: (ToyPatternId)999));

            Assert.That(issues, Has.Some.Contains("unsupported toy pattern"));
        }

        [Test]
        public void ValidatorRejectsDuplicateObjectIds()
        {
            var objects = ValidObjects().ToList();
            objects[1] = TestObject("part_a", PartyStationObjectRole.CoreTask);
            var issues = PartyStationValidator.Validate(Synthetic(objects: objects));

            Assert.That(issues, Has.Some.Contains("object id 'part_a'"));
        }

        [Test]
        public void ValidatorRejectsTooManyObjects()
        {
            var objects = ValidObjects().ToList();
            objects.Add(TestObject("extra_one", PartyStationObjectRole.Bonus));
            objects.Add(TestObject("extra_two", PartyStationObjectRole.Bonus));
            var issues = PartyStationValidator.Validate(Synthetic(objects: objects));

            Assert.That(issues, Has.Some.Contains("interactables"));
        }

        [Test]
        public void ValidatorRejectsSeedsWithTooFewChainObjects()
        {
            var objects = new[]
            {
                TestObject("part_a", PartyStationObjectRole.CoreTask),
                TestObject("part_b", PartyStationObjectRole.CoreTask),
                TestObject("helper_a", PartyStationObjectRole.Helper),
                TestObject("reaction_a", PartyStationObjectRole.Reaction)
            };
            var issues = PartyStationValidator.Validate(Synthetic(objects: objects));

            Assert.That(issues, Has.Some.Contains("task/clue-chain"));
        }

        [Test]
        public void ValidatorRejectsUnknownTargetReferences()
        {
            var objects = ValidObjects().ToList();
            objects[3] = new PartyStationObjectDefinition(
                "clue_a",
                "Clue A",
                PartyStationObjectRole.Clue,
                "prop.party.robotics_garage.clue_a",
                "missing_object",
                "react.glow");
            var issues = PartyStationValidator.Validate(Synthetic(objects: objects));

            Assert.That(issues, Has.Some.Contains("does not reference a known object"));
        }

        [Test]
        public void ValidatorRejectsUnknownReactionKeys()
        {
            var objects = ValidObjects().ToList();
            objects[0] = new PartyStationObjectDefinition(
                "part_a",
                "Part A",
                PartyStationObjectRole.CoreTask,
                "prop.party.robotics_garage.part_a",
                "",
                "react.unknown");
            var issues = PartyStationValidator.Validate(Synthetic(objects: objects));

            Assert.That(issues, Has.Some.Contains("not a known shared cue"));
        }

        [Test]
        public void ValidatorRejectsMissingDefaultSeedAndDuplicateSeedIds()
        {
            var noDefault = Synthetic(seeds: new[]
            {
                TestSeed("robotics_garage.seed_a", false),
                TestSeed("robotics_garage.seed_b", false)
            });
            Assert.That(PartyStationValidator.Validate(noDefault), Has.Some.Contains("exactly one default seed"));

            var duplicateIds = Synthetic(seeds: new[]
            {
                TestSeed("robotics_garage.seed_a", true),
                TestSeed("robotics_garage.seed_a", false)
            });
            Assert.That(PartyStationValidator.Validate(duplicateIds), Has.Some.Contains("duplicate"));
        }

        [Test]
        public void ValidatorRejectsEmptySeedListAndUnknownCareerTags()
        {
            Assert.That(
                PartyStationValidator.Validate(Synthetic(seeds: new PartyStationSeedDefinition[0])),
                Has.Some.Contains("no seeds"));
            Assert.That(
                PartyStationValidator.Validate(Synthetic(careerTags: new[] { "robotics_engineer", "made_up_career" })),
                Has.Some.Contains("unknown career tag 'made_up_career'"));
        }

        private static PartyStationDefinition Synthetic(
            ToyPatternId pattern = ToyPatternId.DragToSlot,
            IEnumerable<PartyStationObjectDefinition> objects = null,
            IEnumerable<PartyStationSeedDefinition> seeds = null,
            IEnumerable<string> careerTags = null)
        {
            // Reuses the Robotics identity so catalog/accessory/evolution
            // alignment stays valid; perturbations isolate one failure each.
            return new PartyStationDefinition(
                CareerQuestCatalog.RoboticsGarageId,
                "Robotics Rescue",
                new[] { "build", "rescue" },
                pattern,
                "Bolt the Bench Buddy",
                "upbeat build coach",
                "A lunchbox robot lost its parts! Rebuild it and pick a rescue route.",
                objects ?? ValidObjects(),
                "Place three robot parts, then pick the route that matches the clue.",
                new[] { new TraitDelta("Building", 5), new TraitDelta("Reasoning", 4) },
                "accessory.tool_belt",
                careerTags ?? new[] { "robotics_engineer", "ai_engineer" },
                "badge.robotics_garage",
                "campus.robotics_garage",
                "prop.city_piece_garage",
                seeds ?? new[]
                {
                    TestSeed("robotics_garage.seed_default", true),
                    TestSeed("robotics_garage.seed_alternate", false)
                });
        }

        private static PartyStationObjectDefinition[] ValidObjects()
        {
            return new[]
            {
                TestObject("part_a", PartyStationObjectRole.CoreTask),
                TestObject("part_b", PartyStationObjectRole.CoreTask),
                TestObject("part_c", PartyStationObjectRole.CoreTask),
                TestObject("clue_a", PartyStationObjectRole.Clue),
                TestObject("reaction_a", PartyStationObjectRole.Reaction)
            };
        }

        private static PartyStationObjectDefinition TestObject(string objectId, PartyStationObjectRole role)
        {
            return new PartyStationObjectDefinition(
                objectId,
                "Test Object",
                role,
                $"prop.party.robotics_garage.{objectId}",
                "",
                "react.pop");
        }

        private static PartyStationSeedDefinition TestSeed(string seedId, bool isDefault)
        {
            return new PartyStationSeedDefinition(
                seedId,
                "Test Seed",
                isDefault,
                "",
                null,
                "Place three robot parts, then pick the matching route.",
                "Bolt beeps: the test robot needs three parts!",
                "Try a part that matches the empty slot shape.",
                "Watch the glowing slot for the next part.",
                "The test robot is rebuilt and rolling!",
                "Finish the rescue to earn the Tool Belt!",
                "You rebuilt the test robot. You practiced Building + Reasoning. New gear: Tool Belt.",
                "The test robot spins a happy circle.");
        }
    }
}
