using System.Collections;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Mounts the authored CampusHub prefab (visual-only, no NetworkObject)
    /// behind the unchanged CampusWorldController.Show* API. Falls back to a
    /// minimal legacy-style ground when the prefab asset is missing, so the
    /// suite fails loudly only in the prefab-specific tests, never everywhere.
    /// Boot contract preserved: IsBootComplete flips immediately on build,
    /// IsDecorLoaded on the next frame (warmup settle: parallax re-anchor).
    /// </summary>
    internal sealed class HubBootController
    {
        private readonly MonoBehaviour _host;
        private readonly CampusWorldBuilder _builder;
        private Coroutine _decorRoutine;

        public HubBootController(MonoBehaviour host, CampusWorldBuilder builder)
        {
            _host = host;
            _builder = builder;
        }

        public bool IsBootComplete { get; private set; }

        public bool IsDecorLoaded { get; private set; }

        public void Cancel()
        {
            if (_decorRoutine != null)
            {
                _host.StopCoroutine(_decorRoutine);
                _decorRoutine = null;
            }

            IsBootComplete = false;
            IsDecorLoaded = false;
        }

        public void BuildCampus(string name)
        {
            Cancel();
            _builder.ClearWorld();

            if (!TryMountHubPrefab())
            {
                BuildFallbackGround();
            }

            IsBootComplete = true;
            IsDecorLoaded = false;
            _decorRoutine = _host.StartCoroutine(FinishDecor());
        }

        private bool TryMountHubPrefab()
        {
            var prefab = Resources.Load<GameObject>(WorldAnchors.ActivePrefabResourcePath);
            if (prefab == null)
            {
                Debug.LogWarning(
                    $"CampusHub prefab missing at Resources/{WorldAnchors.ActivePrefabResourcePath} — " +
                    "run 'Career Quest/World/Build Campus Hub Prefab' (CareerQuestHubPrefabBuilder.Build). " +
                    "Falling back to minimal ground.");
                return false;
            }

            var instance = Object.Instantiate(prefab, _builder.Root);
            instance.name = "CampusHub";
            return true;
        }

        private void BuildFallbackGround()
        {
            _builder.AddSky();
            _builder.AddGround();
            // Design-review (2026-06-16): the minimal fallback keeps a central
            // cross sized to the widened campus (the authored prefab carries the
            // full color-coded district road network). Horizontal reach extended
            // to the spread-out side clusters; vertical reach down to Care Corner.
            _builder.AddPath(new Vector2(0f, -0.55f), new Vector2(11.4f, 0.46f), 0f);
            _builder.AddPath(new Vector2(0f, -1f), new Vector2(0.5f, 3.6f), 0f);
        }

        private IEnumerator FinishDecor()
        {
            yield return null;

            // Warmup settle: bands re-anchor against the route shot so a room
            // round-trip never leaves accumulated parallax drift.
            foreach (var layer in _builder.Root.GetComponentsInChildren<ParallaxLayer>())
            {
                layer.ReAnchor();
            }

            IsDecorLoaded = true;
            _decorRoutine = null;
        }
    }
}
