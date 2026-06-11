using UnityEngine;

namespace CareerQuest
{
    public static class AudioCueCatalog
    {
        public static bool TryPlay(AudioSource source, string cueId)
        {
            if (source == null || string.IsNullOrWhiteSpace(cueId))
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
