using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U6 authored activity-room pipeline, extending the CareerQuestHubPrefabBuilder
    /// convention: idempotent compose-and-save with headless entry points that
    /// EditorApplication.Exit(0/1).
    ///
    /// BuildDesignBuildStudio composes the visual-only DesignBuildStudio prefab
    /// (no NetworkObject): blueprint table with five lot pads + SlotAnchor_{pieceId}
    /// children, a piece tray with TrayAnchor_{i} children, blueprint prop, and
    /// craft-supply dressing. Positions come from the runtime
    /// <see cref="DesignBuildStudioLayout"/> single coordinate truth — the
    /// controller's fallback constants and the baked anchors can never diverge.
    /// The builder NPC is NOT baked: CampusRoomScenes creates it in code for both
    /// prefab and fallback paths so the P14 cheer hook is uniform.
    ///
    /// Canonical asset: Assets/_CareerQuest/Prefabs/Rooms/DesignBuildStudio.prefab
    /// Runtime copy:    Assets/Resources/CareerQuest/World/DesignBuildStudio.prefab
    /// (Resources.Load is the only runtime mount path for the AddComponent-built
    /// world controller.)
    /// </summary>
    public static class CareerQuestRoomPrefabBuilder
    {
        private const string RoomArtFolder = "Assets/_CareerQuest/Art/Rooms/DesignBuild";
        private const string PrefabFolder = "Assets/_CareerQuest/Prefabs/Rooms";
        private const string PrefabAssetPath = "Assets/_CareerQuest/Prefabs/Rooms/DesignBuildStudio.prefab";
        private const string PrefabResourcesFolder = "Assets/Resources/CareerQuest/World";
        private const string PrefabResourcesPath = "Assets/Resources/CareerQuest/World/DesignBuildStudio.prefab";
        private const string RoomBackdropPath = "Assets/Resources/CareerQuest/Room/room.design_build.png";
        private const string BlueprintPropPath = "Assets/Resources/CareerQuest/Prop/prop.blueprint.png";

        // DESIGN.md palette (Design Build identity: Creative Coral).
        private static readonly Color Coral = new(0.969f, 0.424f, 0.369f);
        private static readonly Color Mint = new(0.345f, 0.784f, 0.580f);
        private static readonly Color Amber = new(0.949f, 0.639f, 0.231f);
        private static readonly Color ScienceBlue = new(0.290f, 0.616f, 0.922f);
        private static readonly Color MusicLilac = new(0.620f, 0.522f, 0.863f);
        private static readonly Color PlazaCream = new(1f, 0.92f, 0.64f);
        private static readonly Color PaperWarm = new(1f, 0.969f, 0.878f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.14f);

        private static readonly (string Id, Color Accent)[] Pieces =
        {
            ("clinic", Mint),
            ("court", Amber),
            ("studio", Coral),
            ("lab", ScienceBlue),
            ("art_tower", MusicLilac)
        };

        [MenuItem("Career Quest/World/Build Design Build Studio Prefab")]
        public static void BuildDesignBuildStudioInteractive()
        {
            BuildDesignBuildStudioCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the room prefab, then exits 0/1.</summary>
        public static void BuildDesignBuildStudio()
        {
            BuildDesignBuildStudioCore(exitWhenDone: true);
        }

        private static void BuildDesignBuildStudioCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                GenerateRoomHelperArt();
                root = ComposeDesignBuildStudio();

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);

                EnsureFolder(PrefabResourcesFolder);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabResourcesPath) != null)
                {
                    AssetDatabase.DeleteAsset(PrefabResourcesPath);
                }

                if (!AssetDatabase.CopyAsset(PrefabAssetPath, PrefabResourcesPath))
                {
                    throw new InvalidOperationException($"Failed to copy '{PrefabAssetPath}' to '{PrefabResourcesPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ROOM_PREFAB BuildDesignBuildStudio: saved '{PrefabAssetPath}' (+ runtime copy '{PrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ROOM_PREFAB BuildDesignBuildStudio failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        // ------------------------------------------------------------------
        // U10: Health Hero Clinic prefab (drag-room replication)
        // ------------------------------------------------------------------

        private const string ClinicArtFolder = "Assets/_CareerQuest/Art/Rooms/HealthHero";
        private const string ClinicPrefabAssetPath = "Assets/_CareerQuest/Prefabs/Rooms/HealthHeroClinic.prefab";
        private const string ClinicPrefabResourcesPath = "Assets/Resources/CareerQuest/World/HealthHeroClinic.prefab";
        private const string ClinicBackdropPath = "Assets/Resources/CareerQuest/Room/room.health_hero.png";

        // DESIGN.md activity identity colors.
        private static readonly Color HealthMint = new(0.345f, 0.784f, 0.580f);   // #58C894
        private static readonly Color LogicAmber = new(0.949f, 0.639f, 0.231f);   // #F2A33B
        private static readonly Color LilacSoft = new(0.62f, 0.52f, 0.86f);
        private static readonly Color InkSoft = new(0.098f, 0.196f, 0.235f);

        [MenuItem("Career Quest/World/Build Health Hero Clinic Prefab")]
        public static void BuildHealthHeroClinicInteractive()
        {
            BuildHealthHeroClinicCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the clinic prefab, then exits 0/1.</summary>
        public static void BuildHealthHeroClinic()
        {
            BuildHealthHeroClinicCore(exitWhenDone: true);
        }

        private static void BuildHealthHeroClinicCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                GeneratePiecePropArt();
                GenerateRoomHelperArt(); // the clinic reuses the studio's room_table.png
                GenerateClinicHelperArt();
                root = ComposeHealthHeroClinic();

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, ClinicPrefabAssetPath);

                EnsureFolder(PrefabResourcesFolder);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(ClinicPrefabResourcesPath) != null)
                {
                    AssetDatabase.DeleteAsset(ClinicPrefabResourcesPath);
                }

                if (!AssetDatabase.CopyAsset(ClinicPrefabAssetPath, ClinicPrefabResourcesPath))
                {
                    throw new InvalidOperationException($"Failed to copy '{ClinicPrefabAssetPath}' to '{ClinicPrefabResourcesPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ROOM_PREFAB BuildHealthHeroClinic: saved '{ClinicPrefabAssetPath}' (+ runtime copy '{ClinicPrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ROOM_PREFAB BuildHealthHeroClinic failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        /// <summary>
        /// Clinic helper art (idempotent — rebuild overwrites): warm exam bed,
        /// patient-zone pad, tool tray board, care plan board, symptom clipboard
        /// wall board, wall cross — the DESIGN.md clinic diorama set, in the
        /// Kenney palette with Health Mint identity.
        /// </summary>
        private static void GenerateClinicHelperArt()
        {
            Directory.CreateDirectory(ClinicArtFolder);

            // Warm exam bed with a lighter mattress top and a pillow block.
            var bed = DrawWithPixels(300, 130, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, 12, w / 2 - 8, 11, SoftShadow);
                FillRoundedRect(pixels, w, h, 6, 12, w - 12, h - 38, 14, Color.Lerp(HealthMint, Color.white, 0.62f));
                FillRoundedRect(pixels, w, h, 14, 40, w - 28, h - 70, 12, Color.Lerp(HealthMint, Color.white, 0.8f));
                FillRoundedRect(pixels, w, h, 18, h - 44, 70, 30, 10, PaperWarm);
                FillRect(pixels, w, h, 6, 12, w - 12, 8, Color.Lerp(HealthMint, Color.black, 0.12f));
            });
            WritePng(bed, $"{ClinicArtFolder}/clinic_bed.png");
            UnityEngine.Object.DestroyImmediate(bed);

            // Patient-zone pad: soft mint plate with a lighter inset (drop target).
            var pad = DrawWithPixels(240, 110, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, h / 2, w / 2 - 4, h / 2 - 4, Color.Lerp(HealthMint, Color.white, 0.5f));
                FillEllipse(pixels, w, h, w / 2, h / 2, w / 2 - 18, h / 2 - 14, Color.Lerp(HealthMint, Color.white, 0.72f));
            });
            WritePng(pad, $"{ClinicArtFolder}/clinic_zone_pad.png");
            UnityEngine.Object.DestroyImmediate(pad);

            // Tool tray: paper board with a soft inner well (mirrors the studio tray).
            var tray = DrawWithPixels(640, 104, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 16, PaperWarm);
                FillRoundedRect(pixels, w, h, 8, 8, w - 16, h - 16, 12, Color.Lerp(PaperWarm, Color.black, 0.06f));
            });
            WritePng(tray, $"{ClinicArtFolder}/clinic_tray.png");
            UnityEngine.Object.DestroyImmediate(tray);

            // Care plan board: paper card with a mint header band and plan lines.
            var board = DrawWithPixels(150, 130, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 4, 4, w - 8, h - 8, 10, PaperWarm);
                FillRect(pixels, w, h, 10, h - 30, w - 20, 18, Color.Lerp(HealthMint, Color.white, 0.25f));
                FillRect(pixels, w, h, 14, h - 48, w - 28, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 14, h - 62, w - 36, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 14, h - 76, w - 44, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
            });
            WritePng(board, $"{ClinicArtFolder}/clinic_care_board.png");
            UnityEngine.Object.DestroyImmediate(board);

            // Symptom clipboard wall board: clipboard silhouette with case notes.
            var wallBoard = DrawWithPixels(110, 140, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 6, 4, w - 12, h - 16, 10, Color.Lerp(LogicAmber, Color.white, 0.45f));
                FillRoundedRect(pixels, w, h, 14, 12, w - 28, h - 36, 8, PaperWarm);
                FillRoundedRect(pixels, w, h, w / 2 - 16, h - 18, 32, 12, 5, Color.Lerp(InkSoft, Color.white, 0.4f));
                FillRect(pixels, w, h, 22, h - 54, w - 44, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 22, h - 68, w - 52, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 22, h - 82, w - 48, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
            });
            WritePng(wallBoard, $"{ClinicArtFolder}/clinic_symptom_board.png");
            UnityEngine.Object.DestroyImmediate(wallBoard);

            // Wall cross badge: mint circle with a white cross.
            var cross = DrawWithPixels(96, 96, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, h / 2, w / 2 - 4, h / 2 - 4, HealthMint);
                FillRect(pixels, w, h, w / 2 - 7, 22, 14, h - 44, Color.white);
                FillRect(pixels, w, h, 22, h / 2 - 7, w - 44, 14, Color.white);
            });
            WritePng(cross, $"{ClinicArtFolder}/clinic_wall_cross.png");
            UnityEngine.Object.DestroyImmediate(cross);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Structure and names mirror CampusRoomScenes.BuildFallbackClinic and the
        /// HealthHeroClinicLayout single coordinate truth. World band 200-299;
        /// controller-spawned pieces sit at 330. The patient NPC is NOT baked:
        /// CampusRoomScenes creates it in code for both paths (P14 hook).
        /// </summary>
        private static GameObject ComposeHealthHeroClinic()
        {
            var root = new GameObject("HealthHeroClinic");

            AddSprite(root.transform, "Backdrop", LoadSprite(ClinicBackdropPath), new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 200);

            // Patient zone pad under the bed area — the drop target reads as a place.
            var zone = HealthHeroClinicLayout.PatientZonePosition;
            AddSprite(root.transform, "PatientZonePad", ClinicSprite("clinic_zone_pad.png"), new Vector2(zone.x, zone.y - 0.25f), new Vector2(2.3f, 1.05f), 206);
            AddAnchor(root.transform, HealthHeroClinicLayout.ZoneAnchorPrefix + HealthHeroClinicLayout.PatientZoneId, zone);

            AddSprite(root.transform, "ClinicBed", ClinicSprite("clinic_bed.png"), new Vector2(-1.82f, -0.6f), new Vector2(2.35f, 1.0f), 210);
            AddSprite(root.transform, "CareCounter", RoomSprite("room_table.png"), new Vector2(1.88f, -0.62f), new Vector2(2.72f, 0.62f), 210);
            AddSprite(root.transform, "SymptomBoard", ClinicSprite("clinic_symptom_board.png"), new Vector2(-3.2f, 0.7f), new Vector2(0.85f, 1.1f), 212);
            AddSprite(root.transform, "CarePlanBoard", ClinicSprite("clinic_care_board.png"), new Vector2(2.6f, 0.55f), new Vector2(1.15f, 1.0f), 212);
            AddSprite(root.transform, "WallCross", ClinicSprite("clinic_wall_cross.png"), new Vector2(0.2f, 1.05f), new Vector2(0.55f, 0.55f), 212);

            AddSprite(root.transform, "ToolTrayBoard", ClinicSprite("clinic_tray.png"), new Vector2(0f, HealthHeroClinicLayout.TrayPosition(0).y - 0.02f), new Vector2(6.2f, 1.0f), 208);
            for (var i = 0; i < HealthHeroClinicLayout.PieceIds.Length; i++)
            {
                AddAnchor(root.transform, HealthHeroClinicLayout.TrayAnchorPrefix + i, HealthHeroClinicLayout.TrayPosition(i));
            }

            foreach (var stepPieceId in HealthHeroClinicLayout.StepPieceIds)
            {
                AddAnchor(root.transform, HealthHeroClinicLayout.AppliedAnchorPrefix + stepPieceId, HealthHeroClinicLayout.AppliedPosition(stepPieceId));
            }

            return root;
        }

        private static Sprite ClinicSprite(string fileName)
        {
            return LoadSprite($"{ClinicArtFolder}/{fileName}");
        }

        // ------------------------------------------------------------------
        // U10: Logic Court prefab (drag-room replication)
        // ------------------------------------------------------------------

        private const string CourtArtFolder = "Assets/_CareerQuest/Art/Rooms/LogicCourt";
        private const string CourtPrefabAssetPath = "Assets/_CareerQuest/Prefabs/Rooms/LogicCourt.prefab";
        private const string CourtPrefabResourcesPath = "Assets/Resources/CareerQuest/World/LogicCourt.prefab";
        private const string CourtBackdropPath = "Assets/Resources/CareerQuest/Room/room.logic_court.png";
        private const string ArgumentMeterPropPath = "Assets/Resources/CareerQuest/Prop/prop.argument_meter.png";

        [MenuItem("Career Quest/World/Build Logic Court Prefab")]
        public static void BuildLogicCourtInteractive()
        {
            BuildLogicCourtCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the court prefab, then exits 0/1.</summary>
        public static void BuildLogicCourt()
        {
            BuildLogicCourtCore(exitWhenDone: true);
        }

        private static void BuildLogicCourtCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                GeneratePiecePropArt();
                GenerateCourtHelperArt();
                root = ComposeLogicCourt();

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, CourtPrefabAssetPath);

                EnsureFolder(PrefabResourcesFolder);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(CourtPrefabResourcesPath) != null)
                {
                    AssetDatabase.DeleteAsset(CourtPrefabResourcesPath);
                }

                if (!AssetDatabase.CopyAsset(CourtPrefabAssetPath, CourtPrefabResourcesPath))
                {
                    throw new InvalidOperationException($"Failed to copy '{CourtPrefabAssetPath}' to '{CourtPrefabResourcesPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ROOM_PREFAB BuildLogicCourt: saved '{CourtPrefabAssetPath}' (+ runtime copy '{CourtPrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ROOM_PREFAB BuildLogicCourt failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        /// <summary>
        /// Court helper art (idempotent — rebuild overwrites): judge bench,
        /// podium, sorting zone pads (helpful check / not-helpful cross),
        /// evidence tray, conclusion stamp — the DESIGN.md court diorama set,
        /// in the Kenney palette with Logic Amber identity.
        /// </summary>
        private static void GenerateCourtHelperArt()
        {
            Directory.CreateDirectory(CourtArtFolder);

            // Judge bench: amber desk with a darker front panel and a top lip.
            var bench = DrawWithPixels(300, 120, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, 10, w / 2 - 8, 9, SoftShadow);
                FillRoundedRect(pixels, w, h, 4, 10, w - 8, h - 26, 12, Color.Lerp(LogicAmber, Color.black, 0.18f));
                FillRoundedRect(pixels, w, h, 10, h - 36, w - 20, 22, 8, Color.Lerp(LogicAmber, Color.white, 0.2f));
                FillRect(pixels, w, h, 16, 28, w - 32, 8, Color.Lerp(LogicAmber, Color.black, 0.32f));
            });
            WritePng(bench, $"{CourtArtFolder}/court_bench.png");
            UnityEngine.Object.DestroyImmediate(bench);

            // Podium: narrow amber stand with a paper rest.
            var podium = DrawWithPixels(120, 130, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, 10, w / 2 - 10, 8, SoftShadow);
                FillRoundedRect(pixels, w, h, w / 2 - 14, 10, 28, h - 50, 8, Color.Lerp(LogicAmber, Color.black, 0.22f));
                FillRoundedRect(pixels, w, h, 8, h - 44, w - 16, 30, 8, Color.Lerp(LogicAmber, Color.white, 0.22f));
                FillRoundedRect(pixels, w, h, 16, h - 38, w - 32, 18, 6, PaperWarm);
            });
            WritePng(podium, $"{CourtArtFolder}/court_podium.png");
            UnityEngine.Object.DestroyImmediate(podium);

            // Helpful zone pad: mint plate with a white check mark.
            var helpful = DrawWithPixels(220, 130, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 4, 4, w - 8, h - 8, 14, Color.Lerp(Mint, Color.white, 0.45f));
                FillRoundedRect(pixels, w, h, 14, 14, w - 28, h - 28, 12, Color.Lerp(Mint, Color.white, 0.68f));
                FillRect(pixels, w, h, w / 2 - 26, h / 2 - 6, 18, 10, Color.Lerp(Mint, Color.black, 0.25f));
                FillRect(pixels, w, h, w / 2 - 12, h / 2 - 18, 12, 30, Color.Lerp(Mint, Color.black, 0.25f));
            });
            WritePng(helpful, $"{CourtArtFolder}/court_zone_helpful.png");
            UnityEngine.Object.DestroyImmediate(helpful);

            // Not-helpful zone pad: lilac plate with a soft cross mark.
            var notHelpful = DrawWithPixels(220, 130, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 4, 4, w - 8, h - 8, 14, Color.Lerp(LilacSoft, Color.white, 0.5f));
                FillRoundedRect(pixels, w, h, 14, 14, w - 28, h - 28, 12, Color.Lerp(LilacSoft, Color.white, 0.72f));
                FillRect(pixels, w, h, w / 2 - 18, h / 2 - 5, 36, 10, Color.Lerp(LilacSoft, Color.black, 0.2f));
                FillRect(pixels, w, h, w / 2 - 5, h / 2 - 18, 10, 36, Color.Lerp(LilacSoft, Color.black, 0.2f));
            });
            WritePng(notHelpful, $"{CourtArtFolder}/court_zone_not_helpful.png");
            UnityEngine.Object.DestroyImmediate(notHelpful);

            // Evidence tray: paper board with a soft inner well.
            var tray = DrawWithPixels(640, 104, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 16, PaperWarm);
                FillRoundedRect(pixels, w, h, 8, 8, w - 16, h - 16, 12, Color.Lerp(PaperWarm, Color.black, 0.06f));
            });
            WritePng(tray, $"{CourtArtFolder}/court_tray.png");
            UnityEngine.Object.DestroyImmediate(tray);

            // Conclusion stamp: amber handle over a dark base (the P14 punch prop).
            var stamp = DrawWithPixels(90, 110, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, 10, w / 2 - 12, 7, SoftShadow);
                FillRoundedRect(pixels, w, h, 10, 8, w - 20, 26, 8, Color.Lerp(InkSoft, Color.white, 0.18f));
                FillRoundedRect(pixels, w, h, w / 2 - 9, 30, 18, h - 62, 6, Color.Lerp(LogicAmber, Color.black, 0.12f));
                FillEllipse(pixels, w, h, w / 2, h - 18, 22, 13, LogicAmber);
            });
            WritePng(stamp, $"{CourtArtFolder}/court_stamp.png");
            UnityEngine.Object.DestroyImmediate(stamp);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Structure and names mirror CampusRoomScenes.BuildFallbackCourt and the
        /// LogicCourtLayout single coordinate truth. World band 200-299;
        /// controller-spawned cards sit at 330. The judge NPC is NOT baked:
        /// CampusRoomScenes creates it in code for both paths (P14 hook). The
        /// conclusion stamp IS baked (named prop the stamp punch animates).
        /// </summary>
        private static GameObject ComposeLogicCourt()
        {
            var root = new GameObject("LogicCourt");

            AddSprite(root.transform, "Backdrop", LoadSprite(CourtBackdropPath), new Vector2(0f, 0.18f), new Vector2(8.35f, 4.7f), 200);

            AddSprite(root.transform, "JudgeBench", CourtSprite("court_bench.png"), new Vector2(-2.05f, -0.42f), new Vector2(2.4f, 0.95f), 210);
            AddSprite(root.transform, LogicCourtLayout.StampPropName, CourtSprite("court_stamp.png"), LogicCourtLayout.StampPosition, new Vector2(0.42f, 0.52f), 218);

            var podiumZone = LogicCourtLayout.PodiumZonePosition;
            AddSprite(root.transform, "CourtPodium", CourtSprite("court_podium.png"), new Vector2(podiumZone.x, podiumZone.y - 0.18f), new Vector2(0.85f, 0.95f), 210);
            AddAnchor(root.transform, LogicCourtLayout.ZoneAnchorPrefix + LogicCourtLayout.PodiumZoneId, podiumZone);

            AddSprite(root.transform, "HelpfulZonePad", CourtSprite("court_zone_helpful.png"), LogicCourtLayout.HelpfulZonePosition, new Vector2(1.4f, 0.9f), 206);
            AddAnchor(root.transform, LogicCourtLayout.ZoneAnchorPrefix + LogicCourtLayout.HelpfulZoneId, LogicCourtLayout.HelpfulZonePosition);

            AddSprite(root.transform, "NotHelpfulZonePad", CourtSprite("court_zone_not_helpful.png"), LogicCourtLayout.NotHelpfulZonePosition, new Vector2(1.4f, 0.9f), 206);
            AddAnchor(root.transform, LogicCourtLayout.ZoneAnchorPrefix + LogicCourtLayout.NotHelpfulZoneId, LogicCourtLayout.NotHelpfulZonePosition);

            AddSprite(root.transform, "ArgumentMeterProp", LoadSprite(ArgumentMeterPropPath), new Vector2(3.3f, 0.42f), new Vector2(0.8f, 0.8f), 212);

            AddSprite(root.transform, "EvidenceTrayBoard", CourtSprite("court_tray.png"), new Vector2(0f, LogicCourtLayout.TrayPosition(0).y - 0.02f), new Vector2(6.2f, 1.0f), 208);
            for (var i = 0; i < LogicCourtLayout.PieceIds.Length; i++)
            {
                AddAnchor(root.transform, LogicCourtLayout.TrayAnchorPrefix + i, LogicCourtLayout.TrayPosition(i));
            }

            return root;
        }

        private static Sprite CourtSprite(string fileName)
        {
            return LoadSprite($"{CourtArtFolder}/{fileName}");
        }

        // ------------------------------------------------------------------
        // U10 piece prop art: catalog-convention PNGs for the drag pieces.
        // Fill-missing only (the sprite-kit demotion policy): curated art at
        // Resources/CareerQuest/Prop/{id}.png always wins and is never
        // overwritten by a rebuild.
        // ------------------------------------------------------------------

        private const string PropResourcesFolder = "Assets/Resources/CareerQuest/Prop";

        private static void GeneratePiecePropArt()
        {
            Directory.CreateDirectory(PropResourcesFolder);

            WritePieceIfMissing("prop.symptom_clipboard", (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 18, 8, w - 36, h - 24, 12, Color.Lerp(LogicAmber, Color.white, 0.45f));
                FillRoundedRect(pixels, w, h, 26, 16, w - 52, h - 44, 10, PaperWarm);
                FillRoundedRect(pixels, w, h, w / 2 - 18, h - 22, 36, 14, 6, Color.Lerp(InkSoft, Color.white, 0.4f));
                FillRect(pixels, w, h, 34, h - 52, w - 68, 6, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 34, h - 68, w - 76, 6, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 34, h - 84, w - 72, 6, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillEllipse(pixels, w, h, w - 38, 34, 10, 10, HealthMint);
            });

            WritePieceIfMissing("prop.bandage", (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 10, h / 2 - 22, w - 20, 44, 18, new Color(0.94f, 0.78f, 0.6f));
                FillRoundedRect(pixels, w, h, w / 2 - 20, h / 2 - 16, 40, 32, 8, Color.Lerp(Color.white, new Color(0.94f, 0.78f, 0.6f), 0.2f));
                FillEllipse(pixels, w, h, 26, h / 2, 4, 4, Color.Lerp(new Color(0.94f, 0.78f, 0.6f), Color.black, 0.18f));
                FillEllipse(pixels, w, h, w - 26, h / 2, 4, 4, Color.Lerp(new Color(0.94f, 0.78f, 0.6f), Color.black, 0.18f));
            });

            WritePieceIfMissing("prop.case_file", (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 12, 16, w - 24, h - 44, 10, Color.Lerp(LogicAmber, Color.white, 0.3f));
                FillRoundedRect(pixels, w, h, 12, h - 40, 52, 18, 6, Color.Lerp(LogicAmber, Color.white, 0.3f));
                FillRoundedRect(pixels, w, h, 20, 24, w - 40, h - 64, 8, PaperWarm);
                FillRect(pixels, w, h, 28, h - 70, w - 56, 6, Color.Lerp(InkSoft, Color.white, 0.6f));
                FillRect(pixels, w, h, 28, h - 84, w - 64, 6, Color.Lerp(InkSoft, Color.white, 0.6f));
            });

            WritePieceIfMissing("prop.evidence_test", (pixels, w, h) =>
            {
                DrawEvidenceCard(pixels, w, h, Mint);
                // Bridge truss: deck + two triangle struts.
                FillRect(pixels, w, h, 28, 46, w - 56, 8, Color.Lerp(InkSoft, Color.white, 0.25f));
                FillRect(pixels, w, h, 36, 54, 8, 22, Color.Lerp(InkSoft, Color.white, 0.25f));
                FillRect(pixels, w, h, w / 2 - 4, 54, 8, 22, Color.Lerp(InkSoft, Color.white, 0.25f));
                FillRect(pixels, w, h, w - 44, 54, 8, 22, Color.Lerp(InkSoft, Color.white, 0.25f));
            });

            WritePieceIfMissing("prop.evidence_paint", (pixels, w, h) =>
            {
                DrawEvidenceCard(pixels, w, h, LilacSoft);
                // Paint blob + drip.
                FillEllipse(pixels, w, h, w / 2, 62, 24, 16, Color.Lerp(LilacSoft, Color.white, 0.1f));
                FillEllipse(pixels, w, h, w / 2 - 10, 44, 6, 9, Color.Lerp(LilacSoft, Color.white, 0.1f));
            });

            WritePieceIfMissing("prop.evidence_blueprint", (pixels, w, h) =>
            {
                DrawEvidenceCard(pixels, w, h, ScienceBlue);
                // Blueprint inset with white grid lines.
                FillRoundedRect(pixels, w, h, 30, 38, w - 60, 44, 6, Color.Lerp(ScienceBlue, Color.black, 0.15f));
                FillRect(pixels, w, h, 30, 58, w - 60, 3, Color.white);
                FillRect(pixels, w, h, w / 2 - 2, 38, 3, 44, Color.white);
            });

            AssetDatabase.Refresh();
        }

        private static void DrawEvidenceCard(Color[] pixels, int width, int height, Color accent)
        {
            FillEllipse(pixels, width, height, width / 2, 14, width / 2 - 18, 9, SoftShadow);
            FillRoundedRect(pixels, width, height, 16, 14, width - 32, height - 28, 12, PaperWarm);
            FillRect(pixels, width, height, 22, height - 36, width - 44, 16, Color.Lerp(accent, Color.white, 0.25f));
            FillRect(pixels, width, height, 26, 28, width - 52, 5, Color.Lerp(InkSoft, Color.white, 0.6f));
        }

        private static void WritePieceIfMissing(string propId, Action<Color[], int, int> draw)
        {
            var path = $"{PropResourcesFolder}/{propId}.png";
            if (File.Exists(path))
            {
                return; // curated art wins; never overwrite
            }

            var texture = DrawWithPixels(128, 128, draw);
            WritePng(texture, path);
            UnityEngine.Object.DestroyImmediate(texture);
        }

        // ------------------------------------------------------------------
        // U7: Reveal Stage prefab (in-world cinematic ceremony)
        // ------------------------------------------------------------------

        private const string RevealArtFolder = "Assets/_CareerQuest/Art/Rooms/Reveal";
        private const string WorldPrefabFolder = "Assets/_CareerQuest/Prefabs/World";
        private const string RevealPrefabAssetPath = "Assets/_CareerQuest/Prefabs/World/RevealStage.prefab";
        private const string RevealPrefabResourcesPath = "Assets/Resources/CareerQuest/World/RevealStage.prefab";
        private const string RevealBackdropPath = "Assets/Resources/CareerQuest/Room/room.reveal.png";

        // Mirrors CampusWorldPalette (internal to the runtime assembly).
        private static readonly Color StagePlaza = new(1f, 0.92f, 0.64f);
        private static readonly Color StageShadowInk = new(0.06f, 0.08f, 0.1f);
        private static readonly Color BeamGold = new(1f, 0.9f, 0.34f);
        private static readonly Color BeamBlue = new(0.55f, 0.85f, 1f);

        [MenuItem("Career Quest/World/Build Reveal Stage Prefab")]
        public static void BuildRevealStageInteractive()
        {
            BuildRevealStageCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the reveal stage prefab, then exits 0/1.</summary>
        public static void BuildRevealStage()
        {
            BuildRevealStageCore(exitWhenDone: true);
        }

        private static void BuildRevealStageCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                GenerateRevealStageArt();
                root = ComposeRevealStage();

                EnsureFolder(WorldPrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, RevealPrefabAssetPath);

                EnsureFolder(PrefabResourcesFolder);
                if (AssetDatabase.LoadAssetAtPath<GameObject>(RevealPrefabResourcesPath) != null)
                {
                    AssetDatabase.DeleteAsset(RevealPrefabResourcesPath);
                }

                if (!AssetDatabase.CopyAsset(RevealPrefabAssetPath, RevealPrefabResourcesPath))
                {
                    throw new InvalidOperationException($"Failed to copy '{RevealPrefabAssetPath}' to '{RevealPrefabResourcesPath}'.");
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ROOM_PREFAB BuildRevealStage: saved '{RevealPrefabAssetPath}' (+ runtime copy '{RevealPrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ROOM_PREFAB BuildRevealStage failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
            finally
            {
                if (root != null)
                {
                    UnityEngine.Object.DestroyImmediate(root);
                }
            }
        }

        /// <summary>
        /// P7 faked stage lighting art: glow sprites and gradient overlays only —
        /// no URP/2D lights. Idempotent (rebuild overwrites). Beam/spot/ring
        /// textures are white with baked alpha falloff; the SpriteRenderer color
        /// supplies the tint AND the dim start alpha the light-sweep beat ramps.
        /// </summary>
        private static void GenerateRevealStageArt()
        {
            Directory.CreateDirectory(RevealArtFolder);

            // Stage platform: warm rounded deck with a lighter top inset.
            var platform = DrawWithPixels(520, 100, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, h / 2, w / 2 - 2, h / 2 - 2, StagePlaza);
                FillEllipse(pixels, w, h, w / 2, h / 2 + 8, w / 2 - 16, h / 2 - 14, Color.Lerp(StagePlaza, Color.white, 0.28f));
            });
            WritePng(platform, $"{RevealArtFolder}/reveal_platform.png");
            UnityEngine.Object.DestroyImmediate(platform);

            // Soft radial contact shadow under the platform.
            var shadow = DrawWithPixels(280, 60, (pixels, w, h) =>
                FillRadialGlow(pixels, w, h, StageShadowInk, 0.5f));
            WritePng(shadow, $"{RevealArtFolder}/reveal_platform_shadow.png");
            UnityEngine.Object.DestroyImmediate(shadow);

            // Light beam: vertical gradient (bright at the source above, fading
            // toward the stage) with soft parabolic horizontal edges.
            var beam = DrawWithPixels(64, 256, FillBeamGradient);
            WritePng(beam, $"{RevealArtFolder}/reveal_glow_beam.png");
            UnityEngine.Object.DestroyImmediate(beam);

            // Stage spot pool: radial gradient overlay.
            var spot = DrawWithPixels(220, 110, (pixels, w, h) =>
                FillRadialGlow(pixels, w, h, Color.white, 1f));
            WritePng(spot, $"{RevealArtFolder}/reveal_glow_spot.png");
            UnityEngine.Object.DestroyImmediate(spot);

            // Token slot pedestal: small plaza plinth with a darker base band.
            var pedestal = DrawWithPixels(96, 46, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 2, 2, w - 4, h - 4, 8, StagePlaza);
                FillRect(pixels, w, h, 6, 2, w - 12, 8, Color.Lerp(StagePlaza, Color.black, 0.14f));
                FillRect(pixels, w, h, 6, h - 10, w - 12, 6, Color.Lerp(StagePlaza, Color.white, 0.3f));
            });
            WritePng(pedestal, $"{RevealArtFolder}/reveal_pedestal.png");
            UnityEngine.Object.DestroyImmediate(pedestal);

            // Token slot ring: soft glow annulus marking where a token lands.
            var ring = DrawWithPixels(128, 128, (pixels, w, h) =>
                FillRingGlow(pixels, w, h, Color.white, 0.72f, 0.26f));
            WritePng(ring, $"{RevealArtFolder}/reveal_slot_ring.png");
            UnityEngine.Object.DestroyImmediate(ring);

            AssetDatabase.Refresh();
        }

        /// <summary>
        /// Structure and names mirror CampusRoomScenes.BuildFallbackRevealStage
        /// (the code-built stage used when this prefab is missing) and the
        /// RevealStageLayout single coordinate truth: backdrop 200, shadow 206,
        /// platform 208, spot 212, pedestals 214, rings 215, beams 360 (so the
        /// sweep reads over stage and characters). The hero avatar is NOT baked:
        /// CampusRoomScenes creates it in code for both paths (P15 hook).
        /// </summary>
        private static GameObject ComposeRevealStage()
        {
            var root = new GameObject(RevealStageLayout.StageRootName);

            // Camera shot anchors exported as data on the prefab root.
            root.AddComponent<RevealStageAnchors>().SetData(
                RevealStageLayout.FallbackWideShot,
                RevealStageLayout.FallbackStageShot);

            AddSprite(root.transform, "RevealRoomBackdrop", LoadSprite(RevealBackdropPath), new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 200);
            AddSprite(root.transform, "RevealStageShadow", RevealSprite("reveal_platform_shadow.png"), new Vector2(0f, -0.9f), new Vector2(5.6f, 1.2f), 206);
            AddSprite(root.transform, "RevealStagePlatform", RevealSprite("reveal_platform.png"), new Vector2(0f, -0.76f), new Vector2(5.2f, 1f), 208);

            // P7 faked lighting. Start alphas/rotations ARE the light-sweep
            // beat's from-state: RevealCinematicDirector ramps renderer alpha
            // toward 1 and rotates the beams toward vertical.
            var spot = AddSprite(
                root.transform,
                RevealStageLayout.GlowSpotName,
                RevealSprite("reveal_glow_spot.png"),
                new Vector2(RevealStageLayout.StageCenter.x, RevealStageLayout.StageCenter.y - 0.35f),
                new Vector2(4.2f, 2.1f),
                212);
            TintSprite(spot, BeamGold, 0.3f);

            var beamLeft = AddSprite(root.transform, RevealStageLayout.GlowBeamLeftName, RevealSprite("reveal_glow_beam.png"), new Vector2(-1.1f, 0.65f), new Vector2(0.5f, 3.7f), 360);
            TintSprite(beamLeft, BeamGold, 0.25f);
            beamLeft.transform.localRotation = Quaternion.Euler(0f, 0f, -26f);

            var beamRight = AddSprite(root.transform, RevealStageLayout.GlowBeamRightName, RevealSprite("reveal_glow_beam.png"), new Vector2(1.1f, 0.65f), new Vector2(0.5f, 3.7f), 360);
            TintSprite(beamRight, BeamBlue, 0.25f);
            beamRight.transform.localRotation = Quaternion.Euler(0f, 0f, 26f);

            for (var i = 0; i < RevealStageLayout.SlotCount; i++)
            {
                var slot = RevealStageLayout.SlotPosition(i);
                AddSprite(root.transform, $"RevealSlotPedestal{i}", RevealSprite("reveal_pedestal.png"), new Vector2(slot.x, slot.y - 0.52f), new Vector2(0.72f, 0.34f), 214);
                var ring = AddSprite(root.transform, $"RevealSlotRing{i}", RevealSprite("reveal_slot_ring.png"), slot, new Vector2(0.78f, 0.78f), 215);
                TintSprite(ring, BeamGold, 0.35f);
                AddAnchor(root.transform, RevealStageLayout.SlotAnchorPrefix + i, slot);
            }

            return root;
        }

        private static Sprite RevealSprite(string fileName)
        {
            return LoadSprite($"{RevealArtFolder}/{fileName}");
        }

        private static void TintSprite(GameObject spriteObject, Color color, float alpha)
        {
            spriteObject.GetComponent<SpriteRenderer>().color = new Color(color.r, color.g, color.b, alpha);
        }

        // ------------------------------------------------------------------
        // Helper art (idempotent — rebuild overwrites)
        // ------------------------------------------------------------------

        private static void GenerateRoomHelperArt()
        {
            Directory.CreateDirectory(RoomArtFolder);

            // Blueprint table: warm work surface with a lighter top lip.
            var table = DrawWithPixels(660, 110, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 18, PlazaCream);
                FillRect(pixels, w, h, 6, h - 14, w - 12, 8, Color.Lerp(PlazaCream, Color.white, 0.25f));
                FillRect(pixels, w, h, 6, 0, w - 12, 10, Color.Lerp(PlazaCream, Color.black, 0.12f));
            });
            WritePng(table, $"{RoomArtFolder}/room_table.png");
            UnityEngine.Object.DestroyImmediate(table);

            // Piece tray: paper board with a soft inner well.
            var tray = DrawWithPixels(640, 104, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 16, PaperWarm);
                FillRoundedRect(pixels, w, h, 8, 8, w - 16, h - 16, 12, Color.Lerp(PaperWarm, Color.black, 0.06f));
            });
            WritePng(tray, $"{RoomArtFolder}/room_tray.png");
            UnityEngine.Object.DestroyImmediate(tray);

            // Per-piece lot pads: soft accent plate with a lighter inset.
            foreach (var (id, accent) in Pieces)
            {
                var pad = DrawWithPixels(96, 88, (pixels, w, h) =>
                {
                    FillEllipse(pixels, w, h, w / 2, 10, w / 2 - 4, 9, SoftShadow);
                    FillRoundedRect(pixels, w, h, 4, 10, w - 8, h - 16, 12, Color.Lerp(accent, Color.white, 0.55f));
                    FillRoundedRect(pixels, w, h, 12, 18, w - 24, h - 32, 10, Color.Lerp(accent, Color.white, 0.72f));
                });
                WritePng(pad, $"{RoomArtFolder}/room_slot_pad_{id}.png");
                UnityEngine.Object.DestroyImmediate(pad);
            }

            // Craft supplies: pencil + ruler cluster for workshop dressing.
            var supplies = DrawWithPixels(120, 72, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 6, 10, 86, 16, 6, Amber);
                FillRect(pixels, w, h, 14, 14, 70, 3, Color.Lerp(Amber, Color.black, 0.25f));
                FillRoundedRect(pixels, w, h, 22, 38, 84, 12, 6, Coral);
                FillEllipse(pixels, w, h, 108, 44, 8, 6, Color.Lerp(Coral, Color.white, 0.4f));
            });
            WritePng(supplies, $"{RoomArtFolder}/room_supplies.png");
            UnityEngine.Object.DestroyImmediate(supplies);

            AssetDatabase.Refresh();
        }

        // ------------------------------------------------------------------
        // Prefab composition
        // ------------------------------------------------------------------

        private static GameObject ComposeDesignBuildStudio()
        {
            var root = new GameObject("DesignBuildStudio");

            // World band 200-299 per the sorting decision; pieces (controller-
            // spawned) sit at 330 in the characters/props band.
            AddSprite(root.transform, "Backdrop", LoadSprite(RoomBackdropPath), new Vector2(0f, 0.12f), new Vector2(7.4f, 4.16f), 200);
            AddSprite(root.transform, "BuildTable", RoomSprite("room_table.png"), new Vector2(0f, -0.5f), new Vector2(6.6f, 1.05f), 210);
            AddSprite(root.transform, "TrayBoard", RoomSprite("room_tray.png"), new Vector2(0f, DesignBuildStudioLayout.TrayPosition(0).y - 0.02f), new Vector2(6.2f, 1.0f), 208);
            AddSprite(root.transform, "BlueprintProp", LoadSprite(BlueprintPropPath), new Vector2(-3.15f, -0.55f), new Vector2(0.62f, 0.62f), 218);
            AddSprite(root.transform, "CraftSupplies", RoomSprite("room_supplies.png"), new Vector2(3.1f, -0.18f), new Vector2(0.85f, 0.5f), 218);

            for (var i = 0; i < Pieces.Length; i++)
            {
                var (id, _) = Pieces[i];
                var slot = DesignBuildStudioLayout.SlotPosition(i);
                AddSprite(root.transform, $"SlotPad_{id}", RoomSprite($"room_slot_pad_{id}.png"), slot, new Vector2(1.0f, 0.92f), 215);
                AddAnchor(root.transform, DesignBuildStudioLayout.SlotAnchorPrefix + id, slot);
                AddAnchor(root.transform, DesignBuildStudioLayout.TrayAnchorPrefix + i, DesignBuildStudioLayout.TrayPosition(i));
            }

            return root;
        }

        private static void AddAnchor(Transform parent, string name, Vector2 position)
        {
            var anchor = new GameObject(name);
            anchor.transform.SetParent(parent, false);
            anchor.transform.localPosition = new Vector3(position.x, position.y, 0f);
        }

        private static GameObject AddSprite(Transform parent, string name, Sprite sprite, Vector2 position, Vector2 targetSize, int order)
        {
            var spriteObject = new GameObject(name, typeof(SpriteRenderer));
            spriteObject.transform.SetParent(parent, false);
            spriteObject.transform.localPosition = new Vector3(position.x, position.y, 0f);

            var renderer = spriteObject.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;

            var bounds = sprite.bounds.size;
            var width = Mathf.Approximately(bounds.x, 0f) ? 1f : bounds.x;
            var height = Mathf.Approximately(bounds.y, 0f) ? 1f : bounds.y;
            spriteObject.transform.localScale = new Vector3(targetSize.x / width, targetSize.y / height, 1f);
            return spriteObject;
        }

        private static Sprite RoomSprite(string fileName)
        {
            return LoadSprite($"{RoomArtFolder}/{fileName}");
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite missing at '{assetPath}'. Generate room art first " +
                    "(it is written by this builder; for catalog art run the sprite kit pipeline).");
            }

            return sprite;
        }

        // ------------------------------------------------------------------
        // Texture drawing helpers (technique mirrors CareerQuestHubPrefabBuilder)
        // ------------------------------------------------------------------

        private static Texture2D DrawWithPixels(int width, int height, Action<Color[], int, int> draw)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            draw(pixels, width, height);
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void FillRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color color)
        {
            var xMin = Mathf.Clamp(x, 0, width);
            var xMax = Mathf.Clamp(x + rectWidth, 0, width);
            var yMin = Mathf.Clamp(y, 0, height);
            var yMax = Mathf.Clamp(y + rectHeight, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    pixels[py * width + px] = Blend(pixels[py * width + px], color);
                }
            }
        }

        private static void FillRoundedRect(Color[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, int radius, Color color)
        {
            radius = Mathf.Clamp(radius, 0, Mathf.Min(rectWidth, rectHeight) / 2);
            var xMin = Mathf.Clamp(x, 0, width);
            var xMax = Mathf.Clamp(x + rectWidth, 0, width);
            var yMin = Mathf.Clamp(y, 0, height);
            var yMax = Mathf.Clamp(y + rectHeight, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    var localX = px - x;
                    var localY = py - y;
                    var insideX = Mathf.Clamp(localX, radius, rectWidth - radius);
                    var insideY = Mathf.Clamp(localY, radius, rectHeight - radius);
                    var dx = localX - insideX;
                    var dy = localY - insideY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        pixels[py * width + px] = Blend(pixels[py * width + px], color);
                    }
                }
            }
        }

        private static void FillEllipse(Color[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            var rx2 = Mathf.Max(1, radiusX * radiusX);
            var ry2 = Mathf.Max(1, radiusY * radiusY);

            for (var y = Mathf.Max(0, centerY - radiusY); y < Mathf.Min(height, centerY + radiusY); y++)
            {
                for (var x = Mathf.Max(0, centerX - radiusX); x < Mathf.Min(width, centerX + radiusX); x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx * ry2 + dy * dy * rx2 <= rx2 * ry2)
                    {
                        pixels[y * width + x] = Blend(pixels[y * width + x], color);
                    }
                }
            }
        }

        /// <summary>Radial alpha falloff glow (P7 faked-lighting overlays).</summary>
        private static void FillRadialGlow(Color[] pixels, int width, int height, Color color, float peakAlpha)
        {
            var centerX = (width - 1) * 0.5f;
            var centerY = (height - 1) * 0.5f;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = (x - centerX) / (width * 0.5f);
                    var ny = (y - centerY) / (height * 0.5f);
                    var distance = Mathf.Sqrt(nx * nx + ny * ny);
                    if (distance >= 1f)
                    {
                        continue;
                    }

                    var alpha = peakAlpha * Mathf.Pow(1f - distance, 1.6f);
                    pixels[y * width + x] = Blend(pixels[y * width + x], new Color(color.r, color.g, color.b, alpha));
                }
            }
        }

        /// <summary>Soft glow annulus: alpha peaks at ringRadius (normalized) and fades both ways.</summary>
        private static void FillRingGlow(Color[] pixels, int width, int height, Color color, float ringRadius, float ringWidth)
        {
            var centerX = (width - 1) * 0.5f;
            var centerY = (height - 1) * 0.5f;

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = (x - centerX) / (width * 0.5f);
                    var ny = (y - centerY) / (height * 0.5f);
                    var distance = Mathf.Sqrt(nx * nx + ny * ny);
                    var band = 1f - Mathf.Abs(distance - ringRadius) / ringWidth;
                    if (band <= 0f)
                    {
                        continue;
                    }

                    pixels[y * width + x] = Blend(pixels[y * width + x], new Color(color.r, color.g, color.b, band * band));
                }
            }
        }

        /// <summary>
        /// Light-beam gradient: bright at the source (texture top), fading toward
        /// the stage, with soft parabolic horizontal edges.
        /// </summary>
        private static void FillBeamGradient(Color[] pixels, int width, int height)
        {
            for (var y = 0; y < height; y++)
            {
                var vertical = Mathf.Lerp(0.22f, 1f, (float)y / (height - 1));
                for (var x = 0; x < width; x++)
                {
                    var nx = Mathf.Abs((x - (width - 1) * 0.5f) / (width * 0.5f));
                    var horizontal = Mathf.Max(0f, 1f - nx * nx);
                    pixels[y * width + x] = new Color(1f, 1f, 1f, vertical * horizontal);
                }
            }
        }

        private static Color Blend(Color background, Color foreground)
        {
            var alpha = foreground.a + background.a * (1f - foreground.a);
            if (alpha <= 0f)
            {
                return Color.clear;
            }

            return new Color(
                (foreground.r * foreground.a + background.r * background.a * (1f - foreground.a)) / alpha,
                (foreground.g * foreground.a + background.g * background.a * (1f - foreground.a)) / alpha,
                (foreground.b * foreground.a + background.b * background.a * (1f - foreground.a)) / alpha,
                alpha);
        }

        private static void WritePng(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void EnsureFolder(string assetFolderPath)
        {
            if (AssetDatabase.IsValidFolder(assetFolderPath))
            {
                return;
            }

            var segments = assetFolderPath.Split('/');
            var current = segments[0];
            for (var i = 1; i < segments.Length; i++)
            {
                var next = $"{current}/{segments[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, segments[i]);
                }

                current = next;
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
