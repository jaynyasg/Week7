using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U8 audio import settings (mirrors the texture postprocessor approach so
    /// curated and source clips never need manual inspector edits):
    /// - Short SFX → DecompressOnLoad (zero-latency one-shots).
    /// - Loops (ambient_*/music_* cues, and everything in the MusicJingles
    ///   source folders) → Streaming.
    /// All sources are ogg/wav (never mp3 — leading-silence gap); Vorbis
    /// compression in the import container either way.
    /// </summary>
    public sealed class CareerQuestAudioPostprocessor : AssetPostprocessor
    {
        private const string ResourcesAudioPrefix = "Assets/Resources/Audio/";
        private const string KenneyAudioPrefix = "Assets/_CareerQuest/Art/Kenney/Audio/";
        private const string KenneyPlatformerSoundsPrefix = "Assets/_CareerQuest/Art/Kenney/PlatformerPack/Sounds/";
        private const string KenneyUiPackSoundsPrefix = "Assets/_CareerQuest/Art/Kenney/UiPack/Sounds/";

        private void OnPreprocessAudio()
        {
            var normalized = assetPath.Replace('\\', '/');
            if (!IsManagedPath(normalized))
            {
                return;
            }

            var importer = (AudioImporter)assetImporter;
            var settings = importer.defaultSampleSettings;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = 0.7f;
            settings.loadType = IsLoop(normalized)
                ? AudioClipLoadType.Streaming
                : AudioClipLoadType.DecompressOnLoad;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = false;
            importer.loadInBackground = false;
        }

        private static bool IsManagedPath(string path)
        {
            return path.StartsWith(ResourcesAudioPrefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(KenneyAudioPrefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(KenneyPlatformerSoundsPrefix, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(KenneyUiPackSoundsPrefix, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsLoop(string path)
        {
            if (path.StartsWith(ResourcesAudioPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var cueId = Path.GetFileNameWithoutExtension(path);
                return AudioCueIds.IsLoopCue(cueId);
            }

            // Source-side: jingle families are the loop/fanfare candidates.
            return path.IndexOf("/MusicJingles/", StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
