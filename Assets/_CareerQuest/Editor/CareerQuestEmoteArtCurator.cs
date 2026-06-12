using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U12 emote curation: copies the chosen Kenney Emotes sprites (ONE style
    /// folder — Style1, the white-bubble-with-tail vector style — for visual
    /// coherence) into Assets/Resources/CareerQuest/Emote/ named per the
    /// EmoteBubble resource convention, so the P16 emote render path resolves
    /// them with zero code change. Import settings come from
    /// CareerQuestTexturePostprocessor (Resources/CareerQuest is a managed path).
    ///
    /// Wave note: the pack has no literal hand-wave; the friendly happy face is
    /// the curated greeting (emote IDs are fixed — never text).
    ///
    /// Idempotent: re-running overwrites the same outputs. Headless entry point
    /// Curate() always EditorApplication.Exit(0/1)s.
    /// </summary>
    public static class CareerQuestEmoteArtCurator
    {
        private const string EmoteResourcesFolder = "Assets/Resources/CareerQuest/Emote";
        private const string KenneyEmoteStyleRoot = "Assets/_CareerQuest/Art/Kenney/Emotes/Style1";

        /// <summary>resource name → Kenney Style1 source file.</summary>
        private static readonly (string ResourceName, string SourceFile)[] Mappings =
        {
            ("emote.heart", "emote_heart.png"),
            ("emote.star", "emote_star.png"),
            ("emote.wave", "emote_faceHappy.png")
        };

        [MenuItem("Career Quest/Emotes/Curate Emote Art")]
        public static void CurateInteractive()
        {
            CurateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: curates emote sprites, then exits 0/1.</summary>
        public static void Curate()
        {
            CurateCore(exitWhenDone: true);
        }

        private static void CurateCore(bool exitWhenDone)
        {
            try
            {
                var problems = new List<string>();
                foreach (var (resourceName, sourceFile) in Mappings)
                {
                    if (!File.Exists($"{KenneyEmoteStyleRoot}/{sourceFile}"))
                    {
                        problems.Add($"missing Kenney Style1 source for '{resourceName}': {KenneyEmoteStyleRoot}/{sourceFile}");
                    }
                }

                if (problems.Count > 0)
                {
                    Debug.LogError("CQ_EMOTE Curate failed:\n" + string.Join("\n", problems));
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                Directory.CreateDirectory(EmoteResourcesFolder);
                foreach (var (resourceName, sourceFile) in Mappings)
                {
                    File.Copy(
                        $"{KenneyEmoteStyleRoot}/{sourceFile}",
                        $"{EmoteResourcesFolder}/{resourceName}.png",
                        overwrite: true);
                }

                AssetDatabase.Refresh();
                Debug.Log($"CQ_EMOTE Curate: complete ({Mappings.Length} emote sprites copied to {EmoteResourcesFolder}).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_EMOTE Curate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        private static void ExitIfHeadless(bool exitWhenDone, int code)
        {
            if (exitWhenDone)
            {
                EditorApplication.Exit(code);
            }
        }
    }
}
