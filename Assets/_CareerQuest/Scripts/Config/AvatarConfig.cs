using UnityEngine;

namespace CareerQuest
{
    public class AvatarDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Role { get; }
        public Color ShirtColor { get; }
        public Color AccentColor { get; }
        public string SpriteAssetId { get; }
        public string PaletteId { get; }
        public string PersonalityLabel { get; }
        public string NpcAssetId { get; }

        /// <summary>
        /// U11 character polish pass: the rest-pose render height in avatar-local
        /// units (the AvatarRuntimeView base scale). The pre-U11 set used one
        /// hardcoded 0.75 for every avatar; the polish pass authors a slightly
        /// taller, per-avatar proportion so the cast reads as cleaner, more
        /// distinct game characters at gameplay scale (and the accessory anchors,
        /// which derive from the host sprite extents, sit correctly on top).
        /// Final pixel quality is owner-judged; this is the structural knob the
        /// AvatarConfigTests polish-pass assertion checks.
        /// </summary>
        public float RenderScale { get; }

        public AvatarDefinition(
            string id,
            string displayName,
            string role,
            Color shirtColor,
            Color accentColor,
            string spriteAssetId,
            string paletteId,
            string personalityLabel,
            string npcAssetId,
            float renderScale)
        {
            Id = id;
            DisplayName = displayName;
            Role = role;
            ShirtColor = shirtColor;
            AccentColor = accentColor;
            SpriteAssetId = spriteAssetId;
            PaletteId = paletteId;
            PersonalityLabel = personalityLabel;
            NpcAssetId = npcAssetId;
            RenderScale = renderScale;
        }
    }

    public static class AvatarConfig
    {
        public const string DefaultAvatarId = "sky_builder";

        /// <summary>
        /// The pre-U11 single hardcoded base scale every avatar used. The U11
        /// polish pass authors a taller, per-avatar <see cref="AvatarDefinition.RenderScale"/>
        /// strictly above this, so the upgrade is a structural, testable fact
        /// (AvatarConfigTests) rather than only an owner-judged pixel change.
        /// </summary>
        public const float LegacyRenderScale = 0.75f;

        public static readonly AvatarDefinition[] Avatars =
        {
            new(
                "sky_builder",
                "Sky Builder",
                "Future city maker",
                new Color(0.12f, 0.43f, 0.86f),
                new Color(0.83f, 0.96f, 1f),
                "avatar.sky_builder",
                "sky-blue",
                "Plans big builds and spots patterns in places.",
                "npc.builder_partner",
                0.688f),
            new(
                "care_captain",
                "Care Captain",
                "Health helper",
                new Color(0.05f, 0.55f, 0.5f),
                new Color(0.36f, 0.78f, 0.6f),
                "avatar.care_captain",
                "care-teal",
                "Notices how people feel and chooses kind tools.",
                "npc.patient",
                0.664f),
            new(
                "logic_spark",
                "Logic Spark",
                "Evidence solver",
                new Color(0.93f, 0.55f, 0.12f),
                new Color(0.96f, 0.86f, 0.35f),
                "avatar.logic_spark",
                "logic-gold",
                "Finds clues, checks facts, and explains ideas clearly.",
                "npc.judge",
                0.672f),
            new(
                "art_inventor",
                "Art Inventor",
                "Creative problem solver",
                new Color(0.62f, 0.52f, 0.86f),
                new Color(0.94f, 0.34f, 0.28f),
                "avatar.art_inventor",
                "inventor-lilac",
                "Mixes imagination with experiments to make new things.",
                "npc.campus_guide",
                0.656f)
        };

        public static AvatarDefinition DefaultAvatar => GetAvatar(DefaultAvatarId);

        public static AvatarDefinition GetAvatar(string id)
        {
            foreach (var avatar in Avatars)
            {
                if (avatar.Id == id)
                {
                    return avatar;
                }
            }

            return Avatars[0];
        }

        public static AvatarDefinition GetAvatarAt(int index)
        {
            return index >= 0 && index < Avatars.Length ? Avatars[index] : DefaultAvatar;
        }

        public static int IndexForAvatar(string id)
        {
            for (var i = 0; i < Avatars.Length; i++)
            {
                if (Avatars[i].Id == id)
                {
                    return i;
                }
            }

            return 0;
        }
    }
}
