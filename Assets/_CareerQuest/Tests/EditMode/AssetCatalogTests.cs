using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class AssetCatalogTests
    {
        [Test]
        public void FindsKnownAvatarAndBadgeDefinitions()
        {
            Assert.That(AssetCatalog.TryGetDefinition("avatar.sky_builder", out var avatar), Is.True);
            Assert.That(avatar.Category, Is.EqualTo(AssetCategory.Avatar));

            Assert.That(AssetCatalog.TryGetDefinition("badge.design_build", out var badge), Is.True);
            Assert.That(badge.Category, Is.EqualTo(AssetCategory.Badge));
        }

        [Test]
        public void MissingAssetReturnsVisibleFallbackSprite()
        {
            var sprite = AssetCatalog.SpriteFor("missing.asset.from.test");

            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(96));
            Assert.That(sprite.name, Does.Contain("missing.asset.from.test"));
        }
    }
}
