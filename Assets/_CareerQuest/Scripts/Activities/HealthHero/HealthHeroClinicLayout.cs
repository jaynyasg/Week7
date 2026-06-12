using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Single coordinate truth for the Health Hero Clinic room (U10, mirroring
    /// <see cref="DesignBuildStudioLayout"/>): the editor prefab builder bakes
    /// anchors at these positions, and the runtime controller falls back to the
    /// same constants when the authored prefab (or an individual anchor) is
    /// missing — gameplay never breaks.
    ///
    /// Room model (DESIGN.md Health Hero Clinic: patient NPC, symptom clipboard,
    /// care tools, warm table, care plan board): the tool tray holds four
    /// draggable pieces; care flows by dragging each step's piece onto the
    /// patient zone in order — clipboard (check symptoms) → thermometer (tool)
    /// → care plan. The bandage is the wrong-tool piece and always bounces with
    /// gentle teaching copy.
    /// </summary>
    public static class HealthHeroClinicLayout
    {
        /// <summary>Resources path of the runtime copy of the room prefab.</summary>
        public const string PrefabResourcePath = "CareerQuest/World/HealthHeroClinic";

        public const string ZoneAnchorPrefix = "ClinicZoneAnchor_";
        public const string TrayAnchorPrefix = "ClinicTrayAnchor_";
        public const string AppliedAnchorPrefix = "ClinicAppliedAnchor_";
        public const string PatientNpcName = "PatientNpc";
        public const string PlayfieldName = "HealthHeroPlayfield";

        public const string PatientZoneId = "patient";

        public const string SymptomClipboardPieceId = "symptom_clipboard";
        public const string ThermometerPieceId = "thermometer";
        public const string BandagePieceId = "bandage";
        public const string CarePlanPieceId = "care_plan";

        /// <summary>Every draggable piece in the tool tray (tray-shuffle domain — P13).</summary>
        public static readonly string[] PieceIds =
        {
            SymptomClipboardPieceId,
            ThermometerPieceId,
            BandagePieceId,
            CarePlanPieceId
        };

        /// <summary>Care steps in required order (network step index = array index).</summary>
        public static readonly string[] StepPieceIds =
        {
            SymptomClipboardPieceId,
            ThermometerPieceId,
            CarePlanPieceId
        };

        public static readonly Vector2 NpcPosition = new(-1.82f, -1.04f);
        public static readonly Vector2 PieceWorldSize = new(0.85f, 0.85f);

        public static readonly Vector2 PatientZonePosition = new(-1.82f, -0.55f);
        public static readonly Vector2 PatientZoneSize = new(2.3f, 1.1f);

        private const float TrayStartX = -1.8f;
        private const float TrayStepX = 1.2f;
        private const float TrayY = -2.05f;

        /// <summary>Tool tray rest position for a tray slot index.</summary>
        public static Vector2 TrayPosition(int trayIndex)
        {
            return new Vector2(TrayStartX + trayIndex * TrayStepX, TrayY);
        }

        /// <summary>Where an accepted step's piece parks (clipboard at the bed head, tool on the patient, plan on the care board).</summary>
        public static Vector2 AppliedPosition(string pieceId)
        {
            switch (pieceId)
            {
                case SymptomClipboardPieceId:
                    return new Vector2(-2.75f, -0.35f);
                case ThermometerPieceId:
                    return new Vector2(-1.5f, -0.4f);
                case CarePlanPieceId:
                    return new Vector2(2.6f, 0.55f);
                default:
                    return PatientZonePosition;
            }
        }
    }
}
