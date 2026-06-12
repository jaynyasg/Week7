using UnityEngine;

namespace CareerQuest
{
    public enum CameraDirectorMode
    {
        FixedShot,
        Follow,
        Tween
    }

    [System.Serializable]
    public struct CameraShot
    {
        public Vector3 Position;
        public float OrthographicSize;

        public CameraShot(Vector3 position, float orthographicSize)
        {
            Position = position;
            OrthographicSize = orthographicSize;
        }

        public static CameraShot Default => new CameraShot(new Vector3(0f, 0f, -10f), 4.5f);

        public bool Approximately(CameraShot other)
        {
            return (Position - other.Position).sqrMagnitude < 0.0001f
                && Mathf.Abs(OrthographicSize - other.OrthographicSize) < 0.001f;
        }
    }

    [System.Serializable]
    public struct CameraFollowSettings
    {
        public float OrthographicSize;
        public float HorizontalFactor;
        public float HorizontalClamp;
        public float LerpSpeed;
        public float PlaneY;
        public float PlaneZ;

        /// <summary>Framing that matches the legacy HubCameraRig behavior exactly.</summary>
        public static CameraFollowSettings HubDefault => new CameraFollowSettings
        {
            OrthographicSize = 4.15f,
            HorizontalFactor = 0.2f,
            HorizontalClamp = 0.8f,
            LerpSpeed = 4f,
            PlaneY = 0f,
            PlaneZ = -10f
        };
    }

    /// <summary>
    /// The single owner of the campus camera. Follow, route framing, and cinematic
    /// tweens are requests to this component; every exit path restores a known shot.
    /// All camera reads AND writes route through here — Camera.main access anywhere
    /// else in CareerQuest scripts is banned.
    /// The camera GameObject exposed via <see cref="CameraHost"/> is also the attach
    /// point for the Physics2DRaycaster (added in U6).
    /// </summary>
    public class CameraDirector : MonoBehaviour
    {
        private Camera _camera;
        private CameraShot _routeShot = CameraShot.Default;
        private CameraShot _heldShot = CameraShot.Default;

        private Transform _followTarget;
        private CameraFollowSettings _followSettings;

        private CameraShot _tweenFrom;
        private CameraShot _tweenTarget;
        private float _tweenDuration;
        private float _tweenElapsed;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        public CameraDirectorMode ActiveMode { get; private set; } = CameraDirectorMode.FixedShot;

        /// <summary>The only sanctioned source of the camera reference.</summary>
        public Camera Camera => EnsureCamera();

        /// <summary>Physics2DRaycaster attach point (raycaster itself lands in U6).</summary>
        public GameObject CameraHost => EnsureCamera().gameObject;

        /// <summary>The shot the active route restores to.</summary>
        public CameraShot RouteShot => _routeShot;

        /// <summary>The shot the director is currently holding or tweening toward.</summary>
        public CameraShot CurrentShot => ActiveMode == CameraDirectorMode.Tween ? _tweenTarget : _heldShot;

        /// <summary>True when the camera sits on the route's known shot.</summary>
        public bool IsRestored => ActiveMode == CameraDirectorMode.FixedShot && _heldShot.Approximately(_routeShot);

        /// <summary>Camera position delta produced by the most recent Tick.</summary>
        public Vector3 LastCameraDelta { get; private set; }

        /// <summary>
        /// Fired after every camera write inside Tick with the position delta —
        /// parallax consumers (U4) subscribe here so they always run post-write.
        /// </summary>
        public event System.Action<Vector3> AfterCameraWrite;

        public static CameraDirector Ensure()
        {
            var existing = FindFirstObjectByType<CameraDirector>();
            if (existing != null)
            {
                existing.EnsureCamera();
                return existing;
            }

            var directorObject = new GameObject("CameraDirector", typeof(CameraDirector));
            var director = directorObject.GetComponent<CameraDirector>();
            director.EnsureCamera();
            return director;
        }

        /// <summary>
        /// Adopt-or-create-and-tag: adopts an existing MainCamera, otherwise creates
        /// the campus camera AND tags it MainCamera so Camera.main is never null.
        /// </summary>
        public Camera EnsureCamera()
        {
            if (_camera != null)
            {
                return _camera;
            }

            _camera = Camera.main;
            if (_camera == null)
            {
                var cameraObject = new GameObject("CampusWorldCamera", typeof(Camera));
                cameraObject.tag = "MainCamera";
                _camera = cameraObject.GetComponent<Camera>();
                _camera.backgroundColor = CampusWorldPalette.Sky;
                _camera.clearFlags = CameraClearFlags.SolidColor;
                ApplyShot(_routeShot);
            }

            // The director-owned camera is also the game's ears: nothing else in
            // the project carries an AudioListener (the world is code/prefab
            // built), so without this every AudioSource plays into silence.
            if (_camera.GetComponent<AudioListener>() == null)
            {
                _camera.gameObject.AddComponent<AudioListener>();
            }

            return _camera;
        }

        /// <summary>
        /// Route changes always reset to that route's shot — the restoration
        /// guarantee every exit path relies on. Snaps immediately.
        /// </summary>
        public void SetRouteShot(CameraShot shot)
        {
            _routeShot = shot;
            SnapToShot(shot);
        }

        /// <summary>
        /// Forced reset (disconnect-style): cancels any follow/tween and restores
        /// the current route's shot immediately.
        /// </summary>
        public void ResetToRouteShot()
        {
            SnapToShot(_routeShot);
        }

        /// <summary>Follow mode (hub): tracks the target with the supplied framing.</summary>
        public void BeginFollow(Transform target, CameraFollowSettings settings)
        {
            if (target == null)
            {
                return;
            }

            var camera = EnsureCamera();
            _followTarget = target;
            _followSettings = settings;
            ClearTween();
            ActiveMode = CameraDirectorMode.Follow;
            camera.orthographic = true;
            camera.orthographicSize = settings.OrthographicSize;
        }

        /// <summary>
        /// Ends follow mode (if this target — or a destroyed one — owns it) and
        /// restores the route shot.
        /// </summary>
        public void EndFollow(Transform target)
        {
            if (ActiveMode != CameraDirectorMode.Follow)
            {
                return;
            }

            if (target != null && _followTarget != null && _followTarget != target)
            {
                return;
            }

            SnapToShot(_routeShot);
        }

        /// <summary>
        /// Cinematic tween toward a shot. Starting a tween while another is active
        /// cancels the first cleanly: the new tween re-samples from the camera's
        /// current state, so there is never a jump past the old target.
        /// </summary>
        public void TweenToShot(CameraShot shot, float durationSeconds)
        {
            var camera = EnsureCamera();
            _followTarget = null;

            if (durationSeconds <= 0f)
            {
                SnapToShot(shot);
                return;
            }

            _tweenFrom = new CameraShot(camera.transform.position, camera.orthographicSize);
            _tweenTarget = shot;
            _tweenDuration = durationSeconds;
            _tweenElapsed = 0f;
            ActiveMode = CameraDirectorMode.Tween;
        }

        /// <summary>
        /// Deterministic clock seam — real-time LateUpdate delegates here; tests
        /// call Tick directly to fast-forward without real-time waits.
        /// </summary>
        public void Tick(float deltaSeconds)
        {
            var camera = EnsureCamera();
            var before = camera.transform.position;

            switch (ActiveMode)
            {
                case CameraDirectorMode.Follow:
                    TickFollow(camera, deltaSeconds);
                    break;
                case CameraDirectorMode.Tween:
                    TickTween(camera, deltaSeconds);
                    break;
            }

            LastCameraDelta = camera.transform.position - before;
            AfterCameraWrite?.Invoke(LastCameraDelta);
        }

        private void LateUpdate()
        {
            if (!AutoTick)
            {
                return;
            }

            Tick(Time.deltaTime);
        }

        private void TickFollow(Camera camera, float deltaSeconds)
        {
            if (_followTarget == null)
            {
                // Target destroyed under us (hub teardown) — restore the route shot.
                SnapToShot(_routeShot);
                return;
            }

            var desired = new Vector3(
                Mathf.Clamp(
                    _followTarget.position.x * _followSettings.HorizontalFactor,
                    -_followSettings.HorizontalClamp,
                    _followSettings.HorizontalClamp),
                _followSettings.PlaneY,
                _followSettings.PlaneZ);
            camera.transform.position = Vector3.Lerp(
                camera.transform.position,
                desired,
                _followSettings.LerpSpeed * deltaSeconds);
        }

        private void TickTween(Camera camera, float deltaSeconds)
        {
            _tweenElapsed += deltaSeconds;
            var t = Mathf.Clamp01(_tweenElapsed / _tweenDuration);
            var eased = EaseOutQuad(0f, 1f, t);

            camera.transform.position = Vector3.Lerp(_tweenFrom.Position, _tweenTarget.Position, eased);
            camera.orthographic = true;
            camera.orthographicSize = Mathf.Lerp(_tweenFrom.OrthographicSize, _tweenTarget.OrthographicSize, eased);

            if (t >= 1f)
            {
                SnapToShot(_tweenTarget);
            }
        }

        // Note: SnapToShot never rewrites _routeShot — only SetRouteShot does.
        private void SnapToShot(CameraShot shot)
        {
            // Adopt-only: restoration paths run during teardown (OnDisable on
            // scene close), where lazily creating a camera would leak a fresh
            // GameObject into the closing scene. ApplyShot guards null.
            if (_camera == null)
            {
                _camera = Camera.main;
            }

            _followTarget = null;
            ClearTween();
            _heldShot = shot;
            ActiveMode = CameraDirectorMode.FixedShot;
            ApplyShot(shot);
        }

        private void ApplyShot(CameraShot shot)
        {
            if (_camera == null)
            {
                return;
            }

            _camera.orthographic = true;
            _camera.orthographicSize = shot.OrthographicSize;
            _camera.transform.position = shot.Position;
        }

        private void ClearTween()
        {
            _tweenDuration = 0f;
            _tweenElapsed = 0f;
        }

        private static float EaseOutQuad(float from, float to, float t) => Mathf.Lerp(from, to, 1f - (1f - t) * (1f - t));
    }
}
