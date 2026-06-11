using System.Linq;
using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class SpriteFallbackGateEditModeTests
    {
        [Test]
        public void RequiredFirstPlayableAssetsResolveToFinalArt()
        {
            var fallbackIds = AssetCatalog.RequiredDefinitions
                .Select(definition => AssetCatalog.ResolveSprite(definition.Id))
                .Where(resolution => !resolution.IsFinalArt)
                .Select(resolution => resolution.RequestedId)
                .ToArray();

            Assert.That(
                fallbackIds,
                Is.Empty,
                $"Required first-playable assets must ship with final Resources art, not runtime fallbacks: {string.Join(", ", fallbackIds)}");
        }

        [Test]
        public void FallbackGateDetectsGeneratedFallbackSprites()
        {
            var definition = AssetCatalog.GetDefinition("avatar.sky_builder");
            var fallback = SpriteFallbackFactory.Create(definition);

            Assert.That(AssetCatalog.IsFallbackSprite(fallback), Is.True);
            Assert.That(AssetCatalog.IsFinalArtSprite(fallback), Is.False);
        }
    }
}
