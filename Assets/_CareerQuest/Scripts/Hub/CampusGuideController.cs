using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// The campus guide NPC: curated guide art (npc.campus_guide — the Robot,
    /// never confusable with a player avatar) with a DESIGN.md speech bubble
    /// instead of the legacy GuidePrompt TextMesh (the last hub TextMesh dies
    /// here, U5), plus door-pulse pointer support for the first-run beat.
    /// </summary>
    public class CampusGuideController : MonoBehaviour
    {
        public const string GuideSpriteAssetId = "npc.campus_guide";

        private string _defaultPrompt = string.Empty;
        private DoorSign _pointedDoor;

        public SpeechBubble Bubble { get; private set; }
        public DoorSign PointedDoor => _pointedDoor;

        public void Configure(string prompt)
        {
            var view = gameObject.GetComponent<AvatarRuntimeView>() ?? gameObject.AddComponent<AvatarRuntimeView>();
            view.ApplySpriteAsset(GuideSpriteAssetId);

            _defaultPrompt = prompt ?? string.Empty;

            if (Bubble == null)
            {
                Bubble = SpeechBubble.Attach(transform, new Vector3(0.55f, 0.85f, 0f));
            }

            Bubble.Show(_defaultPrompt);
        }

        /// <summary>Speaks a line; duration ≤ 0 keeps it until the next Say/reset.</summary>
        public void Say(string line, float durationSeconds = 0f)
        {
            if (Bubble == null)
            {
                Bubble = SpeechBubble.Attach(transform, new Vector3(0.55f, 0.85f, 0f));
            }

            Bubble.Show(line, durationSeconds);
        }

        /// <summary>Restores the standing campus prompt.</summary>
        public void SayDefaultPrompt()
        {
            Say(_defaultPrompt);
        }

        /// <summary>Door-pulse pointer: the pointed sign+door pulse per DESIGN motion rules.</summary>
        public void PointToDoor(DoorSign sign)
        {
            StopPointing();
            _pointedDoor = sign;
            if (_pointedDoor != null)
            {
                _pointedDoor.SetPulsing(true);
            }
        }

        public void StopPointing()
        {
            if (_pointedDoor != null)
            {
                _pointedDoor.SetPulsing(false);
                _pointedDoor = null;
            }
        }

        private void OnDestroy()
        {
            StopPointing();
        }
    }
}
