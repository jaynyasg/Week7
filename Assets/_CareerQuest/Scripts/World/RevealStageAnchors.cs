using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Camera shot anchors exported as data by the authored RevealStage prefab
    /// (U7): the wide route framing and the cinematic stage close shot live on
    /// the prefab root, so the shots are authored beside the stage geometry.
    /// When no stage is mounted (or the prefab is missing) the static resolvers
    /// fall back to the RevealStageLayout constants — the cinematic never breaks.
    /// </summary>
    public class RevealStageAnchors : MonoBehaviour
    {
        [SerializeField] private CameraShot wideShot = RevealStageLayout.FallbackWideShot;
        [SerializeField] private CameraShot stageShot = RevealStageLayout.FallbackStageShot;

        public CameraShot WideShot => wideShot;
        public CameraShot StageShot => stageShot;

        /// <summary>Editor-builder seam: populates the serialized data before SaveAsPrefabAsset.</summary>
        public void SetData(CameraShot wide, CameraShot stage)
        {
            wideShot = wide;
            stageShot = stage;
        }

        /// <summary>Live mounted stage shot, else the layout fallback.</summary>
        public static CameraShot ResolveStageShot()
        {
            var live = FindFirstObjectByType<RevealStageAnchors>();
            return live != null ? live.StageShot : RevealStageLayout.FallbackStageShot;
        }

        /// <summary>Live mounted wide shot, else the layout fallback.</summary>
        public static CameraShot ResolveWideShot()
        {
            var live = FindFirstObjectByType<RevealStageAnchors>();
            return live != null ? live.WideShot : RevealStageLayout.FallbackWideShot;
        }
    }
}
