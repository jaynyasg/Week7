using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Single coordinate truth for the Design Build Studio room: the editor
    /// prefab builder bakes anchors at these positions, and the runtime
    /// controller falls back to the same constants when the authored prefab
    /// (or an individual anchor) is missing — gameplay never breaks.
    /// </summary>
    public static class DesignBuildStudioLayout
    {
        /// <summary>Resources path of the runtime copy of the room prefab.</summary>
        public const string PrefabResourcePath = "CareerQuest/World/DesignBuildStudio";

        public const string SlotAnchorPrefix = "SlotAnchor_";
        public const string TrayAnchorPrefix = "TrayAnchor_";
        public const string BuilderNpcName = "BuilderNpc";
        public const string PlayfieldName = "DesignBuildPlayfield";

        public static readonly Vector2 NpcPosition = new(3.65f, -1.2f);
        public static readonly Vector2 PieceWorldSize = new(0.9f, 0.9f);

        private static readonly float[] SlotColumns = { -2.45f, -1.2f, 0f, 1.2f, 2.45f };
        private const float SlotY = -0.27f;

        private const float TrayStartX = -2.4f;
        private const float TrayStepX = 1.2f;
        private const float TrayY = -2.05f;

        /// <summary>Blueprint-table slot position for a piece index.</summary>
        public static Vector2 SlotPosition(int pieceIndex)
        {
            var column = Mathf.Clamp(pieceIndex, 0, SlotColumns.Length - 1);
            return new Vector2(SlotColumns[column], SlotY);
        }

        /// <summary>Tray rest position for a piece index.</summary>
        public static Vector2 TrayPosition(int pieceIndex)
        {
            return new Vector2(TrayStartX + pieceIndex * TrayStepX, TrayY);
        }
    }
}
