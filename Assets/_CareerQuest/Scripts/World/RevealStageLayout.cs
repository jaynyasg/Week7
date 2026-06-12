using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Single coordinate truth for the Reveal Stage (U7): the editor prefab
    /// builder bakes anchors at these positions, and the runtime fallback stage
    /// plus the cinematic director use the same constants when the authored
    /// prefab (or an individual anchor) is missing — the reveal never breaks.
    /// Mirrors the DesignBuildStudioLayout convention.
    /// </summary>
    public static class RevealStageLayout
    {
        /// <summary>Resources path of the runtime copy of the stage prefab.</summary>
        public const string PrefabResourcePath = "CareerQuest/World/RevealStage";

        public const string StageRootName = "RevealStage";
        public const string SlotAnchorPrefix = "TokenSlotAnchor_";
        public const string HeroAvatarName = "RevealHeroAvatar";
        public const string GlowBeamLeftName = "RevealGlowBeamLeft";
        public const string GlowBeamRightName = "RevealGlowBeamRight";
        public const string GlowSpotName = "RevealGlowSpot";
        public const string TokenLayerName = "RevealTokenLayer";
        public const string TokenNamePrefix = "RevealToken";

        public const int SlotCount = 3;

        /// <summary>Badge-token world size (sticker scale on the stage).</summary>
        public static readonly Vector2 TokenWorldSize = new(0.62f, 0.62f);

        /// <summary>Wide route framing (matches the room route shot).</summary>
        public static readonly CameraShot FallbackWideShot = CameraShot.Default;

        /// <summary>Cinematic close shot on the stage.</summary>
        public static readonly CameraShot FallbackStageShot =
            new(new Vector3(0f, -0.2f, -10f), 3.4f);

        /// <summary>Locked-branch settle shot: a gentle push-in, never the full cinematic.</summary>
        public static readonly CameraShot SettleShot =
            new(new Vector3(0f, -0.15f, -10f), 4.2f);

        /// <summary>Hero avatar stage mark (center stage, in front of the platform).</summary>
        public static readonly Vector2 HeroAvatarPosition = new(0f, -1.15f);

        /// <summary>Stage-center burst point for the unlock particles.</summary>
        public static readonly Vector2 StageCenter = new(0f, -0.2f);

        /// <summary>Badge-token slot position above the stage for a slot index.</summary>
        public static Vector2 SlotPosition(int index)
        {
            return index switch
            {
                0 => new Vector2(-1.55f, 0.42f),
                1 => new Vector2(0f, 0.62f),
                _ => new Vector2(1.55f, 0.42f)
            };
        }

        /// <summary>Where a traveling token spawns before flying to its slot.</summary>
        public static Vector2 TokenSpawnPosition(int index)
        {
            return new Vector2(SlotPosition(index).x * 0.4f, 2.75f);
        }
    }
}
