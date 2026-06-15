using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Follow-mode client of the CameraDirector. The legacy per-frame camera
    /// writer moved into the director; this rig only requests hub follow framing
    /// (CameraFollowSettings.HubDefault preserves the old clamp/lerp math) and
    /// releases the camera back to the route shot when the hub hides.
    /// </summary>
    public class HubCameraRig : MonoBehaviour
    {
        private CameraDirector _director;
        private Transform _target;

        public void Configure(CameraDirector director, Transform target)
        {
            _director?.EndFollow(_target);
            _director = director;
            _target = target;

            if (_director == null || _target == null)
            {
                return;
            }

            _director.BeginFollow(_target, CameraFollowSettings.HubDefault);
        }

        public void ClearFollow()
        {
            _director?.EndFollow(_target);
            _target = null;
        }

        private void OnDisable()
        {
            ClearFollow();
        }
    }
}
