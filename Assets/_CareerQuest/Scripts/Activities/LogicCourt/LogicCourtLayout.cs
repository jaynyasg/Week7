using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Single coordinate truth for the Logic Court room (U10, mirroring
    /// <see cref="DesignBuildStudioLayout"/>): the editor prefab builder bakes
    /// anchors at these positions, and the runtime controller falls back to the
    /// same constants when the authored prefab (or an individual anchor) is
    /// missing — gameplay never breaks.
    ///
    /// Room model (DESIGN.md Logic Court: judge NPC, evidence cards, sorting
    /// zones, podium, conclusion stamp): drag the case file to the podium to
    /// review the case, then sort each evidence card into the Helpful or
    /// Not Helpful zone. The judge stamps the conclusion on completion (P14).
    /// </summary>
    public static class LogicCourtLayout
    {
        /// <summary>Resources path of the runtime copy of the room prefab.</summary>
        public const string PrefabResourcePath = "CareerQuest/World/LogicCourt";

        public const string ZoneAnchorPrefix = "CourtZoneAnchor_";
        public const string TrayAnchorPrefix = "CourtTrayAnchor_";
        public const string JudgeNpcName = "JudgeNpc";
        public const string StampPropName = "ConclusionStamp";
        public const string PlayfieldName = "LogicCourtPlayfield";

        public const string HelpfulZoneId = "helpful";
        public const string NotHelpfulZoneId = "not_helpful";
        public const string PodiumZoneId = "podium";

        public const string CaseFilePieceId = "case_file";
        public const string EvidenceTestPieceId = "evidence_test";
        public const string EvidencePaintPieceId = "evidence_paint";
        public const string EvidenceBlueprintPieceId = "evidence_blueprint";

        /// <summary>Every draggable piece (held-piece domain). Case file rides tray slot 0.</summary>
        public static readonly string[] PieceIds =
        {
            CaseFilePieceId,
            EvidenceTestPieceId,
            EvidencePaintPieceId,
            EvidenceBlueprintPieceId
        };

        /// <summary>Evidence cards (network step index = array index; P13 shuffle domain).</summary>
        public static readonly string[] EvidencePieceIds =
        {
            EvidenceTestPieceId,
            EvidencePaintPieceId,
            EvidenceBlueprintPieceId
        };

        public static readonly Vector2 NpcPosition = new(-2.05f, -1.02f);
        public static readonly Vector2 PieceWorldSize = new(0.85f, 0.85f);

        public static readonly Vector2 PodiumZonePosition = new(-0.2f, -0.5f);
        public static readonly Vector2 PodiumZoneSize = new(1.0f, 1.0f);
        public static readonly Vector2 HelpfulZonePosition = new(1.05f, -0.55f);
        public static readonly Vector2 NotHelpfulZonePosition = new(2.55f, -0.55f);
        public static readonly Vector2 SortingZoneSize = new(1.4f, 0.95f);

        public static readonly Vector2 StampPosition = new(-1.35f, 0.12f);

        private const float TrayStartX = -1.8f;
        private const float TrayStepX = 1.2f;
        private const float TrayY = -2.05f;

        /// <summary>Evidence tray rest position for a tray slot index (slot 0 = case file).</summary>
        public static Vector2 TrayPosition(int trayIndex)
        {
            return new Vector2(TrayStartX + trayIndex * TrayStepX, TrayY);
        }

        /// <summary>The zone an evidence card belongs in (the existing rules: test/blueprint helpful, paint not).</summary>
        public static string CorrectZoneFor(string pieceId)
        {
            switch (pieceId)
            {
                case EvidenceTestPieceId:
                case EvidenceBlueprintPieceId:
                    return HelpfulZoneId;
                case EvidencePaintPieceId:
                    return NotHelpfulZoneId;
                default:
                    return null;
            }
        }

        /// <summary>Where an accepted piece parks (cards fan out inside their zone).</summary>
        public static Vector2 LockPosition(string pieceId)
        {
            switch (pieceId)
            {
                case CaseFilePieceId:
                    return new Vector2(-0.2f, -0.4f);
                case EvidenceTestPieceId:
                    return new Vector2(0.75f, -0.55f);
                case EvidenceBlueprintPieceId:
                    return new Vector2(1.35f, -0.55f);
                case EvidencePaintPieceId:
                    return new Vector2(2.55f, -0.55f);
                default:
                    return Vector2.zero;
            }
        }
    }
}
