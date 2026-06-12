using System;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class AssetValidationTests
    {
        private static readonly string[] RequiredFirstPlayableIds =
        {
            "avatar.sky_builder",
            "avatar.care_captain",
            "avatar.logic_spark",
            "avatar.art_inventor",
            "npc.campus_guide",
            "npc.builder_partner",
            "npc.patient",
            "npc.judge",
            "campus.design_build_studio",
            "campus.health_hero_clinic",
            "campus.logic_court",
            "campus.achievement_gallery",
            "campus.reveal_stage",
            "room.design_build",
            "room.health_hero",
            "room.logic_court",
            "room.gallery",
            "room.reveal",
            "prop.blueprint",
            "prop.city_piece_clinic",
            "prop.city_piece_court",
            "prop.city_piece_studio",
            "prop.city_piece_lab",
            "prop.city_piece_art_tower",
            "prop.thermometer",
            "prop.care_plan",
            "prop.evidence_card",
            "prop.argument_meter",
            "badge.design_build",
            "badge.health_hero",
            "badge.logic_court",
            "badge.reveal_ready",
            "ui.exit",
            "ui.gallery",
            "ui.reveal_locked",
            "ui.reveal_unlocked",
            "ui.confirm",
            "ui.back"
        };

        [Test]
        public void RequiredAssetIdsAreUnique()
        {
            var duplicateIds = AssetCatalog.Definitions
                .GroupBy(definition => definition.Id)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            Assert.That(duplicateIds, Is.Empty);
        }

        [Test]
        public void FirstPlayableContainsRequiredArtIds()
        {
            var requiredIds = AssetCatalog.RequiredDefinitions.Select(definition => definition.Id).ToArray();

            foreach (var requiredId in RequiredFirstPlayableIds)
            {
                Assert.That(requiredIds, Does.Contain(requiredId), requiredId);
            }
        }

        [Test]
        public void FirstPlayableHasRequiredVisualCategories()
        {
            var requiredCategories = AssetCatalog.RequiredDefinitions
                .Select(definition => definition.Category)
                .Distinct()
                .ToArray();

            Assert.That(requiredCategories, Does.Contain(AssetCategory.Avatar));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Npc));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Campus));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Room));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Prop));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Badge));
            Assert.That(requiredCategories, Does.Contain(AssetCategory.Ui));
        }

        [Test]
        public void RequiredCatalogSpritesResolveWithinTextureBudget()
        {
            foreach (var definition in AssetCatalog.RequiredDefinitions)
            {
                var resolution = AssetCatalog.ResolveSprite(definition.Id);
                var sprite = resolution.Sprite;

                Assert.That(sprite, Is.Not.Null, definition.Id);
                Assert.That(sprite.texture.width, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
                Assert.That(sprite.texture.height, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
                Assert.That(resolution.IsFinalArt, Is.EqualTo(!resolution.IsFallbackGenerated), definition.Id);
            }
        }

        [Test]
        public void RequiredFallbackSpritesMatchDefinitionDimensions()
        {
            foreach (var definition in AssetCatalog.RequiredDefinitions)
            {
                var fallback = SpriteFallbackFactory.Create(definition);
                var expectedWidth = Math.Max(32, Math.Min(definition.PixelSize.x, AssetCatalog.MaxFallbackTextureSize));
                var expectedHeight = Math.Max(32, Math.Min(definition.PixelSize.y, AssetCatalog.MaxFallbackTextureSize));

                Assert.That(definition.PixelSize.x, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
                Assert.That(definition.PixelSize.y, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
                Assert.That(fallback, Is.Not.Null, definition.Id);
                Assert.That(AssetCatalog.IsFallbackSprite(fallback), Is.True, definition.Id);
                Assert.That(fallback.texture.width, Is.EqualTo(expectedWidth), definition.Id);
                Assert.That(fallback.texture.height, Is.EqualTo(expectedHeight), definition.Id);
            }
        }

        [Test]
        public void RequiredDefinitionsDoNotUseBlankIdsOrNames()
        {
            foreach (var definition in AssetCatalog.RequiredDefinitions)
            {
                Assert.That(definition.Id, Is.Not.Empty);
                Assert.That(definition.DisplayName, Is.Not.Empty);
                Assert.That(Enum.IsDefined(typeof(AssetCategory), definition.Category), Is.True);
            }
        }

        [Test]
        public void PlayerFacingFallbackUsageIsReportedForVisualQa()
        {
            var resolvedPlayerFacingSprites = AssetCatalog.ResolvePlayerFacingSprites();
            var expectedFallbackIds = resolvedPlayerFacingSprites
                .Where(resolution => resolution.IsPlayerFacingFallback)
                .Select(resolution => resolution.RequestedId)
                .ToArray();
            var reportedFallbacks = AssetCatalog.PlayerFacingFallbackUsage();

            Assert.That(reportedFallbacks.Select(resolution => resolution.RequestedId), Is.EquivalentTo(expectedFallbackIds));

            foreach (var fallback in reportedFallbacks)
            {
                Assert.That(fallback.IsCataloged, Is.True, fallback.RequestedId);
                Assert.That(fallback.Definition.RequiresFinalArtForPlayerFacingAcceptance, Is.True, fallback.RequestedId);
                Assert.That(fallback.IsFallbackGenerated, Is.True, fallback.RequestedId);
                Assert.That(fallback.IsFinalArt, Is.False, fallback.RequestedId);
            }
        }

        [Test]
        public void PlayerFacingDefinitionsCoverFirstPlayableArtGate()
        {
            var playerFacingIds = AssetCatalog.PlayerFacingDefinitions.Select(definition => definition.Id).ToArray();

            foreach (var requiredId in RequiredFirstPlayableIds)
            {
                Assert.That(playerFacingIds, Does.Contain(requiredId), requiredId);
            }
        }

        /// <summary>
        /// U1: every Party Pack station identity art key (badge, campus
        /// building, evolution piece) resolves to a cataloged definition. New
        /// station art ships as intentional placeholders (not required, not
        /// player-facing) so the final-art fallback gate ignores them until
        /// the U5/U8/U11 art passes flip the flags.
        /// </summary>
        [Test]
        public void PartyStationIdentityArtKeysAreCataloged()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                Assert.That(AssetCatalog.TryGetDefinition(station.BadgeArtKey, out var badge), Is.True, station.BadgeArtKey);
                Assert.That(badge.Category, Is.EqualTo(AssetCategory.Badge), station.BadgeArtKey);

                Assert.That(AssetCatalog.TryGetDefinition(station.CampusArtKey, out var campus), Is.True, station.CampusArtKey);
                Assert.That(campus.Category, Is.EqualTo(AssetCategory.Campus), station.CampusArtKey);

                Assert.That(AssetCatalog.TryGetDefinition(station.EvolutionPropAssetId, out var piece), Is.True, station.EvolutionPropAssetId);
                Assert.That(piece.Category, Is.EqualTo(AssetCategory.Prop), station.EvolutionPropAssetId);
            }
        }

        [Test]
        public void AccessoryRewardSpritesAreCatalogedAsIntentionalPlaceholders()
        {
            foreach (var accessory in AccessoryRewardConfig.All)
            {
                Assert.That(AssetCatalog.TryGetDefinition(accessory.SpriteAssetId, out var definition), Is.True, accessory.Id);
                Assert.That(definition.Category, Is.EqualTo(AssetCategory.Prop), accessory.Id);
                // Placeholder contract: accessory art stays outside the
                // final-art gates until the U6/U11 accessory fit pass.
                Assert.That(definition.RequiredInFirstPlayable, Is.False, accessory.Id);
                Assert.That(definition.RequiresFinalArtForPlayerFacingAcceptance, Is.False, accessory.Id);
            }
        }

        [Test]
        public void PartyStationObjectSpriteKeysAreCatalogedOrIntentionalPlaceholders()
        {
            foreach (var station in PartyStationDefinitions.All)
            {
                foreach (var seed in station.Seeds)
                {
                    foreach (var item in station.ResolveObjects(seed))
                    {
                        var resolvable = AssetCatalog.TryGetDefinition(item.SpriteKey, out _)
                            || item.SpriteKey.StartsWith(PartyStationValidator.PlaceholderSpritePrefix);

                        Assert.That(resolvable, Is.True, $"{seed.SeedId}.{item.ObjectId}: {item.SpriteKey}");
                    }
                }
            }
        }
    }
}
