using System.Collections;
using System.Linq;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U12 P18 hub-toy suite. Fails LOUDLY when the CampusHub prefab predates
    /// the toy pass — rebuild with CareerQuestHubPrefabBuilder.GenerateBuildingArt
    /// then Build (the toys live in the authored prefab, not in code mounts).
    ///
    /// Toys are pure local delight: assertions cover idempotent re-trigger
    /// (rapid clicks never stack broken transforms) and bounded cue plays
    /// through the AudioDirector seams — never real audio.
    /// </summary>
    public class HubToyPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            // Known suite pitfall: stale hub mounts from earlier fixtures make
            // GameObject.Find non-deterministic — start each test clean.
            foreach (var stale in Object.FindObjectsByType<WorldAnchors>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (stale.gameObject.name == "CampusHub")
                {
                    Object.DestroyImmediate(stale.gameObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator CampusHubMountsThreeToysOnTheRaycastClickPath()
        {
            var worldObject = NewWorld(out var world);
            world.ShowCampus(new GameSession());
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "CampusHub prefab should mount — build it with CareerQuestHubPrefabBuilder.");

            var toys = hub.GetComponentsInChildren<HubToy>();
            Assert.That(toys.Length, Is.EqualTo(3), "P18: fountain, bell, and flag toys live in the hub prefab.");
            Assert.That(toys.Select(toy => toy.Kind).Distinct().Count(), Is.EqualTo(3), "One toy of each kind.");

            foreach (var toy in toys)
            {
                // Same Physics2D raycast path as drag: Collider2D is required
                // for the Physics2DRaycaster to deliver IPointerClickHandler.
                Assert.That(toy.GetComponent<Collider2D>(), Is.Not.Null, $"{toy.Kind} toy needs a click collider.");
            }

            // The flag toy rides the SAME pennant as the ambient sway (P18:
            // flutter burst on top of the sway, not a second flag).
            var flagToy = toys.First(toy => toy.Kind == HubToyKind.Flag);
            Assert.That(flagToy.GetComponent<AmbientMotion>(), Is.Not.Null,
                "The flag toy composes with the pennant's AmbientMotion sway.");

            Object.DestroyImmediate(worldObject);
        }

        [UnityTest]
        public IEnumerator RapidClicksResetTheBeatAndRestoreTheRestPose()
        {
            var worldObject = NewWorld(out var world);
            world.ShowCampus(new GameSession());
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "Toy test requires the built CampusHub prefab.");
            var toys = hub.GetComponentsInChildren<HubToy>();

            foreach (var toy in toys)
            {
                toy.AutoTick = false;
                var baseScale = toy.transform.localScale;
                var baseRotation = toy.transform.localRotation;

                // Click spam mid-beat: every re-trigger resets against the SAME
                // rest pose — transforms never compound.
                for (var click = 0; click < 10; click++)
                {
                    toy.Activate();
                    toy.Tick(0.03f);
                }

                Assert.That(toy.IsPlaying, Is.True, $"{toy.Kind} should be mid-beat after clicks.");

                // Let the final beat run out completely.
                toy.Tick(HubToy.BeatSeconds + 0.1f);

                Assert.That(toy.IsPlaying, Is.False, $"{toy.Kind} beat must end.");
                Assert.That((toy.transform.localScale - baseScale).magnitude, Is.LessThan(0.001f),
                    $"{toy.Kind} must restore its rest scale after click spam.");
                Assert.That(Quaternion.Angle(toy.transform.localRotation, baseRotation), Is.LessThan(0.1f),
                    $"{toy.Kind} must restore its rest rotation after click spam.");
                toy.AutoTick = true;
            }

            Object.DestroyImmediate(worldObject);
        }

        [UnityTest]
        public IEnumerator ClickSpamPlaysABoundedNumberOfCuesThroughTheDirector()
        {
            var worldObject = NewWorld(out var world);
            world.ShowCampus(new GameSession());
            yield return null;

            var hub = GameObject.Find("CampusHub");
            Assert.That(hub, Is.Not.Null, "Toy test requires the built CampusHub prefab.");
            var bell = hub.GetComponentsInChildren<HubToy>().First(toy => toy.Kind == HubToyKind.Bell);

            var director = AudioDirector.Ensure();
            var clip = AudioClip.Create("toy-test-clip", 441, 1, 44100, false);
            director.AutoTick = false; // throttle clock frozen — spam window
            director.ClipLoader = _ => clip;

            try
            {
                // Open the throttle window first (instance-scoped: an earlier
                // fixture may have played this cue against the same director).
                director.Tick(AudioDirector.DefaultMinCueIntervalSeconds + 0.05f);
                var playsBefore = director.TotalGameplayPlays;
                bell.AutoTick = false;
                for (var click = 0; click < 8; click++)
                {
                    bell.Activate();
                }

                // Per-cue throttle: 8 clicks inside one throttle window = 1 play.
                Assert.That(director.TotalGameplayPlays - playsBefore, Is.EqualTo(1),
                    "Click spam must play a bounded number of cue instances.");

                // The window reopens with time — the next click is audible again.
                director.Tick(AudioDirector.DefaultMinCueIntervalSeconds + 0.05f);
                bell.Activate();
                Assert.That(director.TotalGameplayPlays - playsBefore, Is.EqualTo(2));
            }
            finally
            {
                bell.AutoTick = true;
                director.ClipLoader = null;
                director.AutoTick = true;
                Object.DestroyImmediate(clip);
            }

            Object.DestroyImmediate(worldObject);
        }

        private static GameObject NewWorld(out CampusWorldController world)
        {
            var worldObject = new GameObject("hub-toy-test");
            world = worldObject.AddComponent<CampusWorldController>();
            return worldObject;
        }
    }
}
