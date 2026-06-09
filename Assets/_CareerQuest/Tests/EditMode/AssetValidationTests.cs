using System;
using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class AssetValidationTests
    {
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
        public void RequiredFallbackSpritesStayWithinTextureBudget()
        {
            foreach (var definition in AssetCatalog.RequiredDefinitions)
            {
                var sprite = AssetCatalog.SpriteFor(definition.Id);

                Assert.That(sprite, Is.Not.Null, definition.Id);
                Assert.That(sprite.texture.width, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
                Assert.That(sprite.texture.height, Is.InRange(1, AssetCatalog.MaxFallbackTextureSize), definition.Id);
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
    }
}
