using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U5 character-art curation: maps the four player avatars and four NPCs to
    /// the Kenney Toon Characters set (CC0) and copies curated, catalog-ID-named
    /// frames into Resources so AssetCatalog resolves them with zero code change.
    ///
    /// Mapping (6 Kenney characters, 8 catalog IDs — the two reuses are
    /// pose-differentiated and placed in rooms that are NOT the matching
    /// avatar's thematic home room, so a player never stands beside an
    /// identical-looking NPC in that NPC's room):
    ///
    ///   avatar.sky_builder   → MalePerson        (idle default)
    ///   avatar.care_captain  → FemalePerson      (idle default)
    ///   avatar.logic_spark   → MaleAdventurer    (idle default — hat reads as detective)
    ///   avatar.art_inventor  → FemaleAdventurer  (idle default)
    ///   npc.campus_guide     → Robot             (unique — the guide is never confusable with a player)
    ///   npc.patient          → Zombie            (unique — the cartoon under-the-weather patient)
    ///   npc.builder_partner  → MaleAdventurer    ('hold' pose — Design Build room; logic_spark's home is Logic Court)
    ///   npc.judge            → FemalePerson      ('show' pose — Logic Court; care_captain's home is Health Hero)
    ///
    /// Frame-set convention (defined in AssetCatalog.FrameSetFor):
    ///   Resources/CareerQuest/{Category}/{id}.png             — default/idle pose (static)
    ///   Resources/CareerQuest/{Category}/{id}.{state}{n}.png  — frame n of state, contiguous from 0
    ///   states: walk (0..7), idle (0), celebrate (0..1, Kenney cheer poses)
    ///   avatars additionally keep {id}.walk.png (legacy single walk pose = walk4)
    ///
    /// Review copies land at Assets/_CareerQuest/Art/Avatars/ and Art/Npcs/ per
    /// house convention. Idempotent: re-running overwrites the same outputs.
    /// Headless entry point Curate() always EditorApplication.Exit(0/1)s.
    /// </summary>
    public static class CareerQuestCharacterArtCurator
    {
        private const string KenneyToonRoot = "Assets/_CareerQuest/Art/Kenney/ToonCharacters";
        private const string AvatarResourcesFolder = "Assets/Resources/CareerQuest/Avatar";
        private const string NpcResourcesFolder = "Assets/Resources/CareerQuest/Npc";
        private const string AvatarReviewFolder = "Assets/_CareerQuest/Art/Avatars";
        private const string NpcReviewFolder = "Assets/_CareerQuest/Art/Npcs";

        private const int WalkFrameCount = 8;

        private sealed class CharacterMap
        {
            public string CatalogId;
            public bool IsAvatar;
            public string KenneyFolder;
            public string KenneyName;
            public string DefaultPose;
            public bool CopyWalkFrames;

            public CharacterMap(string catalogId, bool isAvatar, string kenneyFolder, string kenneyName, string defaultPose, bool copyWalkFrames)
            {
                CatalogId = catalogId;
                IsAvatar = isAvatar;
                KenneyFolder = kenneyFolder;
                KenneyName = kenneyName;
                DefaultPose = defaultPose;
                CopyWalkFrames = copyWalkFrames;
            }
        }

        private static readonly CharacterMap[] Mappings =
        {
            new("avatar.sky_builder", true, "MalePerson", "malePerson", "idle", true),
            new("avatar.care_captain", true, "FemalePerson", "femalePerson", "idle", true),
            new("avatar.logic_spark", true, "MaleAdventurer", "maleAdventurer", "idle", true),
            new("avatar.art_inventor", true, "FemaleAdventurer", "femaleAdventurer", "idle", true),
            new("npc.campus_guide", false, "Robot", "robot", "idle", true),
            new("npc.patient", false, "Zombie", "zombie", "idle", false),
            new("npc.builder_partner", false, "MaleAdventurer", "maleAdventurer", "hold", false),
            new("npc.judge", false, "FemalePerson", "femalePerson", "show", false)
        };

        [MenuItem("Career Quest/Avatars/Curate Character Art")]
        public static void CurateInteractive()
        {
            CurateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: curates character art, then exits 0/1.</summary>
        public static void Curate()
        {
            CurateCore(exitWhenDone: true);
        }

        private static void CurateCore(bool exitWhenDone)
        {
            try
            {
                var missing = new List<string>();
                var plan = BuildCopyPlan(missing);

                if (missing.Count > 0)
                {
                    Debug.LogError(
                        "CQ_CHAR_ART Curate failed — missing Kenney source files:\n" +
                        string.Join("\n", missing));
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                Directory.CreateDirectory(AvatarResourcesFolder);
                Directory.CreateDirectory(NpcResourcesFolder);
                Directory.CreateDirectory(AvatarReviewFolder);
                Directory.CreateDirectory(NpcReviewFolder);

                foreach (var (source, destinations) in plan)
                {
                    foreach (var destination in destinations)
                    {
                        File.Copy(source, destination, overwrite: true);
                    }
                }

                AssetDatabase.Refresh();
                Debug.Log($"CQ_CHAR_ART Curate: complete ({Mappings.Length} characters, {plan.Count} source poses copied).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_CHAR_ART Curate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        private static List<(string Source, List<string> Destinations)> BuildCopyPlan(List<string> missing)
        {
            var plan = new List<(string, List<string>)>();

            foreach (var map in Mappings)
            {
                var resourcesFolder = map.IsAvatar ? AvatarResourcesFolder : NpcResourcesFolder;
                var reviewFolder = map.IsAvatar ? AvatarReviewFolder : NpcReviewFolder;

                void Add(string pose, string outputName)
                {
                    var source = $"{KenneyToonRoot}/{map.KenneyFolder}/character_{map.KenneyName}_{pose}.png";
                    if (!File.Exists(source))
                    {
                        missing.Add(source);
                        return;
                    }

                    plan.Add((source, new List<string>
                    {
                        $"{resourcesFolder}/{outputName}.png",
                        $"{reviewFolder}/{outputName}.png"
                    }));
                }

                // Default/static pose at the bare catalog ID (replaces generated art).
                Add(map.DefaultPose, map.CatalogId);

                // Idle frame set (single frame; the animator bobs gently on top).
                Add("idle", $"{map.CatalogId}.idle0");

                // Celebrate frame set from the Kenney cheer poses (P15).
                Add("cheer0", $"{map.CatalogId}.celebrate0");
                Add("cheer1", $"{map.CatalogId}.celebrate1");

                if (map.CopyWalkFrames)
                {
                    for (var frame = 0; frame < WalkFrameCount; frame++)
                    {
                        Add($"walk{frame}", $"{map.CatalogId}.walk{frame}");
                    }
                }

                if (map.IsAvatar)
                {
                    // Legacy single walk pose id ({id}.walk) stays final-art for
                    // the catalog's required-asset gate and animator fallback.
                    Add("walk4", $"{map.CatalogId}.walk");
                }
            }

            return plan;
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
