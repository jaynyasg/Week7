using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace CareerQuest
{
    [RequireComponent(typeof(AvatarRuntimeView))]
    public class PlayerAvatarController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private Vector2 minBounds = new(-5.25f, -2.45f);
        [SerializeField] private Vector2 maxBounds = new(5.25f, 0.55f);

        private IReadOnlyList<BuildingEntrance> _entrances = Array.Empty<BuildingEntrance>();
        private Action<ActivityRoute> _onDestination;
        private AvatarRuntimeView _avatarView;

        private void Awake()
        {
            _avatarView = GetComponent<AvatarRuntimeView>();
        }

        public void Configure(GameSession session, IReadOnlyList<BuildingEntrance> entrances, Action<ActivityRoute> onDestination)
        {
            _avatarView ??= GetComponent<AvatarRuntimeView>();
            _avatarView.ApplyAvatar(session?.SelectedAvatar ?? AvatarConfig.DefaultAvatar);
            _entrances = entrances ?? Array.Empty<BuildingEntrance>();
            _onDestination = onDestination;

            // Walk clamp from the single anchor truth (WorldAnchors); the
            // serialized defaults stay only as an editor-visible mirror.
            var bounds = WorldAnchors.ActiveWalkBounds;
            minBounds = bounds.min;
            maxBounds = bounds.max;
        }

        private void Update()
        {
            var move = ReadMove();
            if (move.sqrMagnitude > 0f)
            {
                Move(move * moveSpeed * Time.deltaTime);
                _avatarView.SetLocomotion(true, move.x);
            }
            else
            {
                _avatarView.SetLocomotion(false, transform.localScale.x);
            }

            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.E))
            {
                TryEnterNearest();
            }

            if (Input.GetMouseButtonDown(0))
            {
                var camera = CameraDirector.Ensure().Camera;
                if (camera != null)
                {
                    var world = camera.ScreenToWorldPoint(Input.mousePosition);
                    TryEnterAt(world);
                }
            }
        }

        public void Move(Vector2 delta)
        {
            var next = (Vector2)transform.position + delta;
            next.x = Mathf.Clamp(next.x, minBounds.x, maxBounds.x);
            next.y = Mathf.Clamp(next.y, minBounds.y, maxBounds.y);
            transform.position = new Vector3(next.x, next.y, 0f);
        }

        public bool TryEnterNearest()
        {
            var entrance = NearestEntrance(transform.position);
            if (entrance == null || !entrance.Contains(transform.position))
            {
                return false;
            }

            _onDestination?.Invoke(entrance.Route);
            return true;
        }

        public bool TryEnterAt(Vector2 worldPosition)
        {
            var entrance = _entrances.FirstOrDefault(candidate => candidate.Contains(worldPosition));
            if (entrance == null)
            {
                return false;
            }

            _onDestination?.Invoke(entrance.Route);
            return true;
        }

        private BuildingEntrance NearestEntrance(Vector2 worldPosition)
        {
            return _entrances
                .OrderBy(entrance => Vector2.Distance(entrance.transform.position, worldPosition))
                .FirstOrDefault();
        }

        private static Vector2 ReadMove()
        {
            var move = Vector2.zero;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            {
                move.x -= 1f;
            }

            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            {
                move.x += 1f;
            }

            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            {
                move.y -= 1f;
            }

            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            {
                move.y += 1f;
            }

            return move.sqrMagnitude > 1f ? move.normalized : move;
        }
    }
}
