using System.Collections;
using CareerQuest;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U7 cinematic reveal scenarios on the deterministic clock (AutoTick off,
    /// Tick driven directly — house idiom shared with CeremonyController and
    /// CameraDirector). Covers AE3/F3: full sequence within the 12s cap, locked
    /// branch, skip at every beat boundary, the CancelCeremony teardown path,
    /// the reveal-start sync latch, and the R22 invariants.
    ///
    /// 2P note: the host-only NetcodePlayModeHarness cannot run two clients, so
    /// the true two-client scenarios (A skips at 3.5s while B watches; B still
    /// in a room is unaffected) are manual-evidence rows per the U6 convention.
    /// The latch contract itself (start = max(sync received, stage mounted)) is
    /// proven here at the director seam, and the host announce moment is proven
    /// in CampusSessionStatePlayModeTests.
    /// </summary>
    public class RevealCinematicPlayModeTests
    {
        private GameObject _appObject;
        private CareerQuestApp _app;
        private CareerRevealController _reveal;
        private RevealCinematicDirector _director;
        private CampusWorldController _world;
        private CameraDirector _cameraDirector;

        [SetUp]
        public void SetUp()
        {
            // Earlier tests in the run can leave self-ticking worlds, canvases,
            // and cameras behind — clean before, not just after, so global
            // lookups never hit stale objects.
            DestroyLeftovers();
        }

        [TearDown]
        public void TearDown()
        {
            DestroyLeftovers();
        }

        [UnityTest]
        public IEnumerator ThreeBadgesFullSequenceCompletesWithinCapAndRestoresCameraOnExit()
        {
            yield return CreateAppOnRevealRoute(3);

            Assert.That(_app.Session.RevealReady, Is.True, "R22 gate count unchanged: 3 unique badges unlock.");
            Assert.That(_director.LatchOpened, Is.True);
            Assert.That(_director.CurrentBeat, Is.EqualTo(RevealCinematicBeat.CameraToStage));

            var elapsed = 0f;
            while (!_director.IsResolved && elapsed < RevealCinematicDirector.MaxSeconds)
            {
                TickBoth(0.25f);
                elapsed += 0.25f;
            }

            Assert.That(_director.IsResolved, Is.True, "Full beat sequence should resolve within the 12s cap.");
            AssertTokensInSlots(3);

            // World-first contract: result copy + exit actions mount only after
            // the sequence resolves.
            Assert.That(GameObject.Find("RevealResultCard"), Is.Not.Null, "Result copy mounts after resolve.");
            Assert.That(GameObject.Find("RevealCampusButton"), Is.Not.Null, "Exit actions mount after resolve.");

            // R22: reveal copy stays strength-based.
            var confidence = GameObject.Find("RevealConfidence");
            Assert.That(confidence, Is.Not.Null);
            Assert.That(
                confidence.GetComponent<TextMeshProUGUI>().text,
                Is.EqualTo(_app.Session.ConfidencePhrase()),
                "Strength-based confidence copy unchanged.");

            // Exit to campus restores the camera through CameraDirector.
            _app.ShowCampus();
            Assert.That(_cameraDirector.ActiveMode, Is.Not.EqualTo(CameraDirectorMode.Tween), "No stranded camera tween after exit.");
            Assert.That(_cameraDirector.CurrentShot.Approximately(CameraShot.Default), Is.True, "Campus route shot restored on exit.");
        }

        [UnityTest]
        public IEnumerator UnlockedRevealRendersSynthesisCopyAndKeepsAccessoriesInCeremonyContext()
        {
            // U7: the unlocked card is driven by RevealSynthesis (KTD9) — the
            // headline leads with the superpower, with a family subhead and the
            // top-5 paths. The stage hero avatar flips into ceremony context so
            // earned + ceremony-only accessories stay visible through the reveal.
            yield return CreateAppOnRevealRoute(3);

            Assert.That(_reveal.Synthesis, Is.Not.Null, "Render computes a synthesis snapshot.");
            Assert.That(_reveal.Synthesis.Style, Is.EqualTo(RevealStyle.Simple), "3 unique completions = Simple style.");

            // Ceremony-context accessory flip on the stage hero (U6 seam).
            var hero = FindHeroAvatar();
            Assert.That(hero, Is.Not.Null, "Reveal stage hero avatar exists.");
            Assert.That(hero.AccessoryLayer, Is.Not.Null, "Hero avatar binds an accessory layer for the ceremony.");
            Assert.That(hero.AccessoryLayer.IsCeremonyContext, Is.True, "Hero avatar is in ceremony context during reveal.");

            var elapsed = 0f;
            while (!_director.IsResolved && elapsed < RevealCinematicDirector.MaxSeconds)
            {
                TickBoth(0.25f);
                elapsed += 0.25f;
            }

            Assert.That(_director.IsResolved, Is.True);

            var lead = GameObject.Find("RevealLead");
            Assert.That(lead, Is.Not.Null, "Synthesis headline mounts.");
            Assert.That(
                lead.GetComponent<TextMeshProUGUI>().text,
                Is.EqualTo(_reveal.Synthesis.Superpower),
                "Headline leads with the synthesized superpower.");
            Assert.That(GameObject.Find("RevealSubhead"), Is.Not.Null, "Family subhead mounts.");
            Assert.That(GameObject.Find("RevealPaths"), Is.Not.Null, "Top-5 paths mount.");

            _app.ShowCampus();
            yield return null;
        }

        private AvatarRuntimeView FindHeroAvatar()
        {
            foreach (var view in Object.FindObjectsByType<AvatarRuntimeView>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (view.name == RevealStageLayout.HeroAvatarName)
                {
                    return view;
                }
            }

            return null;
        }

        [UnityTest]
        public IEnumerator TwoBadgesShowsLockedStageWithProgressAndNoSkip()
        {
            yield return CreateAppOnRevealRoute(2);

            Assert.That(_app.Session.RevealReady, Is.False, "R22 gate count unchanged: 2 unique badges stay locked.");
            Assert.That(_director.CurrentBeat, Is.EqualTo(RevealCinematicBeat.Settle), "Locked branch settles — no full cinematic.");
            Assert.That(_director.SpawnedTokenCount, Is.EqualTo(2), "Earned tokens sit on the slots immediately (2/3 state).");
            Assert.That(_director.CanSkip, Is.False, "Locked branch never arms skip.");
            Assert.That(_director.TrySkip(), Is.False);
            Assert.That(
                _cameraDirector.CurrentShot.Approximately(RevealStageLayout.SettleShot),
                Is.True,
                "No camera move beyond the settle shot.");

            TickBoth(0.25f, 1.0f);
            Assert.That(_director.IsResolved, Is.True, "Settle resolves quickly.");

            Assert.That(GameObject.Find(CareerRevealController.SkipButtonName), Is.Null, "No Skip control on the locked stage.");
            Assert.That(GameObject.Find("RevealLockedCard"), Is.Not.Null, "Locked card mounts after settle.");

            var progress = GameObject.Find("RevealBadgeProgress");
            Assert.That(progress, Is.Not.Null);
            Assert.That(progress.GetComponent<TextMeshProUGUI>().text, Does.Contain("2/3"), "Clear earned/3 state.");
        }

        [UnityTest]
        public IEnumerator SkipAtEveryBeatBoundaryLandsEndStateWithNoStrandedTweens()
        {
            // (seconds on the deterministic clock, expected live beat there)
            var boundaries = new (float at, RevealCinematicBeat beat)[]
            {
                (0.6f, RevealCinematicBeat.CameraToStage), // mid-camera-tween
                (3.2f, RevealCinematicBeat.TokenTravel),   // mid-token-travel
                (4.3f, RevealCinematicBeat.LightSweep)     // mid-light-sweep
            };

            foreach (var (at, beat) in boundaries)
            {
                yield return CreateAppOnRevealRoute(3);

                TickBoth(0.25f, at);
                Assert.That(_director.CurrentBeat, Is.EqualTo(beat), $"Expected beat at {at}s.");

                if (at < RevealCinematicDirector.SkipDelaySeconds)
                {
                    Assert.That(_director.TrySkip(), Is.False, "Skip arms only after 3s.");
                    // The fast-forward seam is the skip end-state contract at
                    // boundaries the 3s gate makes unreachable through TrySkip.
                    _director.FastForwardToEnd();
                }
                else
                {
                    Assert.That(_director.TrySkip(), Is.True, $"Skip should fire at {at}s.");
                }

                Assert.That(_director.IsResolved, Is.True, "Skip resolves immediately.");
                AssertTokensInSlots(3);
                Assert.That(
                    _cameraDirector.ActiveMode,
                    Is.EqualTo(CameraDirectorMode.FixedShot),
                    "Camera snapped to the final shot — no stranded tween.");
                Assert.That(GameObject.Find("RevealResultCard"), Is.Not.Null, "Result copy mounts when skip resolves.");

                yield return null; // skip control teardown is a deferred Destroy
                Assert.That(GameObject.Find(CareerRevealController.SkipButtonName), Is.Null, "Skip control unmounts after resolve.");

                _app.ShowCampus();
                Assert.That(_cameraDirector.CurrentShot.Approximately(CameraShot.Default), Is.True, "Camera restored on exit.");

                DestroyLeftovers(); // fresh world per boundary iteration
            }
        }

        [UnityTest]
        public IEnumerator CancelMidCinematicStopsBeatsRestoresCameraAndLeavesSafeRoute()
        {
            yield return CreateAppOnRevealRoute(3);

            TickBoth(0.25f, 2.0f);
            Assert.That(_director.IsRunning, Is.True);

            // The disconnect path (HandleClientConnectionLost → CancelCeremony)
            // routes through this single teardown.
            _reveal.CancelCinematic();

            Assert.That(_director.IsRunning, Is.False, "Beats stop.");
            Assert.That(_director.CurrentBeat, Is.EqualTo(RevealCinematicBeat.Idle));
            Assert.That(_director.SpawnedTokenCount, Is.EqualTo(0), "Token beats dropped — nothing half-traveled.");
            Assert.That(_cameraDirector.IsRestored, Is.True, "Camera restored to the route shot.");

            yield return null; // token layer Destroy is deferred one frame
            Assert.That(GameObject.Find(RevealStageLayout.TokenLayerName), Is.Null, "No stranded token layer.");

            // Safe route after teardown — no exceptions on the next navigation.
            _app.ShowCampus();
            Assert.That(_app.CurrentRoute, Is.EqualTo(ActivityRoute.Campus));
        }

        [UnityTest]
        public IEnumerator RevealStartLatchRequiresBothSyncAndLocalStageMount()
        {
            var directorObject = new GameObject("reveal-latch-test");
            var director = directorObject.AddComponent<RevealCinematicDirector>();
            director.AutoTick = false;

            var synced = false;
            var mounted = false;
            director.Begin(new RevealCinematicContext
            {
                Unlocked = true,
                EarnedCount = 3,
                RequireRevealStartSync = true,
                HasRevealStartSync = () => synced,
                IsStageMounted = () => mounted
            });

            director.Tick(0f);
            Assert.That(director.LatchOpened, Is.False, "Neither latch input is open.");

            synced = true;
            director.Tick(0f);
            Assert.That(director.LatchOpened, Is.False, "Never on the sync RPC alone — the local stage must mount.");

            synced = false;
            mounted = true;
            director.Tick(0f);
            Assert.That(director.LatchOpened, Is.False, "A connected client waits for the host's sync moment.");

            synced = true;
            director.Tick(0f);
            Assert.That(director.LatchOpened, Is.True, "Latch = max(sync received, local stage mounted).");
            Assert.That(director.CurrentBeat, Is.EqualTo(RevealCinematicBeat.CameraToStage));

            director.StopImmediate();
            Object.DestroyImmediate(directorObject);
            yield break;
        }

        [UnityTest]
        public IEnumerator LatchFallsBackToLocalStartWhenHostNeverAnnounces()
        {
            // Soft-lock regression: a client that opens the reveal while the host
            // stays on campus never receives the sync moment. The latch must open
            // after the fallback window so Skip/exit arm and the sequence plays —
            // the plan's "B entering later gets the normal local sequence".
            var directorObject = new GameObject("reveal-latch-fallback-test");
            var director = directorObject.AddComponent<RevealCinematicDirector>();
            director.AutoTick = false;

            director.Begin(new RevealCinematicContext
            {
                Unlocked = true,
                EarnedCount = 3,
                RequireRevealStartSync = true,
                HasRevealStartSync = () => false, // host never announces
                IsStageMounted = () => true
            });

            director.Tick(RevealCinematicDirector.LatchFallbackSeconds * 0.5f);
            Assert.That(director.LatchOpened, Is.False, "Inside the grace window the client still waits for a sync in flight.");

            director.Tick(RevealCinematicDirector.LatchFallbackSeconds * 0.5f);
            Assert.That(director.LatchOpened, Is.True, "Past the window the client starts its normal local sequence.");
            Assert.That(director.CurrentBeat, Is.EqualTo(RevealCinematicBeat.CameraToStage));

            // And the clock now runs — skip arms, the hard cap is reachable.
            director.Tick(RevealCinematicDirector.SkipDelaySeconds);
            Assert.That(director.CanSkip, Is.True, "Skip must arm once the fallback latch opened.");

            director.StopImmediate();
            Object.DestroyImmediate(directorObject);
            yield break;
        }

        // ------------------------------------------------------------------
        // Helpers
        // ------------------------------------------------------------------

        private IEnumerator CreateAppOnRevealRoute(int badges)
        {
            _appObject = new GameObject("reveal-cinematic-test");
            _app = _appObject.AddComponent<CareerQuestApp>();
            yield return null; // Start() renders the entry screen

            if (badges >= 3)
            {
                _app.Session.SeedShowcase();
            }
            else
            {
                if (badges >= 1)
                {
                    _app.Session.RecordResult(NetcodePlayModeHarness.SampleDegreeResult(CareerConfig.DesignBuildId, "Design Build"));
                }

                if (badges >= 2)
                {
                    _app.Session.RecordResult(NetcodePlayModeHarness.SampleDegreeResult(CareerConfig.HealthHeroId, "Health Hero"));
                }
            }

            _app.ShowReveal();

            _reveal = _appObject.GetComponent<CareerRevealController>();
            _director = _reveal.Director;
            _world = CampusWorldController.Ensure();
            _cameraDirector = _world.CameraDirector;
            Assert.That(_director, Is.Not.Null, "Render should attach the beat sequencer.");

            // Deterministic clock from here: tests drive Tick directly.
            _director.AutoTick = false;
            _cameraDirector.AutoTick = false;

            // The latch waits for the local stage mount (room veil clears a
            // frame or two after the route change).
            var safety = 0;
            while (_world.IsRoomVeilActive && safety++ < 120)
            {
                yield return null;
            }

            Assert.That(_world.IsRoomVeilActive, Is.False, "Room veil should clear so the stage-mount latch input opens.");
            _director.Tick(0f); // opens the latch without advancing the clock
        }

        private void TickBoth(float step, float seconds = -1f)
        {
            if (seconds < 0f)
            {
                _director.Tick(step);
                _cameraDirector.Tick(step);
                return;
            }

            var remaining = seconds;
            while (remaining > 0f)
            {
                var delta = Mathf.Min(step, remaining);
                _director.Tick(delta);
                _cameraDirector.Tick(delta);
                remaining -= delta;
            }
        }

        private void AssertTokensInSlots(int expected)
        {
            Assert.That(_director.SpawnedTokenCount, Is.EqualTo(expected));
            for (var i = 0; i < expected; i++)
            {
                var token = _director.TokenAt(i);
                Assert.That(token, Is.Not.Null, $"Token {i} should exist.");
                Assert.That(
                    Vector3.Distance(token.position, _director.SlotWorldPosition(i)),
                    Is.LessThan(0.001f),
                    $"Token {i} should end snapped to its slot.");
            }
        }

        private void DestroyLeftovers()
        {
            if (_appObject != null)
            {
                Object.DestroyImmediate(_appObject);
                _appObject = null;
            }

            foreach (var hub in Object.FindObjectsByType<PlayableHubController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(hub.gameObject);
            }

            foreach (var world in Object.FindObjectsByType<CampusWorldController>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Object.DestroyImmediate(world.gameObject);
            }

            // Director-created cameras must die with the director so scene
            // close never logs cleanup errors (suite pitfall).
            foreach (var director in Object.FindObjectsByType<CameraDirector>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var camera = director.Camera;
                Object.DestroyImmediate(director.gameObject);
                if (camera != null)
                {
                    Object.DestroyImmediate(camera.gameObject);
                }
            }

            DestroyAllNamed("CareerQuestCanvas");
            DestroyAllNamed(RevealStageLayout.TokenLayerName);
            DestroyAllNamed("AcceptPoof");
            DestroyAllNamed("CeremonyConfetti");

            _app = null;
            _reveal = null;
            _director = null;
            _world = null;
            _cameraDirector = null;
        }

        private static void DestroyAllNamed(string name)
        {
            var safety = 0;
            for (var found = GameObject.Find(name); found != null && safety++ < 32; found = GameObject.Find(name))
            {
                Object.DestroyImmediate(found);
            }
        }
    }
}
