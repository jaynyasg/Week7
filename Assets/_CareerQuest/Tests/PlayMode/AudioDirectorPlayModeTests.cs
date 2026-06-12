using System.Collections;
using System.Collections.Generic;
using CareerQuest;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace CareerQuest.Tests
{
    /// <summary>
    /// U8 AudioDirector suite: per-cue throttle (drag-spam guard), the silent
    /// no-op contract (simulated via the ClipLoader seam — files stay on disk),
    /// and the P4 ambient/music crossfade on route change. All assertions are
    /// director STATE (targets, counters, return values) — never real-time
    /// audio, which is unavailable in batchmode runs.
    /// </summary>
    public class AudioDirectorPlayModeTests
    {
        private readonly List<Object> _cleanup = new();
        private AudioClip _dummyClip;

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _cleanup)
            {
                if (created != null)
                {
                    Object.DestroyImmediate(created);
                }
            }

            _cleanup.Clear();

            if (_dummyClip != null)
            {
                Object.DestroyImmediate(_dummyClip);
                _dummyClip = null;
            }
        }

        private AudioDirector CreateDirector()
        {
            var host = new GameObject("audio-director-test");
            _cleanup.Add(host);
            var director = host.AddComponent<AudioDirector>();
            // Own instance only — AutoTick=false never leaks onto shared objects.
            director.AutoTick = false;
            return director;
        }

        private AudioClip DummyClip()
        {
            if (_dummyClip == null)
            {
                _dummyClip = AudioClip.Create("cq-test-clip", 441, 1, 44100, false);
            }

            return _dummyClip;
        }

        [Test]
        public void TenRapidPickupsWithinTheThrottleWindowPlayABoundedNumberOfInstances()
        {
            var director = CreateDirector();
            director.ClipLoader = _ => DummyClip();

            for (var i = 0; i < 10; i++)
            {
                director.PlayCue(AudioCueIds.DragPickup);
            }

            Assert.That(director.TotalGameplayPlays, Is.EqualTo(1),
                "Drag-spam guard: only the first pickup inside the window may play.");

            // A different cue is throttled independently.
            Assert.That(director.PlayCue(AudioCueIds.DropAccept), Is.True);
            Assert.That(director.TotalGameplayPlays, Is.EqualTo(2));

            // After the interval elapses (deterministic Tick clock) it plays again.
            director.Tick(AudioDirector.DefaultMinCueIntervalSeconds + 0.01f);
            Assert.That(director.PlayCue(AudioCueIds.DragPickup), Is.True);
            Assert.That(director.TotalGameplayPlays, Is.EqualTo(3));
        }

        [Test]
        public void AllTiersAreSilentNoOpsWhenClipsAreAbsent()
        {
            var director = CreateDirector();
            director.ClipLoader = _ => null; // simulate every clip missing

            Assert.DoesNotThrow(() =>
            {
                Assert.That(director.PlayUi(AudioCueIds.UiPress), Is.False);
                Assert.That(director.PlayCue(AudioCueIds.DropAccept), Is.False);
                Assert.That(director.PlayFanfare(AudioCueIds.CeremonyDesignBuildSuccess), Is.False);
                director.StopFanfare();
                director.SetAmbience(AudioCueIds.AmbientCampus, AudioCueIds.MusicCampus);
                director.Tick(2f);
            });

            Assert.That(director.TotalGameplayPlays, Is.Zero);

            // Route logic still observes the loop targets — flows complete
            // identically with audio entirely absent.
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientCampus));
            Assert.That(director.CurrentMusicCue, Is.EqualTo(AudioCueIds.MusicCampus));
        }

        [Test]
        public void AmbienceCrossfadeSettlesOnTheDeterministicClockAndIgnoresRepeatTargets()
        {
            var director = CreateDirector();
            director.ClipLoader = _ => DummyClip();

            director.SetAmbience(AudioCueIds.AmbientCampus, AudioCueIds.MusicCampus);
            Assert.That(director.IsCrossfading, Is.True, "A new target starts a crossfade.");

            director.Tick(AudioDirector.CrossfadeSeconds + 0.05f);
            Assert.That(director.IsCrossfading, Is.False, "~1s crossfade settles.");

            // Re-targeting the same cues must not restart the fade (P4: route
            // re-renders to the same room never stutter the ambience).
            director.SetAmbience(AudioCueIds.AmbientCampus, AudioCueIds.MusicCampus);
            Assert.That(director.IsCrossfading, Is.False);

            director.SetAmbience(AudioCueIds.AmbientDesignBuild, null);
            Assert.That(director.IsCrossfading, Is.True);
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientDesignBuild));
            Assert.That(director.CurrentMusicCue, Is.Null, "Rooms fade the campus music out.");
        }

        [UnityTest]
        public IEnumerator RouteChangesCrossfadeRoomFlavorAndHubRestoresCampusAmbienceAndMusic()
        {
            // The shared Ensure() instance is the one CampusWorldController
            // talks to; assertions are state-only and AutoTick stays untouched.
            var director = AudioDirector.Ensure();
            if (!_cleanup.Contains(director.gameObject) && director.gameObject.name == "AudioDirector")
            {
                _cleanup.Add(director.gameObject); // only reap a director this test created
            }

            var worldObject = new GameObject("audio-route-test");
            _cleanup.Add(worldObject);
            var world = worldObject.AddComponent<CampusWorldController>();
            var session = new GameSession();
            yield return null;

            world.ShowCampus(session);
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientCampus));
            Assert.That(director.CurrentMusicCue, Is.EqualTo(AudioCueIds.MusicCampus));

            world.ShowDesignBuild(session);
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientDesignBuild),
                "Room change must switch the target ambient cue (P4).");
            Assert.That(director.CurrentMusicCue, Is.Null);

            world.ShowClinic(session);
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientHealthHero),
                "Each core room carries its own flavor.");

            world.ShowCampus(session);
            Assert.That(director.CurrentAmbientCue, Is.EqualTo(AudioCueIds.AmbientCampus),
                "Returning to the hub restores campus ambience.");
            Assert.That(director.CurrentMusicCue, Is.EqualTo(AudioCueIds.MusicCampus),
                "Returning to the hub restores the music loop.");
            yield return null;
        }
    }
}
