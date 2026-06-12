using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Cue entry point for gameplay call sites. U8: routes through the
    /// three-tier <see cref="AudioDirector"/> (gameplay tier: pooled voices,
    /// pitch variation, per-cue throttle) while preserving the founding
    /// contract — a missing clip is a silent no-op and TryPlay returns false;
    /// gameplay flows never depend on audio succeeding.
    /// </summary>
    public static class AudioCueCatalog
    {
        public static bool TryPlay(string cueId)
        {
            if (string.IsNullOrWhiteSpace(cueId))
            {
                return false;
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
