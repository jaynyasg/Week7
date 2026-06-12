using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U8 audio curation: copies chosen Kenney CC0 clips into
    /// Assets/Resources/Audio/ named per the cue convention so
    /// AudioDirector/AudioCueCatalog resolve them with zero code change.
    /// Idempotent: re-running overwrites the same outputs. Headless entry
    /// point Curate() always EditorApplication.Exit(0/1)s.
    ///
    /// The mapping is validated against AudioCueIds.All both ways before any
    /// copy: a cue without a source line or a mapping for an unknown cue fails
    /// the run — the curated set and the code registry cannot drift.
    ///
    /// Loop note: the Kenney jingle packs contain no true seamless loops; the
    /// ambient_*/music_* cues use the longest, most resolution-stable jingle
    /// per instrument family (pizzicato = music, steel/sax = room flavor) as
    /// the most loop-tolerant available option. A by-ear replacement pass can
    /// swap individual source paths here without touching runtime code.
    /// </summary>
    public static class CareerQuestAudioCurator
    {
        private const string ResourcesAudioFolder = "Assets/Resources/Audio";
        private const string KenneyAudioRoot = "Assets/_CareerQuest/Art/Kenney/Audio";
        private const string KenneyPlatformerSounds = "Assets/_CareerQuest/Art/Kenney/PlatformerPack/Sounds";

        /// <summary>cue ID → Kenney source path (project-relative).</summary>
        private static readonly (string CueId, string SourcePath)[] Mappings =
        {
            // UI tier
            (AudioCueIds.UiPress, $"{KenneyAudioRoot}/UiAudio/click1.ogg"),

            // Drag framework (U6)
            (AudioCueIds.DragPickup, $"{KenneyAudioRoot}/RpgAudio/cloth2.ogg"),
            (AudioCueIds.DropAccept, $"{KenneyAudioRoot}/InterfaceSounds/confirmation_001.ogg"),
            (AudioCueIds.DropReject, $"{KenneyAudioRoot}/InterfaceSounds/back_002.ogg"), // gentle, never punishing

            // Ceremony / badges
            (AudioCueIds.BadgeStamp, $"{KenneyAudioRoot}/ImpactSounds/impactPlate_medium_000.ogg"),
            (AudioCueIds.CeremonyDesignBuildSuccess, $"{KenneyAudioRoot}/MusicJingles/Hit jingles/jingles_HIT00.ogg"),
            (AudioCueIds.CeremonyDesignBuildPractice, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI00.ogg"),
            (AudioCueIds.CeremonyHealthHeroSuccess, $"{KenneyAudioRoot}/MusicJingles/Hit jingles/jingles_HIT04.ogg"),
            (AudioCueIds.CeremonyHealthHeroPractice, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI04.ogg"),
            (AudioCueIds.CeremonyLogicCourtSuccess, $"{KenneyAudioRoot}/MusicJingles/Hit jingles/jingles_HIT08.ogg"),
            (AudioCueIds.CeremonyLogicCourtPractice, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI08.ogg"),

            // Reveal cinematic beats (U7)
            (AudioCueIds.RevealToken, $"{KenneyPlatformerSounds}/sfx_coin.ogg"),
            (AudioCueIds.RevealSweep, $"{KenneyAudioRoot}/InterfaceSounds/maximize_008.ogg"),
            (AudioCueIds.RevealUnlock, $"{KenneyPlatformerSounds}/sfx_magic.ogg"),

            // World / hub
            (AudioCueIds.DoorEnter, $"{KenneyAudioRoot}/RpgAudio/doorOpen_1.ogg"),
            (AudioCueIds.RoomWipe, $"{KenneyAudioRoot}/RpgAudio/bookFlip2.ogg"), // paper curtain = page flip
            (AudioCueIds.Footstep, $"{KenneyAudioRoot}/ImpactSounds/footstep_grass_001.ogg"),
            (AudioCueIds.EmotePop, $"{KenneyAudioRoot}/InterfaceSounds/bong_001.ogg"),
            (AudioCueIds.CityPiecePop, $"{KenneyAudioRoot}/InterfaceSounds/confirmation_002.ogg"), // P19 city piece arrival

            // Hub toys (U12 P18 — click-to-delight)
            (AudioCueIds.ToyFountain, $"{KenneyAudioRoot}/InterfaceSounds/drop_002.ogg"),          // water bloop splash
            (AudioCueIds.ToyBell, $"{KenneyAudioRoot}/ImpactSounds/impactBell_heavy_000.ogg"),     // real bell strike
            (AudioCueIds.ToyFlag, $"{KenneyAudioRoot}/RpgAudio/cloth1.ogg"),                       // cloth flutter

            // Ambient/music loops (P4) — longest jingle per family, see loop note
            (AudioCueIds.AmbientCampus, $"{KenneyAudioRoot}/MusicJingles/Steel jingles/jingles_STEEL07.ogg"),
            (AudioCueIds.AmbientDesignBuild, $"{KenneyAudioRoot}/MusicJingles/Sax jingles/jingles_SAX07.ogg"),
            (AudioCueIds.AmbientHealthHero, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI03.ogg"),
            (AudioCueIds.AmbientLogicCourt, $"{KenneyAudioRoot}/MusicJingles/Steel jingles/jingles_STEEL02.ogg"),
            (AudioCueIds.AmbientGallery, $"{KenneyAudioRoot}/MusicJingles/Sax jingles/jingles_SAX03.ogg"),
            (AudioCueIds.AmbientReveal, $"{KenneyAudioRoot}/MusicJingles/Steel jingles/jingles_STEEL01.ogg"),
            (AudioCueIds.AmbientOptional, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI01.ogg"),
            (AudioCueIds.MusicCampus, $"{KenneyAudioRoot}/MusicJingles/Pizzicato jingles/jingles_PIZZI07.ogg")
        };

        [MenuItem("Career Quest/Audio/Curate Audio Cues")]
        public static void CurateInteractive()
        {
            CurateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: curates audio cues, then exits 0/1.</summary>
        public static void Curate()
        {
            CurateCore(exitWhenDone: true);
        }

        private static void CurateCore(bool exitWhenDone)
        {
            try
            {
                var problems = new List<string>();
                ValidateRegistryParity(problems);

                foreach (var (cueId, sourcePath) in Mappings)
                {
                    if (!File.Exists(sourcePath))
                    {
                        problems.Add($"missing Kenney source for '{cueId}': {sourcePath}");
                    }
                }

                if (problems.Count > 0)
                {
                    Debug.LogError("CQ_AUDIO Curate failed:\n" + string.Join("\n", problems));
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                Directory.CreateDirectory(ResourcesAudioFolder);

                foreach (var (cueId, sourcePath) in Mappings)
                {
                    File.Copy(sourcePath, $"{ResourcesAudioFolder}/{cueId}.ogg", overwrite: true);
                }

                AssetDatabase.Refresh();
                Debug.Log($"CQ_AUDIO Curate: complete ({Mappings.Length} cue clips copied to {ResourcesAudioFolder}).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_AUDIO Curate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        /// <summary>Mapping ↔ AudioCueIds.All must match exactly (no drift).</summary>
        private static void ValidateRegistryParity(List<string> problems)
        {
            var mapped = new HashSet<string>(Mappings.Select(mapping => mapping.CueId));
            if (mapped.Count != Mappings.Length)
            {
                problems.Add("duplicate cue IDs in the curation mapping");
            }

            foreach (var cueId in AudioCueIds.All)
            {
                if (!mapped.Contains(cueId))
                {
                    problems.Add($"registry cue '{cueId}' has no curation mapping");
                }
            }

            var registry = new HashSet<string>(AudioCueIds.All);
            foreach (var cueId in mapped)
            {
                if (!registry.Contains(cueId))
                {
                    problems.Add($"mapping cue '{cueId}' is not in AudioCueIds.All");
                }
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
