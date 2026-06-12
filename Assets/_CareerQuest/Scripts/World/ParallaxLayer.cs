using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// One parallax band of the campus diorama. Driven exclusively from
    /// CameraDirector.AfterCameraWrite (never LateUpdate ordering races): when
    /// the camera moves by delta, the band shifts by factor * delta, so a band
    /// with factor 1 is glued to the camera (infinite distance), factor 0 is
    /// world-static, and a small negative factor pops toward the viewer.
    /// ReAnchor() resets the band to its authored offset for the current
    /// camera position — called on route change so a room round-trip never
    /// leaves accumulated drift.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] private float factor;

        private CameraDirector _director;
        private Vector3 _baseLocalPosition;
        private bool _baseCaptured;

        public float Factor => factor;

        /// <summary>Editor-builder/runtime seam: sets the band factor.</summary>
        public void Configure(float parallaxFactor)
        {
            factor = parallaxFactor;
        }

        private void OnEnable()
        {
            CaptureBase();
            _director = CameraDirector.Ensure();
            _director.AfterCameraWrite += HandleCameraWrite;
            ReAnchor();
        }

        private void OnDisable()
        {
            if (_director != null)
            {
                _director.AfterCameraWrite -= HandleCameraWrite;
            }
        }

        private void HandleCameraWrite(Vector3 cameraDelta)
        {
            if (Mathf.Approximately(factor, 0f))
            {
                return;
            }

            transform.localPosition += new Vector3(cameraDelta.x * factor, cameraDelta.y * factor, 0f);
        }

        /// <summary>
        /// Re-anchors the band to its authored position adjusted for where the
        /// camera currently sits relative to the route shot. With the camera on
        /// the route shot this restores the exact authored layout.
        /// </summary>
        public void ReAnchor()
        {
            CaptureBase();
            var director = _director != null ? _director : CameraDirector.Ensure();
            var cameraDelta = director.Camera.transform.position - director.RouteShot.Position;
            transform.localPosition = _baseLocalPosition + new Vector3(cameraDelta.x * factor, cameraDelta.y * factor, 0f);
        }

        private void CaptureBase()
        {
            if (_baseCaptured)
            {
                return;
            }

            _baseLocalPosition = transform.localPosition;
            _baseCaptured = true;
        }
    }
}
