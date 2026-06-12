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

        /// <summary>
        /// U11: every CareerQuestCatalog badge art key — including the four
        /// optional rooms — resolves to a cataloged Badge definition that the
        /// fallback gate polices.
        /// </summary>
        [Test]
        public void EveryCatalogBadgeArtKeyIsACatalogedPlayerFacingBadge()
        {
            foreach (var entry in CareerQuestCatalog.All)
            {
                Assert.That(AssetCatalog.TryGetDefinition(entry.BadgeArtKey, out var definition), Is.True, entry.BadgeArtKey);
                Assert.That(definition.Category, Is.EqualTo(AssetCategory.Badge), entry.BadgeArtKey);
                Assert.That(definition.RequiredInFirstPlayable, Is.True, entry.BadgeArtKey);
                Assert.That(definition.RequiresFinalArtForPlayerFacingAcceptance, Is.True, entry.BadgeArtKey);
            }
        }

        [Test]
        public void OptionalRoomBadgeDefinitionsExistWithDistinctCareerColors()
        {
            var badgeIds = new[]
            {
                "badge.ai_lab",
                "badge.music_studio",
                "badge.robotics_garage",
                "badge.community_kitchen"
            };

            var colors = new System.Collections.Generic.List<Color>();
            foreach (var badgeId in badgeIds)
            {
                Assert.That(AssetCatalog.TryGetDefinition(badgeId, out var definition), Is.True, badgeId);
                Assert.That(definition.Category, Is.EqualTo(AssetCategory.Badge), badgeId);
                colors.Add(definition.PrimaryColor);
            }

            // Career identity: the four badges must not share a ring color.
            for (var a = 0; a < colors.Count; a++)
            {
                for (var b = a + 1; b < colors.Count; b++)
                {
                    Assert.That(colors[a] != colors[b], Is.True,
                        $"{badgeIds[a]} and {badgeIds[b]} must use distinct career identity colors.");
                }
            }
        }

        [Test]
        public void OptionalRoomInteriorAndCityPieceDefinitionsExist()
        {
            foreach (var roomId in new[] { "room.ai_lab", "room.music_studio", "room.robotics_garage", "room.community_kitchen" })
            {
                Assert.That(AssetCatalog.TryGetDefinition(roomId, out var room), Is.True, roomId);
                Assert.That(room.Category, Is.EqualTo(AssetCategory.Room), roomId);
            }

            foreach (var propId in new[] { "prop.city_piece_garage", "prop.city_piece_kitchen" })
            {
                Assert.That(AssetCatalog.TryGetDefinition(propId, out var prop), Is.True, propId);
                Assert.That(prop.Category, Is.EqualTo(AssetCategory.Prop), propId);
            }
        }

        [Test]
        public void FrameSetResolvesContiguousCuratedFrames()
        {
            // U5 frame-set convention: Resources/CareerQuest/{Category}/{id}.{state}{n}.png.
            var walkFrames = AssetCatalog.FrameSetFor("avatar.sky_builder", AssetCatalog.FrameStateWalk);
            Assert.That(walkFrames.Count, Is.GreaterThanOrEqualTo(2),
                "Curated walk frames are missing — run CareerQuestCharacterArtCurator.Curate.");
            Assert.That(walkFrames, Is.All.Not.Null);

            var celebrateFrames = AssetCatalog.FrameSetFor("avatar.sky_builder", AssetCatalog.FrameStateCelebrate);
            Assert.That(celebrateFrames.Count, Is.GreaterThanOrEqualTo(2),
                "Curated celebrate (cheer) frames are missing — run CareerQuestCharacterArtCurator.Curate.");

            var guideCelebrate = AssetCatalog.FrameSetFor("npc.campus_guide", AssetCatalog.FrameStateCelebrate);
            Assert.That(guideCelebrate.Count, Is.GreaterThanOrEqualTo(2),
                "NPC celebrate frames are missing — run CareerQuestCharacterArtCurator.Curate.");
        }

        [Test]
        public void FrameSetIsSafeForMissingIdsAndStates()
        {
            // The fallback contract: missing frames yield an empty set, never throw.
            Assert.That(AssetCatalog.FrameSetFor(null, AssetCatalog.FrameStateWalk), Is.Empty);
            Assert.That(AssetCatalog.FrameSetFor("avatar.sky_builder", null), Is.Empty);
            Assert.That(AssetCatalog.FrameSetFor("not.a.catalog.id", AssetCatalog.FrameStateWalk), Is.Empty);
            Assert.That(AssetCatalog.FrameSetFor("avatar.sky_builder", "unknown_state"), Is.Empty);
            // Props have no frame sets — empty, not an error.
            Assert.That(AssetCatalog.FrameSetFor("prop.blueprint", AssetCatalog.FrameStateWalk), Is.Empty);
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
