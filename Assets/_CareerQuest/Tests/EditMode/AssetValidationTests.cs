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
    }
}
