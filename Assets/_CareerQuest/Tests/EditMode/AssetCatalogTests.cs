using CareerQuest;
using NUnit.Framework;
using UnityEngine;

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
            var resolution = AssetCatalog.ResolveSprite("missing.asset.from.test");
            var sprite = resolution.Sprite;

            Assert.That(sprite, Is.Not.Null);
            Assert.That(sprite.texture.width, Is.EqualTo(96));
            Assert.That(sprite.name, Does.Contain("missing.asset.from.test"));
            Assert.That(resolution.IsCataloged, Is.False);
            Assert.That(resolution.IsMissingDefinition, Is.True);
            Assert.That(resolution.IsFallbackGenerated, Is.True);
            Assert.That(resolution.IsFinalArt, Is.False);
            Assert.That(AssetCatalog.IsFallbackSprite(sprite), Is.True);
            Assert.That(AssetCatalog.IsFinalArtSprite(sprite), Is.False);
            Assert.That(AssetCatalog.SpriteFor("missing.asset.from.test"), Is.SameAs(sprite));
        }

        [Test]
        public void CatalogResolutionReportsFallbackOrFinalArtStatus()
        {
            var resolution = AssetCatalog.ResolveSprite("avatar.sky_builder");

            Assert.That(resolution.Sprite, Is.Not.Null);
            Assert.That(resolution.Definition.Id, Is.EqualTo("avatar.sky_builder"));
            Assert.That(resolution.IsCataloged, Is.True);
            Assert.That(resolution.IsMissingDefinition, Is.False);
            Assert.That(resolution.IsFinalArt, Is.EqualTo(!resolution.IsFallbackGenerated));
            Assert.That(resolution.IsPlayerFacingFallback, Is.EqualTo(resolution.Definition.RequiresFinalArtForPlayerFacingAcceptance && resolution.IsFallbackGenerated));
            Assert.That(AssetCatalog.SpriteFor("avatar.sky_builder"), Is.SameAs(resolution.Sprite));
        }

        [Test]
        public void DisplayedCatalogSpriteCanBeMappedBackToResolution()
        {
            var sprite = AssetCatalog.SpriteFor("badge.design_build");

            Assert.That(AssetCatalog.TryGetDisplayedSpriteInfo(sprite, out var displayed), Is.True);
            Assert.That(displayed.RequestedId, Is.EqualTo("badge.design_build"));
            Assert.That(displayed.Sprite, Is.SameAs(sprite));
            Assert.That(displayed.IsPlayerFacingFallback, Is.EqualTo(displayed.Definition.RequiresFinalArtForPlayerFacingAcceptance && displayed.IsFallbackGenerated));
        }

        [Test]
        public void FallbackFactorySpritesAreIdentifiableAsQaFallbacks()
        {
            var definition = AssetCatalog.GetDefinition("prop.blueprint");
            var fallback = SpriteFallbackFactory.Create(definition);

            Assert.That(AssetCatalog.IsFallbackSprite(fallback), Is.True);
            Assert.That(SpriteFallbackFactory.IsFallbackTexture(fallback.texture), Is.True);
            Assert.That(AssetCatalog.IsFinalArtSprite(fallback), Is.False);
        }

        [Test]
        public void UncatalogedDisplayedSpriteDoesNotCountAsFinalArt()
        {
            var texture = new Texture2D(16, 16, TextureFormat.RGBA32, false)
            {
                name = "uncataloged.texture",
                hideFlags = HideFlags.HideAndDontSave
            };
            var sprite = Sprite.Create(texture, new Rect(0f, 0f, 16f, 16f), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "uncataloged.sprite";
            sprite.hideFlags = HideFlags.HideAndDontSave;

            Assert.That(AssetCatalog.TryGetDisplayedSpriteInfo(sprite, out var displayed), Is.True);
            Assert.That(displayed.IsCataloged, Is.False);
            Assert.That(displayed.IsFinalArt, Is.False);
            Assert.That(AssetCatalog.IsFinalArtSprite(sprite), Is.False);
        }
    }
}
