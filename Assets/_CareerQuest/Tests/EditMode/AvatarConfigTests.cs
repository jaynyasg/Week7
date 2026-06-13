using CareerQuest;
using NUnit.Framework;

namespace CareerQuest.Tests
{
    public class AvatarConfigTests
    {
        [Test]
        public void EveryAvatarHasCatalogedSpriteAndNpcIdentity()
        {
            foreach (var avatar in AvatarConfig.Avatars)
            {
                Assert.That(avatar.SpriteAssetId, Is.Not.Empty, avatar.Id);
                Assert.That(AssetCatalog.TryGetDefinition(avatar.SpriteAssetId, out var spriteDefinition), Is.True, avatar.Id);
                Assert.That(spriteDefinition.Category, Is.EqualTo(AssetCategory.Avatar), avatar.Id);

                Assert.That(avatar.NpcAssetId, Is.Not.Empty, avatar.Id);
                Assert.That(AssetCatalog.TryGetDefinition(avatar.NpcAssetId, out var npcDefinition), Is.True, avatar.Id);
                Assert.That(npcDefinition.Category, Is.EqualTo(AssetCategory.Npc), avatar.Id);

                Assert.That(avatar.PaletteId, Is.Not.Empty, avatar.Id);
                Assert.That(avatar.PersonalityLabel, Is.Not.Empty, avatar.Id);
            }
        }

        [Test]
        public void UnknownAvatarFallsBackToDefault()
        {
            var avatar = AvatarConfig.GetAvatar("not_real");

            Assert.That(avatar.Id, Is.EqualTo(AvatarConfig.DefaultAvatarId));
        }

        [Test]
        public void EveryAvatarHasCharacterCardCopy()
        {
            foreach (var avatar in AvatarConfig.Avatars)
            {
                Assert.That(avatar.DisplayName, Is.Not.Empty, avatar.Id);
                Assert.That(avatar.Role, Is.Not.Empty, avatar.Id);
                Assert.That(avatar.Role.Length, Is.LessThanOrEqualTo(32), avatar.Id);
                Assert.That(avatar.PersonalityLabel.Length, Is.LessThanOrEqualTo(80), avatar.Id);
            }
        }

        /// <summary>
        /// U11 base-avatar polish pass (R1 / Character Visual Acceptance Bar):
        /// the cast carries a visibly cleaner proportion vs. the pre-U11 set.
        /// Pixel quality is owner-judged, so this is a STRUCTURAL assertion — the
        /// config produces a per-avatar RenderScale strictly above the single
        /// legacy 0.75 every avatar used before, within a sane gameplay range.
        /// </summary>
        [Test]
        public void EveryAvatarShowsAPolishPassProportionAboveTheLegacyBaseline()
        {
            foreach (var avatar in AvatarConfig.Avatars)
            {
                Assert.That(avatar.RenderScale, Is.GreaterThan(AvatarConfig.LegacyRenderScale), avatar.Id);
                // Stays a believable on-campus character size (never a giant).
                Assert.That(avatar.RenderScale, Is.InRange(AvatarConfig.LegacyRenderScale, 1.1f), avatar.Id);
            }
        }

        /// <summary>
        /// The polish pass authors PER-AVATAR proportions (more distinct
        /// characters), not one shared bump — so the set has at least two
        /// different RenderScale values.
        /// </summary>
        [Test]
        public void PolishPassProportionsVaryAcrossTheCast()
        {
            var distinct = new System.Collections.Generic.HashSet<float>();
            foreach (var avatar in AvatarConfig.Avatars)
            {
                distinct.Add(avatar.RenderScale);
            }

            Assert.That(distinct.Count, Is.GreaterThanOrEqualTo(2),
                "The polish pass tunes proportions per avatar, not one shared value.");
        }
    }
}
