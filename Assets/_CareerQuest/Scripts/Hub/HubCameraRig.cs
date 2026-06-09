using UnityEngine;

namespace CareerQuest
{
    public class HubCameraRig : MonoBehaviour
    {
        private Camera _camera;
        private Transform _target;

        public void Configure(Camera camera, Transform target)
        {
            _camera = camera;
            _target = target;

            if (_camera == null)
            {
                return;
            }

            _camera.orthographic = true;
            _camera.orthographicSize = 4.15f;
        }

        private void LateUpdate()
        {
            if (_camera == null || _target == null)
            {
                return;
            }

            var targetPosition = new Vector3(
                Mathf.Clamp(_target.position.x * 0.2f, -0.8f, 0.8f),
                0f,
                -10f);
            _camera.transform.position = Vector3.Lerp(_camera.transform.position, targetPosition, 4f * Time.deltaTime);
        }
    }
}
