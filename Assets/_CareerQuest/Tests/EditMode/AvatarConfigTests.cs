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
    }
}
