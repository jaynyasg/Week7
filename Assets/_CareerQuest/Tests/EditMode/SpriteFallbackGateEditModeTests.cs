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

        /// <summary>
        /// R3/AE4 (U11): the gate covers the FULL player-facing catalog —
        /// including the optional-room campus buildings (flags flipped to
        /// required), the four optional badges, the optional-room interiors,
        /// and the two new city pieces. Fails loudly until
        /// CareerQuestOptionalArtBuilder.Generate has been run.
        /// </summary>
        [Test]
        public void FullPlayerFacingCatalogResolvesToFinalArt()
        {
            var fallbackIds = AssetCatalog.ResolvePlayerFacingSprites()
                .Where(resolution => !resolution.IsFinalArt)
                .Select(resolution => resolution.RequestedId)
                .ToArray();

            Assert.That(
                fallbackIds,
                Is.Empty,
                "Player-facing assets must ship with final Resources art — run "
                + "CareerQuestOptionalArtBuilder.Generate (menu: Career Quest/Art/"
                + "Generate Optional Surface Art) for optional surfaces. Fallbacks: "
                + string.Join(", ", fallbackIds));
        }

        [Test]
        public void OptionalRoomSurfacesAreInsideTheRequiredGate()
        {
            var requiredIds = AssetCatalog.RequiredDefinitions
                .Select(definition => definition.Id)
                .ToArray();

            var optionalSurfaceIds = new[]
            {
                "badge.ai_lab",
                "badge.music_studio",
                "badge.robotics_garage",
                "badge.community_kitchen",
                "campus.space_lab",
                "campus.music_studio",
                "campus.green_energy_center",
                "campus.robotics_garage",
                "campus.community_kitchen",
                "room.ai_lab",
                "room.music_studio",
                "room.robotics_garage",
                "room.community_kitchen",
                "prop.city_piece_garage",
                "prop.city_piece_kitchen"
            };

            foreach (var id in optionalSurfaceIds)
            {
                Assert.That(requiredIds, Does.Contain(id), id);
            }
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
