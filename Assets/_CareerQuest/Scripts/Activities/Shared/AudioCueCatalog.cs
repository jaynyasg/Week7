using System;
using System.Collections.Generic;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Cue entry point for gameplay call sites. U8: routes through the
    /// three-tier <see cref="AudioDirector"/> (gameplay tier: pooled voices,
    /// pitch variation, per-cue throttle) while preserving the founding
    /// contract — a missing clip is a silent no-op and TryPlay returns false;
    /// gameplay flows never depend on audio succeeding.
    ///
    /// U9 (R19) quiet-mode audio gating: when
    /// <see cref="ClassroomAccessSettings.QuietAudioActive"/> is set, this gate
    /// SOFTENS the soundscape without losing completion clarity:
    /// - looping ambience/music intensity is ducked to a quiet floor (via the
    ///   director's public music tier, snapshot/restored so the pause-menu
    ///   slider is honored when quiet mode lifts), and
    /// - non-essential one-shot FLAVOR cues (drag spam, toy bells, room wipe)
    ///   are suppressed, while ESSENTIAL completion cues (drop accept, gentle
    ///   reject, badge stamp, reveal unlock, door enter) still play so a
    ///   completing action always has an audible confirmation.
    /// The quiet gate is driven through <see cref="SetQuietMode"/> (CareerQuestApp
    /// pushes ClassroomAccessSettings.QuietMode here on every change).
    /// </summary>
    public static class AudioCueCatalog
    {
        /// <summary>The ducked music-tier floor in quiet mode (soft, not silent).</summary>
        public const float QuietMusicFloor = 0.12f;

        /// <summary>
        /// Completion-clarity allowlist: cues that ALWAYS play, even in quiet
        /// mode, because they confirm a player action landed (accept/reject) or
        /// a milestone completed (badge/reveal/door). Flavor/ambient-spam cues
        /// not in this set are suppressed while quiet.
        /// </summary>
        private static readonly HashSet<string> EssentialCues = new(StringComparer.Ordinal)
        {
            AudioCueIds.DropAccept,
            AudioCueIds.DropReject,
            AudioCueIds.BadgeStamp,
            AudioCueIds.RevealUnlock,
            AudioCueIds.DoorEnter
        };

        private static bool _quietMode;
        private static bool _hasSnapshot;
        private static float _musicVolumeSnapshot = 1f;

        /// <summary>True while quiet-classroom audio gating is active (test/QA seam).</summary>
        public static bool QuietMode => _quietMode;

        /// <summary>
        /// Whether a cue id is allowed to play under quiet mode (the completion-
        /// clarity allowlist). Pure + static so tests can assert the contract
        /// without audio. Non-essential cues return false only while quiet.
        /// </summary>
        public static bool IsAudibleUnderQuietMode(string cueId)
        {
            return !_quietMode || (cueId != null && EssentialCues.Contains(cueId));
        }

        /// <summary>
        /// U9 quiet toggle: ducks the looping music/ambience tier to a soft
        /// floor (snapshotting the live value to restore when quiet lifts) and
        /// flips the one-shot suppression gate. Uses only the director's public
        /// API — AudioDirector is owned elsewhere. Idempotent.
        /// </summary>
        public static void SetQuietMode(bool quiet)
        {
            if (_quietMode == quiet)
            {
                return;
            }

            _quietMode = quiet;
            var director = AudioDirector.Instance;

            if (quiet)
            {
                if (director != null)
                {
                    // Snapshot the player's chosen music level once, then duck
                    // the looping tier so ambience/music intensity softens.
                    _musicVolumeSnapshot = director.MusicVolume;
                    _hasSnapshot = true;
                    director.MusicVolume = Mathf.Min(director.MusicVolume, QuietMusicFloor);
                }
            }
            else if (_hasSnapshot && director != null)
            {
                director.MusicVolume = _musicVolumeSnapshot;
                _hasSnapshot = false;
            }
        }

        /// <summary>Test/teardown reset so a quiet-mode test never leaks into later suites.</summary>
        public static void ResetQuietMode()
        {
            _quietMode = false;
            _hasSnapshot = false;
            _musicVolumeSnapshot = 1f;
        }

        public static bool TryPlay(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return false;
            }

            if (!IsAudibleUnderQuietMode(cueId))
            {
                return false; // quiet mode suppresses flavor cues (completion cues pass)
            }

            return AudioDirector.Ensure().PlayCue(cueId);
        }

        /// <summary>
        /// Legacy signature (pre-U8 call sites passed their own AudioSource).
        /// The director now owns playback; the source only matters as a direct
        /// fallback when no director exists at all.
        /// </summary>
        public static bool TryPlay(AudioSource source, string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return false;
            }

            if (!IsAudibleUnderQuietMode(cueId))
            {
                return false;
            }

            var director = AudioDirector.Instance;
            if (director != null)
            {
                return director.PlayCue(cueId);
            }

            if (source == null)
            {
                return false;
            }

            var clip = Resources.Load<AudioClip>($"Audio/{cueId}");
            if (clip == null)
            {
                return false;
            }

            source.PlayOneShot(clip);
            return true;
        }
    }
}
