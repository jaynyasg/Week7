using CareerQuest;
using UnityEngine;

namespace CareerQuest.Tests
{
    /// <summary>
    /// Shared test-isolation scrub (SceneWipe leak history): suites that count
    /// scene objects or session results start from a clean hierarchy. Mirrors
    /// the OptionalSurfacesArtPlayModeTests SetUp — app, world, hub, and canvas
    /// roots are removed; the camera rig is intentionally left alone
    /// (CameraDirector adopts-never-creates in teardown).
    /// </summary>
    internal static class PlayModeSceneScrubber
    {
        public static void DestroyStaleAppRoots()
        {
            DestroyAll<CareerQuestApp>();
            DestroyAll<CampusWorldController>();
            DestroyAll<PlayableHubController>();
            foreach (var canvas in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(canvas.gameObject);
            }
        }

        private static void DestroyAll<T>() where T : Component
        {
            foreach (var component in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(component.gameObject);
            }
        }
    }
}
