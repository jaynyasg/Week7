using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// U8 three-tier audio system. Lives on the app object (survives
    /// ClearWorld — world teardown never cuts a fanfare or ambience).
    ///
    /// Tiers:
    /// 1. UI — a single 2D PlayOneShot source (button presses; no pitch games).
    /// 2. Gameplay — <see cref="GameplayVoiceCount"/> pooled 2D sources with
    ///    per-play pitch variation (±<see cref="GameplayPitchVariation"/>) and a
    ///    per-cue-ID minimum-interval throttle (drag-spam guard). The ceremony
    ///    fanfare rides a dedicated stoppable source in this tier so Skip can
    ///    duck it (the pooled PlayOneShot voices cannot be stopped per-clip).
    /// 3. Ambient/music — two looping channels (room flavor + campus music)
    ///    with a ~1s equal-power-ish crossfade on room change (P4).
    ///
    /// Contracts:
    /// - Silent no-op on missing clips (AudioCueCatalog's founding contract):
    ///   every Play* returns false and changes nothing audible when the clip is
    ///   absent; loop channels still record their target cue so route logic and
    ///   tests observe state without real audio.
    /// - Deterministic-friendly: the throttle clock and all fades advance ONLY
    ///   through Tick(deltaSeconds) (house idiom — AutoTick forwards
    ///   Time.deltaTime; tests set AutoTick=false and drive Tick directly).
    /// - Volume tiers: <see cref="SfxVolume"/> (UI + gameplay + fanfare) and
    ///   <see cref="MusicVolume"/> (ambient/music) — persisted device prefs
    ///   (U13/P20) load in Awake; PauseMenuController owns the pref writes.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        /// <summary>
        /// U13 device-settings keys (P20). These are device preferences only —
        /// never child data (R23). PauseMenuController writes them; Awake
        /// applies them so persisted volumes load on boot.
        /// </summary>
        public const string SfxVolumePrefKey = "cq.settings.sfx_volume";
        public const string MusicVolumePrefKey = "cq.settings.music_volume";

        public const int GameplayVoiceCount = 6;            // plan: 4–8 pooled sources
        public const float GameplayPitchVariation = 0.08f;  // plan: ±5–10%
        public const float DefaultMinCueIntervalSeconds = 0.12f;
        public const float CrossfadeSeconds = 1f;           // P4 ~1s room crossfade
        public const float FanfareStopFadeSeconds = 0.25f;
        public const float AmbientChannelVolume = 0.45f;    // flavor sits under SFX
        public const float MusicChannelVolume = 0.55f;

        public static AudioDirector Instance { get; private set; }

        /// <summary>Finds or creates the singleton (house Ensure idiom).</summary>
        public static AudioDirector Ensure()
        {
            if (Instance != null)
            {
                return Instance;
            }

            var existing = FindFirstObjectByType<AudioDirector>();
            if (existing != null)
            {
                Instance = existing;
                return existing;
            }

            var host = new GameObject("AudioDirector", typeof(AudioDirector));
            return host.GetComponent<AudioDirector>();
        }

        /// <summary>App-object attach point — CareerQuestApp.Awake routes here.</summary>
        public static AudioDirector AttachTo(GameObject host)
        {
            if (Instance != null)
            {
                return Instance;
            }

            var director = host.GetComponent<AudioDirector>();
            if (director == null)
            {
                director = host.AddComponent<AudioDirector>();
            }

            Instance = director;
            return director;
        }

        private sealed class LoopChannel
        {
            public AudioSource A;
            public AudioSource B;
            public bool ActiveIsA = true;
            public string TargetCue;
            public float FadeT = 1f; // 1 = settled
            public float ChannelVolume;

            // Volume factor the outgoing source fades down FROM. Retargeting
            // mid-crossfade hands over a source that is only partly faded in;
            // restarting its fade-out from full would pop it loud first.
            public float OutgoingStartFactor = 1f;

            public AudioSource Active => ActiveIsA ? A : B;
            public AudioSource Outgoing => ActiveIsA ? B : A;
        }

        private readonly List<AudioSource> _gameplayVoices = new();
        private readonly Dictionary<string, float> _lastPlayClock = new();
        private readonly Dictionary<string, AudioClip> _clipCache = new();
        private readonly HashSet<string> _missingClips = new();

        private AudioSource _uiSource;
        private AudioSource _fanfareSource;
        private LoopChannel _ambient;
        private LoopChannel _music;
        private int _nextVoice;
        private float _clock;
        private float _sfxVolume = 1f;
        private float _musicVolume = 1f;
        private float _fanfareFadeRemaining;
        private float _fanfareFadeTotal;
        private Func<string, AudioClip> _clipLoader;
        private bool _sourcesReady;

        /// <summary>Real-time clock toggle. Tests set false and drive Tick directly.</summary>
        public bool AutoTick { get; set; } = true;

        /// <summary>Per-cue-ID minimum interval (the drag-spam guard); test-tunable.</summary>
        public float MinCueIntervalSeconds { get; set; } = DefaultMinCueIntervalSeconds;

        /// <summary>
        /// Test seam: replaces clip resolution (e.g. `_ => null` simulates every
        /// clip missing without touching files). Setting it clears the cache.
        /// </summary>
        public Func<string, AudioClip> ClipLoader
        {
            get => _clipLoader ?? DefaultClipLoader;
            set
            {
                _clipLoader = value;
                _clipCache.Clear();
                _missingClips.Clear();
            }
        }

        /// <summary>SFX tier volume (UI + gameplay + fanfare). 0..1.</summary>
        public float SfxVolume
        {
            get => _sfxVolume;
            set
            {
                _sfxVolume = Mathf.Clamp01(value);
                ApplySfxVolume();
            }
        }

        /// <summary>Music tier volume (ambient + music loops). 0..1.</summary>
        public float MusicVolume
        {
            get => _musicVolume;
            set
            {
                _musicVolume = Mathf.Clamp01(value);
                ApplyChannelVolumes(_ambient);
                ApplyChannelVolumes(_music);
            }
        }

        /// <summary>Director state seams (tests assert these, never real audio).</summary>
        public string CurrentAmbientCue => _ambient?.TargetCue;
        public string CurrentMusicCue => _music?.TargetCue;
        public bool IsCrossfading =>
            (_ambient != null && _ambient.FadeT < 1f) || (_music != null && _music.FadeT < 1f);
        public int TotalGameplayPlays { get; private set; }
        public bool IsFanfarePlaying => _fanfareSource != null && _fanfareSource.isPlaying;

        // ------------------------------------------------------------------
        // Tier 1: UI
        // ------------------------------------------------------------------

        /// <summary>Single 2D one-shot source; no pitch variation, no throttle.</summary>
        public bool PlayUi(string cueId)
        {
            var clip = LoadClip(cueId);
            if (clip == null)
            {
                return false; // silent no-op contract
            }

            EnsureSources();
            _uiSource.PlayOneShot(clip);
            return true;
        }

        // ------------------------------------------------------------------
        // Tier 2: gameplay (pooled, pitch variation, per-cue throttle)
        // ------------------------------------------------------------------

        public bool PlayCue(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return false;
            }

            if (_lastPlayClock.TryGetValue(cueId, out var last)
                && _clock - last < MinCueIntervalSeconds)
            {
                return false; // throttled (drag-spam guard)
            }

            var clip = LoadClip(cueId);
            if (clip == null)
            {
                return false; // silent no-op contract
            }

            EnsureSources();
            var voice = _gameplayVoices[_nextVoice];
            _nextVoice = (_nextVoice + 1) % _gameplayVoices.Count;
            voice.pitch = 1f + UnityEngine.Random.Range(-GameplayPitchVariation, GameplayPitchVariation);
            voice.PlayOneShot(clip);

            _lastPlayClock[cueId] = _clock;
            TotalGameplayPlays++;
            return true;
        }

        /// <summary>
        /// Ceremony fanfare on the dedicated stoppable source — Skip ducks it
        /// via <see cref="StopFanfare"/> (pooled one-shots cannot be stopped).
        /// </summary>
        public bool PlayFanfare(string cueId)
        {
            EnsureSources();
            _fanfareFadeRemaining = 0f;
            _fanfareSource.Stop();
            _fanfareSource.volume = _sfxVolume;
            _fanfareSource.pitch = 1f;

            var clip = LoadClip(cueId);
            if (clip == null)
            {
                return false; // silent no-op contract
            }

            _fanfareSource.clip = clip;
            _fanfareSource.Play();
            return true;
        }

        public void StopFanfare(float fadeSeconds = FanfareStopFadeSeconds)
        {
            if (_fanfareSource == null || !_fanfareSource.isPlaying)
            {
                return;
            }

            if (fadeSeconds <= 0f)
            {
                _fanfareSource.Stop();
                return;
            }

            _fanfareFadeTotal = fadeSeconds;
            _fanfareFadeRemaining = fadeSeconds;
        }

        // ------------------------------------------------------------------
        // Tier 3: ambient/music loops (P4 crossfade on room change)
        // ------------------------------------------------------------------

        /// <summary>
        /// Sets both loop channels' targets; each channel that actually changed
        /// crossfades over ~1s. Null/empty cue fades a channel to silence.
        /// Target cues are recorded even when clips are missing (state seam).
        /// </summary>
        public void SetAmbience(string ambientCueId, string musicCueId)
        {
            EnsureSources();
            SetChannelTarget(_ambient, NormalizeCue(ambientCueId));
            SetChannelTarget(_music, NormalizeCue(musicCueId));
        }

        // ------------------------------------------------------------------
        // Deterministic clock (house Tick idiom)
        // ------------------------------------------------------------------

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            _clock += deltaSeconds;
            TickChannel(_ambient, deltaSeconds);
            TickChannel(_music, deltaSeconds);
            TickFanfareFade(deltaSeconds);
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }

            // U13 (P20): persisted device volumes apply on boot. Setters route
            // through the properties so live sources (none yet at Awake; the
            // lazy EnsureSources path re-applies) always match the prefs.
            SfxVolume = PlayerPrefs.GetFloat(SfxVolumePrefKey, 1f);
            MusicVolume = PlayerPrefs.GetFloat(MusicVolumePrefKey, 1f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private static AudioClip DefaultClipLoader(string cueId)
        {
            return Resources.Load<AudioClip>($"Audio/{cueId}");
        }

        private AudioClip LoadClip(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return null;
            }

            if (_clipCache.TryGetValue(cueId, out var cached))
            {
                return cached;
            }

            if (_missingClips.Contains(cueId))
            {
                return null;
            }

            var clip = ClipLoader(cueId);
            if (clip == null)
            {
                _missingClips.Add(cueId); // negative cache — no per-frame Resources.Load storms
            }
            else
            {
                _clipCache[cueId] = clip;
            }

            return clip;
        }

        private static string NormalizeCue(string cueId)
        {
            return string.IsNullOrWhiteSpace(cueId) ? null : cueId;
        }

        private void SetChannelTarget(LoopChannel channel, string cueId)
        {
            if (channel == null || string.Equals(channel.TargetCue, cueId, StringComparison.Ordinal))
            {
                return; // same target — never restart/crossfade into itself
            }

            channel.TargetCue = cueId;

            // Mid-crossfade retarget: keep the LOUDER source audible as the new
            // outgoing (continuing its fade from its current level) and reuse the
            // quieter one for the new cue — no pop up, no loud click off.
            var activeFactor = channel.FadeT;                                  // current incoming level
            var outgoingFactor = (1f - channel.FadeT) * channel.OutgoingStartFactor; // current outgoing level
            if (activeFactor >= outgoingFactor)
            {
                channel.ActiveIsA = !channel.ActiveIsA; // old incoming becomes outgoing
                channel.OutgoingStartFactor = activeFactor;
            }
            else
            {
                // Old outgoing stays outgoing, continuing from its current level;
                // the half-faded-in source is reused for the new cue.
                channel.OutgoingStartFactor = outgoingFactor;
            }

            channel.FadeT = 0f;

            var incoming = channel.Active;
            incoming.Stop();
            incoming.clip = cueId != null ? LoadClip(cueId) : null;
            incoming.volume = 0f;
            if (incoming.clip != null)
            {
                incoming.Play(); // silent no-op contract: missing clip = fade to quiet
            }
        }

        private void TickChannel(LoopChannel channel, float deltaSeconds)
        {
            if (channel == null)
            {
                return;
            }

            if (channel.FadeT < 1f)
            {
                channel.FadeT = Mathf.Min(1f, channel.FadeT + deltaSeconds / CrossfadeSeconds);
                if (channel.FadeT >= 1f && channel.Outgoing != null)
                {
                    channel.Outgoing.Stop();
                    channel.Outgoing.clip = null;
                }
            }

            ApplyChannelVolumes(channel);
        }

        private void ApplyChannelVolumes(LoopChannel channel)
        {
            if (channel?.A == null || channel.B == null)
            {
                return;
            }

            var full = channel.ChannelVolume * _musicVolume;
            channel.Active.volume = full * channel.FadeT;
            channel.Outgoing.volume = full * channel.OutgoingStartFactor * (1f - channel.FadeT);
        }

        private void TickFanfareFade(float deltaSeconds)
        {
            if (_fanfareFadeRemaining <= 0f || _fanfareSource == null)
            {
                return;
            }

            _fanfareFadeRemaining -= deltaSeconds;
            if (_fanfareFadeRemaining <= 0f)
            {
                _fanfareFadeRemaining = 0f;
                _fanfareSource.Stop();
                _fanfareSource.volume = _sfxVolume;
                return;
            }

            _fanfareSource.volume = _sfxVolume * (_fanfareFadeRemaining / _fanfareFadeTotal);
        }

        private void ApplySfxVolume()
        {
            if (!_sourcesReady)
            {
                return;
            }

            _uiSource.volume = _sfxVolume;
            foreach (var voice in _gameplayVoices)
            {
                voice.volume = _sfxVolume;
            }

            if (_fanfareFadeRemaining <= 0f)
            {
                _fanfareSource.volume = _sfxVolume;
            }
        }

        private void EnsureSources()
        {
            if (_sourcesReady)
            {
                return;
            }

            _uiSource = CreateSource("UiVoice", loop: false);
            _fanfareSource = CreateSource("FanfareVoice", loop: false);

            _gameplayVoices.Clear();
            for (var i = 0; i < GameplayVoiceCount; i++)
            {
                _gameplayVoices.Add(CreateSource($"GameplayVoice{i}", loop: false));
            }

            _ambient = CreateChannel("Ambient", AmbientChannelVolume);
            _music = CreateChannel("Music", MusicChannelVolume);
            _sourcesReady = true;
            ApplySfxVolume();
        }

        private LoopChannel CreateChannel(string label, float channelVolume)
        {
            return new LoopChannel
            {
                A = CreateSource($"{label}LoopA", loop: true),
                B = CreateSource($"{label}LoopB", loop: true),
                ChannelVolume = channelVolume
            };
        }

        private AudioSource CreateSource(string label, bool loop)
        {
            var child = new GameObject(label, typeof(AudioSource));
            child.transform.SetParent(transform, false);
            var source = child.GetComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.spatialBlend = 0f; // 2D — the diorama camera never pans far
            source.volume = loop ? 0f : _sfxVolume;
            return source;
        }
    }
}
