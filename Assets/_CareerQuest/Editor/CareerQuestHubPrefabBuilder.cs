using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U4 authored campus pipeline, two stages (run in this order):
    ///
    /// 1. GenerateBuildingArt — campus building PNGs in the owner-affirmed
    ///    direction (docs/references/building-direction-sample.md): Kenney-soft
    ///    flat fills, NO dark outlines, warm muted walls, saturated
    ///    activity-color roof + sign band areas (sign TEXT is world-space TMP at
    ///    runtime, never baked), soft ground shadow. Written to
    ///    Assets/Resources/CareerQuest/Campus/campus.&lt;id&gt;.png (stable catalog
    ///    IDs — zero AssetCatalog change) plus review copies under
    ///    Assets/_CareerQuest/Art/Campus/, plus hub helper sprites (ground,
    ///    paths, plaza, flag, butterfly) under Assets/_CareerQuest/Art/Campus/Hub/.
    ///
    /// 2. Build — composes the visual-only CampusHub prefab (no NetworkObject)
    ///    from Kenney sprites + the upgraded building art: 5 parallax bands,
    ///    ambient motion, WorldAnchors export. Canonical asset at
    ///    Assets/_CareerQuest/Prefabs/World/CampusHub.prefab plus a runtime copy
    ///    at Assets/Resources/CareerQuest/World/CampusHub.prefab (Resources.Load
    ///    is the only runtime mount path for the AddComponent-built controller).
    ///
    /// Both stages are idempotent (rebuild overwrites) and expose headless
    /// entry points that EditorApplication.Exit(0/1).
    /// </summary>
    public static class CareerQuestHubPrefabBuilder
    {
        private const string CampusResourcesFolder = "Assets/Resources/CareerQuest/Campus";
        private const string CampusArtFolder = "Assets/_CareerQuest/Art/Campus";
        private const string HubArtFolder = "Assets/_CareerQuest/Art/Campus/Hub";
        private const string PrefabFolder = "Assets/_CareerQuest/Prefabs/World";
        private const string PrefabAssetPath = "Assets/_CareerQuest/Prefabs/World/CampusHub.prefab";
        private const string PrefabResourcesFolder = "Assets/Resources/CareerQuest/World";
        private const string PrefabResourcesPath = "Assets/Resources/CareerQuest/World/CampusHub.prefab";
        private const string KenneyRoot = "Assets/_CareerQuest/Art/Kenney";

        // DESIGN.md palette.
        private static readonly Color WarmWall = new(0.784f, 0.710f, 0.604f);       // #C8B59A family
        private static readonly Color Glass = new(0.83f, 0.96f, 1f);
        private static readonly Color DoorWood = new(0.43f, 0.31f, 0.21f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.14f);

        private static readonly Color Coral = new(0.969f, 0.424f, 0.369f);          // Creative Coral #F76C5E
        private static readonly Color Mint = new(0.345f, 0.784f, 0.580f);           // Health Mint #58C894
        private static readonly Color Amber = new(0.949f, 0.639f, 0.231f);          // Logic Amber #F2A33B
        private static readonly Color ScienceBlue = new(0.290f, 0.616f, 0.922f);    // #4A9DEB
        private static readonly Color MusicLilac = new(0.620f, 0.522f, 0.863f);     // #9E85DC
        private static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);       // #F3C45B
        private static readonly Color WorkshopTeal = new(0.055f, 0.420f, 0.435f);   // #0E6B6F
        private static readonly Color SuccessGreen = new(0.192f, 0.651f, 0.416f);   // #31A66A
        private static readonly Color KitchenLeaf = new(0.55f, 0.82f, 0.5f);
        private static readonly Color CampusGrass = new(0.545f, 0.820f, 0.486f);    // #8BD17C
        private static readonly Color PlazaCream = new(1f, 0.92f, 0.64f);

        private enum AccentKind
        {
            Medallion,
            Cross,
            Pillars,
            Awning,
            Dome,
            GarageDoor
        }

        private sealed class BuildingSpec
        {
            public string Id;
            public Color Accent;
            public AccentKind Kind;
            public int Width;
            public int Height;

            public BuildingSpec(string id, Color accent, AccentKind kind, int width, int height)
            {
                Id = id;
                Accent = accent;
                Kind = kind;
                Width = width;
                Height = height;
            }
        }

        private static readonly BuildingSpec[] Buildings =
        {
            new("campus.design_build_studio", Coral, AccentKind.Awning, 512, 448),
            new("campus.health_hero_clinic", Mint, AccentKind.Cross, 512, 448),
            new("campus.logic_court", Amber, AccentKind.Pillars, 512, 448),
            new("campus.achievement_gallery", PathGold, AccentKind.Medallion, 512, 448),
            new("campus.reveal_stage", ScienceBlue, AccentKind.Medallion, 512, 448),
            new("campus.space_lab", ScienceBlue, AccentKind.Dome, 320, 288),
            new("campus.music_studio", MusicLilac, AccentKind.Medallion, 320, 288),
            new("campus.green_energy_center", SuccessGreen, AccentKind.Medallion, 320, 288),
            new("campus.robotics_garage", WorkshopTeal, AccentKind.GarageDoor, 320, 288),
            new("campus.community_kitchen", KitchenLeaf, AccentKind.Awning, 320, 288),
            // Design-review (2026-06-15): the six station-id stations are fully
            // playable, so they get REAL campus buildings instead of "Soon"
            // construction-site markers. Art keys match the AssetCatalog campus
            // entries and each station's CampusArtKey.
            new("campus.spaceport", ScienceBlue, AccentKind.Dome, 320, 288),
            new("campus.weather_lab", WorkshopTeal, AccentKind.Pillars, 320, 288),
            new("campus.newsroom", Amber, AccentKind.Awning, 320, 288),
            new("campus.vet_clinic", Mint, AccentKind.Awning, 320, 288),
            new("campus.game_studio", MusicLilac, AccentKind.Medallion, 320, 288),
            new("campus.green_city", SuccessGreen, AccentKind.Medallion, 320, 288)
        };

        // ------------------------------------------------------------------
        // Stage 1: building + hub helper art
        // ------------------------------------------------------------------

        [MenuItem("Career Quest/World/Generate Building Art")]
        public static void GenerateBuildingArtInteractive()
        {
            GenerateBuildingArtCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: regenerates campus building art, then exits 0/1.</summary>
        public static void GenerateBuildingArt()
        {
            GenerateBuildingArtCore(exitWhenDone: true);
        }

        private static void GenerateBuildingArtCore(bool exitWhenDone)
        {
            try
            {
                Directory.CreateDirectory(CampusResourcesFolder);
                Directory.CreateDirectory(CampusArtFolder);
                Directory.CreateDirectory(HubArtFolder);

                foreach (var spec in Buildings)
                {
                    var texture = DrawBuilding(spec);
                    WritePng(texture, $"{CampusResourcesFolder}/{spec.Id}.png");
                    WritePng(texture, $"{CampusArtFolder}/{spec.Id}.png");
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                WriteHubHelperSprites();

                AssetDatabase.Refresh();
                Debug.Log($"CQ_HUB_ART GenerateBuildingArt: complete ({Buildings.Length} buildings + hub helpers).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_HUB_ART GenerateBuildingArt failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        private static Texture2D DrawBuilding(BuildingSpec spec)
        {
            var w = spec.Width;
            var h = spec.Height;
            var pixels = NewPixels(w, h);

            var wall = Color.Lerp(WarmWall, spec.Accent, 0.08f);
            var wallShade = wall * 0.93f;
            wallShade.a = 1f;
            var roof = spec.Accent;
            var roofLight = Color.Lerp(spec.Accent, Color.white, 0.20f);
            var sign = Color.Lerp(spec.Accent, Color.white, 0.10f);
            var signInner = Color.Lerp(spec.Accent, Color.white, 0.30f);

            // Soft ground shadow (grounding without hard outlines).
            FillEllipse(pixels, w, h, w / 2, (int)(h * 0.065f), (int)(w * 0.42f), (int)(h * 0.05f), SoftShadow);

            // Wall: rounded, flat, warm; one shade step at the base.
            var wallX = (int)(w * 0.11f);
            var wallW = w - 2 * wallX;
            var wallY = (int)(h * 0.08f);
            var wallH = (int)(h * 0.62f);
            FillRoundedRect(pixels, w, h, wallX, wallY, wallW, wallH, (int)(w * 0.05f), wall);
            FillRect(pixels, w, h, wallX + 4, wallY, wallW - 8, (int)(wallH * 0.16f), wallShade);

            // Roof: saturated activity color cap with a lighter top step.
            var roofX = (int)(w * 0.07f);
            var roofW = w - 2 * roofX;
            var roofY = wallY + wallH - (int)(h * 0.02f);
            var roofH = (int)(h * 0.20f);
            FillRoundedRect(pixels, w, h, roofX, roofY, roofW, roofH, (int)(w * 0.06f), roof);
            FillRoundedRect(pixels, w, h, roofX + (int)(w * 0.025f), roofY + roofH / 2, roofW - (int)(w * 0.05f), roofH / 2, (int)(w * 0.05f), roofLight);

            // Sign band: rounded activity-color plate on the wall below the roof.
            // The sign TEXT is a world-space TMP label at runtime — never baked.
            var signW = (int)(w * 0.48f);
            var signX = (w - signW) / 2;
            var signH = (int)(h * 0.105f);
            var signY = roofY - signH - (int)(h * 0.02f);
            FillRoundedRect(pixels, w, h, signX, signY, signW, signH, signH / 3, sign);
            FillRoundedRect(pixels, w, h, signX + 6, signY + 6, signW - 12, signH - 12, signH / 3, signInner);

            // Windows: rounded glass squares with a shine line. Buildings with a
            // centered accent (cross/medallion) keep the middle bay clear.
            var winSize = (int)(w * 0.115f);
            var winY = wallY + (int)(wallH * 0.52f);
            var centerAccent = spec.Kind == AccentKind.Cross || spec.Kind == AccentKind.Medallion;
            foreach (var centerFactor in new[] { 0.285f, 0.5f, 0.715f })
            {
                if (centerAccent && Mathf.Approximately(centerFactor, 0.5f))
                {
                    continue;
                }

                var winX = (int)(w * centerFactor) - winSize / 2;
                FillRoundedRect(pixels, w, h, winX, winY, winSize, winSize, winSize / 4, Glass);
                FillRect(pixels, w, h, winX + 4, winY + winSize - winSize / 4, winSize - 8, winSize / 8, new Color(1f, 1f, 1f, 0.55f));
            }

            // Door: chunky, rounded, toy-like, oversized (kid-scale doors invite).
            var doorW = (int)(w * 0.17f);
            var doorH = (int)(wallH * 0.46f);
            var doorX = w / 2 - doorW / 2;
            FillRoundedRect(pixels, w, h, doorX, wallY, doorW, doorH, doorW / 3, DoorWood);
            FillRoundedRect(pixels, w, h, doorX + 5, wallY + 5, doorW - 10, doorH - 10, doorW / 3, Color.Lerp(DoorWood, Color.white, 0.12f));
            FillEllipse(pixels, w, h, doorX + doorW - doorW / 4, wallY + doorH / 2, doorW / 12 + 2, doorW / 12 + 2, Color.Lerp(PathGold, Color.white, 0.2f));

            DrawAccent(pixels, w, h, spec, wallX, wallW, wallY, wallH, doorX, doorW, doorH, winY);

            var texture = NewTexture(w, h);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static void DrawAccent(Color[] pixels, int w, int h, BuildingSpec spec, int wallX, int wallW, int wallY, int wallH, int doorX, int doorW, int doorH, int winY)
        {
            var accent = spec.Accent;
            switch (spec.Kind)
            {
                case AccentKind.Cross:
                {
                    var cx = w / 2;
                    var cy = winY + (int)(w * 0.058f);
                    FillEllipse(pixels, w, h, cx, cy, (int)(w * 0.052f), (int)(w * 0.052f), Color.white);
                    FillRect(pixels, w, h, cx - (int)(w * 0.011f), cy - (int)(w * 0.034f), (int)(w * 0.022f), (int)(w * 0.068f), accent);
                    FillRect(pixels, w, h, cx - (int)(w * 0.034f), cy - (int)(w * 0.011f), (int)(w * 0.068f), (int)(w * 0.022f), accent);
                    break;
                }
                case AccentKind.Pillars:
                {
                    var pillarW = (int)(w * 0.045f);
                    var pillar = Color.Lerp(WarmWall, Color.white, 0.35f);
                    FillRoundedRect(pixels, w, h, doorX - pillarW - (int)(w * 0.03f), wallY, pillarW, (int)(wallH * 0.52f), pillarW / 3, pillar);
                    FillRoundedRect(pixels, w, h, doorX + doorW + (int)(w * 0.03f), wallY, pillarW, (int)(wallH * 0.52f), pillarW / 3, pillar);
                    break;
                }
                case AccentKind.Awning:
                {
                    var awnY = wallY + doorH + (int)(h * 0.015f);
                    var awnW = doorW + (int)(w * 0.12f);
                    var awnX = w / 2 - awnW / 2;
                    var awnH = (int)(h * 0.05f);
                    var stripes = 5;
                    var stripeW = awnW / stripes;
                    for (var i = 0; i < stripes; i++)
                    {
                        var color = i % 2 == 0 ? accent : new Color(1f, 0.97f, 0.88f);
                        FillRoundedRect(pixels, w, h, awnX + i * stripeW, awnY, stripeW, awnH, 4, color);
                    }

                    break;
                }
                case AccentKind.Dome:
                {
                    var domeY = wallY + wallH + (int)(h * 0.16f);
                    FillEllipse(pixels, w, h, w / 2, domeY, (int)(w * 0.14f), (int)(h * 0.085f), Glass);
                    FillEllipse(pixels, w, h, w / 2, domeY, (int)(w * 0.10f), (int)(h * 0.06f), Color.Lerp(Glass, Color.white, 0.4f));
                    break;
                }
                case AccentKind.GarageDoor:
                {
                    var gW = (int)(w * 0.34f);
                    var gH = (int)(wallH * 0.42f);
                    var gX = w / 2 - gW / 2;
                    FillRoundedRect(pixels, w, h, gX, wallY, gW, gH, gW / 10, Color.Lerp(WarmWall, Color.white, 0.3f));
                    for (var i = 1; i < 4; i++)
                    {
                        FillRect(pixels, w, h, gX + 5, wallY + i * gH / 4, gW - 10, 4, Color.Lerp(WarmWall, accent, 0.35f));
                    }

                    break;
                }
                case AccentKind.Medallion:
                {
                    var cy = winY + (int)(w * 0.058f);
                    FillEllipse(pixels, w, h, w / 2, cy, (int)(w * 0.05f), (int)(w * 0.05f), accent);
                    FillEllipse(pixels, w, h, w / 2, cy, (int)(w * 0.032f), (int)(w * 0.032f), Color.Lerp(accent, Color.white, 0.45f));
                    break;
                }
            }
        }

        private static void WriteHubHelperSprites()
        {
            // Ground: flat campus grass with a light top lip and one base shade step.
            var ground = DrawWithPixels(1280, 340, (pixels, w, h) =>
            {
                var grassShade = new Color(CampusGrass.r * 0.92f, CampusGrass.g * 0.92f, CampusGrass.b * 0.92f, 1f);
                FillRoundedRect(pixels, w, h, 0, 0, w, h, 40, CampusGrass);
                FillRect(pixels, w, h, 0, 0, w, (int)(h * 0.18f), grassShade);
                FillRect(pixels, w, h, 0, h - 10, w, 10, Color.Lerp(CampusGrass, Color.white, 0.16f));
            });
            WritePng(ground, $"{HubArtFolder}/hub_ground.png");
            UnityEngine.Object.DestroyImmediate(ground);

            // Paths: rounded Path Gold strips with a lighter center.
            var pathH = DrawWithPixels(880, 56, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, h / 2, PathGold);
                FillRoundedRect(pixels, w, h, 8, h / 4, w - 16, h / 2, h / 4, Color.Lerp(PathGold, Color.white, 0.14f));
            });
            WritePng(pathH, $"{HubArtFolder}/hub_path_h.png");
            UnityEngine.Object.DestroyImmediate(pathH);

            var pathV = DrawWithPixels(56, 320, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 0, 0, w, h, w / 2, PathGold);
                FillRoundedRect(pixels, w, h, w / 4, 8, w / 2, h - 16, w / 4, Color.Lerp(PathGold, Color.white, 0.14f));
            });
            WritePng(pathV, $"{HubArtFolder}/hub_path_v.png");
            UnityEngine.Object.DestroyImmediate(pathV);

            var plaza = DrawWithPixels(360, 140, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2, h / 2, w / 2 - 2, h / 2 - 2, PlazaCream);
                FillEllipse(pixels, w, h, w / 2, h / 2, (int)(w * 0.4f), (int)(h * 0.38f), Color.Lerp(PlazaCream, Color.white, 0.16f));
            });
            WritePng(plaza, $"{HubArtFolder}/hub_plaza.png");
            UnityEngine.Object.DestroyImmediate(plaza);

            var pole = DrawWithPixels(16, 240, (pixels, w, h) =>
            {
                FillRoundedRect(pixels, w, h, 4, 0, 8, h, 4, new Color(0.62f, 0.58f, 0.52f));
                FillEllipse(pixels, w, h, w / 2, h - 6, 7, 7, PathGold);
            });
            WritePng(pole, $"{HubArtFolder}/hub_flag_pole.png");
            UnityEngine.Object.DestroyImmediate(pole);

            var pennant = DrawWithPixels(120, 72, (pixels, w, h) =>
            {
                for (var x = 0; x < w; x++)
                {
                    var halfHeight = (int)((h / 2f) * (1f - (float)x / w));
                    if (halfHeight < 1)
                    {
                        continue;
                    }

                    FillRect(pixels, w, h, x, h / 2 - halfHeight, 1, halfHeight * 2, Coral);
                }

                FillRect(pixels, w, h, 0, h / 2 - 4, (int)(w * 0.45f), 8, Color.Lerp(Coral, Color.white, 0.25f));
            });
            WritePng(pennant, $"{HubArtFolder}/hub_flag_pennant.png");
            UnityEngine.Object.DestroyImmediate(pennant);

            // U12 toy props (P18): plaza fountain and a hand bell on a stand.
            var fountain = DrawWithPixels(220, 200, (pixels, w, h) =>
            {
                var stone = new Color(0.78f, 0.76f, 0.72f);
                var stoneShade = new Color(stone.r * 0.9f, stone.g * 0.9f, stone.b * 0.9f, 1f);
                var water = new Color(0.62f, 0.87f, 0.97f);
                var waterLight = Color.Lerp(water, Color.white, 0.35f);

                // Ground shadow + lower basin with water ring.
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.08f), (int)(w * 0.46f), (int)(h * 0.06f), SoftShadow);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.2f), (int)(w * 0.45f), (int)(h * 0.15f), stone);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.23f), (int)(w * 0.37f), (int)(h * 0.11f), water);

                // Pedestal + upper bowl.
                FillRoundedRect(pixels, w, h, w / 2 - (int)(w * 0.05f), (int)(h * 0.24f), (int)(w * 0.1f), (int)(h * 0.3f), 8, stoneShade);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.57f), (int)(w * 0.22f), (int)(h * 0.08f), stone);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.59f), (int)(w * 0.17f), (int)(h * 0.055f), water);

                // Spout column with a light cap.
                FillRoundedRect(pixels, w, h, w / 2 - 5, (int)(h * 0.6f), 10, (int)(h * 0.22f), 5, waterLight);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.84f), (int)(w * 0.06f), (int)(h * 0.045f), waterLight);
            });
            WritePng(fountain, $"{HubArtFolder}/hub_fountain.png");
            UnityEngine.Object.DestroyImmediate(fountain);

            var bell = DrawWithPixels(120, 150, (pixels, w, h) =>
            {
                var wood = new Color(0.55f, 0.42f, 0.3f);
                var bellGold = PathGold;
                var bellLight = Color.Lerp(PathGold, Color.white, 0.3f);

                // Stand: two posts + crossbar, soft shadow.
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.06f), (int)(w * 0.4f), (int)(h * 0.045f), SoftShadow);
                FillRoundedRect(pixels, w, h, (int)(w * 0.12f), (int)(h * 0.06f), 8, (int)(h * 0.74f), 4, wood);
                FillRoundedRect(pixels, w, h, (int)(w * 0.81f), (int)(h * 0.06f), 8, (int)(h * 0.74f), 4, wood);
                FillRoundedRect(pixels, w, h, (int)(w * 0.08f), (int)(h * 0.78f), (int)(w * 0.84f), 9, 4, wood);

                // Bell dome + lip + clapper hanging from the crossbar.
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.52f), (int)(w * 0.26f), (int)(h * 0.22f), bellGold);
                FillRoundedRect(pixels, w, h, w / 2 - (int)(w * 0.28f), (int)(h * 0.3f), (int)(w * 0.56f), (int)(h * 0.08f), 6, bellLight);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.66f), (int)(w * 0.1f), (int)(h * 0.05f), bellLight);
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.24f), 6, 6, wood);
            });
            WritePng(bell, $"{HubArtFolder}/hub_bell.png");
            UnityEngine.Object.DestroyImmediate(bell);

            var butterfly = DrawWithPixels(48, 40, (pixels, w, h) =>
            {
                FillEllipse(pixels, w, h, w / 2 - 10, h / 2 + 3, 11, 12, MusicLilac);
                FillEllipse(pixels, w, h, w / 2 + 10, h / 2 + 3, 11, 12, Color.Lerp(MusicLilac, Color.white, 0.25f));
                FillEllipse(pixels, w, h, w / 2 - 9, h / 2 - 6, 8, 8, Color.Lerp(MusicLilac, Color.white, 0.35f));
                FillEllipse(pixels, w, h, w / 2 + 9, h / 2 - 6, 8, 8, MusicLilac);
                FillEllipse(pixels, w, h, w / 2, h / 2, 3, 12, new Color(0.25f, 0.2f, 0.28f));
            });
            WritePng(butterfly, $"{HubArtFolder}/hub_butterfly.png");
            UnityEngine.Object.DestroyImmediate(butterfly);

            // U8 station construction-site marker (campus visibility rule): a
            // soft, friendly "coming soon" scaffold — base plot, a little A-frame
            // building outline, and warning-stripe banner posts. White-tinted so
            // AddStationSite can recolor it per station accent.
            var site = DrawWithPixels(180, 180, (pixels, w, h) =>
            {
                var scaffold = new Color(0.55f, 0.42f, 0.3f);
                var plot = new Color(0.86f, 0.82f, 0.7f);
                var stripe = PathGold;

                // Ground shadow + plot pad.
                FillEllipse(pixels, w, h, w / 2, (int)(h * 0.1f), (int)(w * 0.44f), (int)(h * 0.06f), SoftShadow);
                FillRoundedRect(pixels, w, h, (int)(w * 0.14f), (int)(h * 0.1f), (int)(w * 0.72f), (int)(h * 0.16f), 10, plot);

                // A-frame building-to-be outline.
                for (var x = 0; x < w; x++)
                {
                    var t = (float)x / w;
                    var roofHalf = (int)((h * 0.42f) * (1f - System.Math.Abs(t - 0.5f) * 2f));
                    if (roofHalf < 1)
                    {
                        continue;
                    }

                    FillRect(pixels, w, h, x, (int)(h * 0.26f), 1, System.Math.Min(roofHalf, (int)(h * 0.34f)), Color.Lerp(plot, Color.white, 0.3f));
                }

                // Scaffold poles + warning-stripe banner across the top.
                FillRoundedRect(pixels, w, h, (int)(w * 0.16f), (int)(h * 0.24f), 8, (int)(h * 0.56f), 4, scaffold);
                FillRoundedRect(pixels, w, h, (int)(w * 0.79f), (int)(h * 0.24f), 8, (int)(h * 0.56f), 4, scaffold);
                var bannerY = (int)(h * 0.74f);
                var bannerStripes = 6;
                var stripeW = (int)(w * 0.66f) / bannerStripes;
                for (var i = 0; i < bannerStripes; i++)
                {
                    var color = i % 2 == 0 ? stripe : new Color(1f, 0.97f, 0.88f);
                    FillRoundedRect(pixels, w, h, (int)(w * 0.17f) + i * stripeW, bannerY, stripeW, (int)(h * 0.06f), 2, color);
                }
            });
            WritePng(site, $"{HubArtFolder}/hub_station_site.png");
            UnityEngine.Object.DestroyImmediate(site);
        }

        // ------------------------------------------------------------------
        // Stage 2: prefab composition
        // ------------------------------------------------------------------

        [MenuItem("Career Quest/World/Build Campus Hub Prefab")]
        public static void BuildInteractive()
        {
            BuildCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: composes and saves the CampusHub prefab, then exits 0/1.</summary>
        public static void Build()
        {
            BuildCore(exitWhenDone: true);
        }

        private static void BuildCore(bool exitWhenDone)
        {
            GameObject root = null;
            try
            {
                root = ComposeHub();

                EnsureFolder(PrefabFolder);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabAssetPath);

                // Runtime copy under Resources — the only load path available to
                // the AddComponent-built CampusWorldController and the server clamp.
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
                Debug.Log($"CQ_HUB_PREFAB Build: saved '{PrefabAssetPath}' (+ runtime copy '{PrefabResourcesPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_HUB_PREFAB Build failed: {exception}");
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

        private static GameObject ComposeHub()
        {
            var root = new GameObject("CampusHub");
            var anchors = root.AddComponent<WorldAnchors>();

            // U8: serialize the SAME 13-door district set the fallback exposes,
            // so the live prefab and the hard fallback agree exactly (ids,
            // positions, radii, walk bounds) and ValidateEntrances passes
            // identically with or without the prefab. The three Quest Yard core
            // doors and the four converted legacy-route stations keep their
            // ActivityRoute; the six station-id doors route through the generic
            // PartyStation branch.
            anchors.SetData(
                WorldAnchors.FallbackEntrancesWithStations,
                WorldAnchors.FallbackWalkBounds,
                WorldAnchors.FallbackPlayerSpawn,
                WorldAnchors.FallbackGuideSpawn);

            // --- Band: sky (glued almost fully to the camera) -------------
            var sky = AddBand(root.transform, "Band_Sky", 0.93f);
            AddSprite(sky, "Sun", Kenney("BackgroundElements/sun.png"), new Vector2(4.7f, 3.15f), new Vector2(0.85f, 0.85f), 10);
            AddDriftingCloud(sky, "Cloud1", "BackgroundElements/cloud1.png", new Vector2(-4.6f, 2.9f), new Vector2(1.7f, 0.95f), 20, 0.22f, 0.4f);
            AddDriftingCloud(sky, "Cloud2", "BackgroundElements/cloud2.png", new Vector2(0.8f, 3.35f), new Vector2(1.35f, 0.72f), 21, 0.15f, 2.1f);
            AddDriftingCloud(sky, "Cloud3", "BackgroundElements/cloud3.png", new Vector2(3.4f, 2.45f), new Vector2(1.1f, 0.6f), 22, 0.28f, 4.2f);

            // --- Band: far hills -------------------------------------------
            var far = AddBand(root.transform, "Band_FarHills", 0.72f);
            AddSprite(far, "HillsLeft", Kenney("BackgroundElements/Flat/hills1.png"), new Vector2(-3.4f, 0.55f), new Vector2(8.2f, 2.3f), 40);
            AddSprite(far, "HillsRight", Kenney("BackgroundElements/Flat/hills2.png"), new Vector2(3.6f, 0.6f), new Vector2(8.6f, 2.5f), 41);
            AddSprite(far, "FarTower", Kenney("BackgroundElements/tower_beige.png"), new Vector2(-5.7f, 1.35f), new Vector2(1.0f, 1.9f), 45);
            AddSprite(far, "FarHouse", Kenney("BackgroundElements/house_beige_front.png"), new Vector2(5.85f, 1.15f), new Vector2(1.1f, 1.2f), 46);

            // --- Band: mid trees and fences --------------------------------
            var mid = AddBand(root.transform, "Band_Mid", 0.4f);
            AddSprite(mid, "MidTreeA", Kenney("BackgroundElements/tree10.png"), new Vector2(-4.85f, 0.5f), new Vector2(1.15f, 1.55f), 120);
            // U11 owner-review swap: tree16/tree19 are cacti — desert props that
            // read wrong on a green campus. Clear leafy trees instead.
            AddSprite(mid, "MidTreeB", Kenney("BackgroundElements/tree06.png"), new Vector2(4.85f, 0.55f), new Vector2(1.1f, 1.5f), 121);
            AddSprite(mid, "MidTreeC", Kenney("BackgroundElements/tree04.png"), new Vector2(-1.6f, 0.62f), new Vector2(0.85f, 1.15f), 122);
            AddSprite(mid, "MidTreeD", Kenney("BackgroundElements/tree13.png"), new Vector2(1.7f, 0.64f), new Vector2(0.85f, 1.2f), 123);
            AddSprite(mid, "FenceLeft", Kenney("BackgroundElements/fence.png"), new Vector2(-3.95f, 0.06f), new Vector2(1.7f, 0.5f), 130);
            AddSprite(mid, "FenceRight", Kenney("BackgroundElements/fence.png"), new Vector2(3.95f, 0.06f), new Vector2(1.7f, 0.5f), 131);

            // --- Band: world (factor 0 — gameplay-aligned) ------------------
            var world = AddBand(root.transform, "Band_World", 0f);
            AddSprite(world, "Ground", Hub("hub_ground.png"), new Vector2(0f, -2.3f), new Vector2(12.8f, 3.4f), 200);
            AddSprite(world, "Plaza", Hub("hub_plaza.png"), new Vector2(0f, -0.62f), new Vector2(3.4f, 1.3f), 206);
            AddSprite(world, "PathAcross", Hub("hub_path_h.png"), new Vector2(0f, -0.96f), new Vector2(8.8f, 0.5f), 210);
            AddSprite(world, "PathUp", Hub("hub_path_v.png"), new Vector2(0f, -0.5f), new Vector2(0.5f, 3.2f), 211);

            AddSprite(world, "GrassTuftA", Kenney("BackgroundElements/grass1.png"), new Vector2(-1.5f, -0.85f), new Vector2(0.45f, 0.28f), 215);
            AddSprite(world, "GrassTuftB", Kenney("BackgroundElements/grass3.png"), new Vector2(2.25f, -1.5f), new Vector2(0.45f, 0.28f), 216);
            AddSprite(world, "GrassTuftC", Kenney("BackgroundElements/grass5.png"), new Vector2(-3.4f, -1.95f), new Vector2(0.45f, 0.28f), 217);

            // U11 owner-review swap: tree07 is a featureless brown blob at this
            // scale — tree05's green rounded canopy reads clearly as a tree.
            AddSprite(world, "NearTreeLeft", Kenney("BackgroundElements/tree05.png"), new Vector2(-4.8f, -0.55f), new Vector2(1.0f, 1.4f), 232);
            AddSprite(world, "NearTreeRight", Kenney("BackgroundElements/tree22.png"), new Vector2(4.8f, -0.45f), new Vector2(1.0f, 1.35f), 233);

            // U8 district layout: buildings sit just behind their door, grouped
            // into four readable clusters (Quest Yard core / Tech Lane left /
            // Story Street right / Care Corner bottom) that mirror the
            // WorldAnchors entrance districts. Sign text is DoorSign TMP (never
            // baked); PlayableHubController adds the per-door label at runtime.

            // Quest Yard (core quad) — upgraded owned building art.
            AddMainBuilding(world, "DesignBuildStudio", "campus.design_build_studio", "Design Build", Coral, new Vector2(-1.7f, 1.3f));
            AddMainBuilding(world, "HealthHeroClinic", "campus.health_hero_clinic", "Health Hero", Mint, new Vector2(0f, 1.65f));
            AddMainBuilding(world, "LogicCourt", "campus.logic_court", "Logic Court", Amber, new Vector2(1.7f, 1.3f));

            // Tech Lane (left column) + Story Street (right column): the four
            // converted optional rooms keep their owned small-building art.
            // Positions track their WorldAnchors door (door + ~0.55 up), pulled in
            // from the camera edge so the buildings stop clipping.
            AddSmallBuilding(world, "AiLab", "campus.space_lab", new Vector2(-4.5f, 1.05f));
            AddSmallBuilding(world, "RoboticsGarage", "campus.robotics_garage", new Vector2(-4.6f, -0.35f));
            AddSmallBuilding(world, "MusicStudio", "campus.music_studio", new Vector2(3.4f, 0.1f));
            AddSmallBuilding(world, "CommunityKitchen", "campus.community_kitchen", new Vector2(-2.3f, -1.65f));

            // Design-review (2026-06-15): these six stations are fully playable
            // (each routes the generic PartyStation branch and runs a real verb),
            // so they now show REAL campus buildings instead of "Soon" construction
            // sites. The misleading scaffold made the new-verb stations (Spaceport
            // = trace, Weather Lab = trace, Newsroom = deduce) look unbuilt, so kids
            // never entered them. Building art keys match the Buildings specs above;
            // the readable station label still mounts at runtime via the door sign.
            AddSmallBuilding(world, "Spaceport", "campus.spaceport", new Vector2(-3.4f, 0.1f));
            AddSmallBuilding(world, "GameStudio", "campus.game_studio", new Vector2(4.6f, -0.35f));
            AddSmallBuilding(world, "Newsroom", "campus.newsroom", new Vector2(4.5f, 1.05f));
            AddSmallBuilding(world, "VetClinic", "campus.vet_clinic", new Vector2(-0.9f, -2.0f));
            AddSmallBuilding(world, "WeatherLab", "campus.weather_lab", new Vector2(0.9f, -2.0f));
            AddSmallBuilding(world, "GreenCity", "campus.green_city", new Vector2(2.3f, -1.65f));

            // Living-campus beats (P9): waving flag and butterflies.
            AddSprite(world, "FlagPole", Hub("hub_flag_pole.png"), new Vector2(1.05f, -0.05f), new Vector2(0.08f, 1.35f), 246);
            var pennant = AddSprite(world, "FlagPennant", Hub("hub_flag_pennant.png"), new Vector2(1.36f, 0.5f), new Vector2(0.55f, 0.32f), 247);
            pennant.AddComponent<AmbientMotion>().Configure(AmbientMotionKind.Sway, 0f, 7f, 1.9f, 0.6f);

            // U12 interactive toys (P18): pure local click-delight — fountain
            // splash, bell ring, and a flutter burst on the SAME flag pennant
            // (sway owns rotation, flutter is scale-only — they compose).
            var fountainToy = AddSprite(world, "FountainToy", Hub("hub_fountain.png"), new Vector2(-1.45f, -1.28f), new Vector2(1.05f, 0.95f), 242);
            AddToy(fountainToy, HubToyKind.Fountain, AudioCueIds.ToyFountain, 0.55f);

            var bellToy = AddSprite(world, "BellToy", Hub("hub_bell.png"), new Vector2(2.5f, -1.08f), new Vector2(0.62f, 0.78f), 243);
            AddToy(bellToy, HubToyKind.Bell, AudioCueIds.ToyBell, 0.5f);

            AddToy(pennant, HubToyKind.Flag, AudioCueIds.ToyFlag, 0.45f);

            var butterflyA = AddSprite(world, "ButterflyA", Hub("hub_butterfly.png"), new Vector2(-2.6f, -0.4f), new Vector2(0.22f, 0.18f), 290);
            butterflyA.AddComponent<AmbientMotion>().Configure(AmbientMotionKind.Bob, 0f, 0.1f, 1.3f, 0.5f);
            var butterflyB = AddSprite(world, "ButterflyB", Hub("hub_butterfly.png"), new Vector2(3.6f, -0.2f), new Vector2(0.2f, 0.16f), 291);
            butterflyB.AddComponent<AmbientMotion>().Configure(AmbientMotionKind.Bob, 0f, 0.08f, 1.7f, 2.8f);

            // --- Band: foreground pop (slight counter-parallax) -------------
            var fore = AddBand(root.transform, "Band_Foreground", -0.12f);
            AddSprite(fore, "ForeTuftA", Kenney("BackgroundElements/grass2.png"), new Vector2(-4.2f, -3.55f), new Vector2(0.75f, 0.45f), 410);
            AddSprite(fore, "ForeTuftB", Kenney("BackgroundElements/grass4.png"), new Vector2(1.2f, -3.65f), new Vector2(0.7f, 0.42f), 411);
            AddSprite(fore, "ForeTuftC", Kenney("BackgroundElements/grass6.png"), new Vector2(4.6f, -3.5f), new Vector2(0.75f, 0.45f), 412);

            return root;
        }

        private static void AddMainBuilding(Transform parent, string name, string assetId, string label, Color accent, Vector2 position)
        {
            // Design-review (2026-06-15): the three core buildings used to bake a
            // building-name sign here AND get a runtime door label at the entry
            // circle (PlayableHubController), so each core name rendered twice and
            // crowded the Quest Yard. Small buildings only carry the runtime door
            // label; drop the baked sign so all 13 doors read with one name each.
            // (label/accent kept in the signature for call-site symmetry.)
            _ = label;
            _ = accent;
            AddSprite(parent, name, Campus(assetId), position, new Vector2(2.45f, 2.14f), 240);
        }

        private static void AddSmallBuilding(Transform parent, string name, string assetId, Vector2 position)
        {
            AddSprite(parent, name, Campus(assetId), position, new Vector2(1.45f, 1.3f), 238);
        }

        /// <summary>
        /// U8 construction-site marker for a station whose final campus art has
        /// not landed yet (campus visibility rule). A generated placeholder
        /// scaffold sprite, tinted with the station accent, plus a small "Soon"
        /// banner — the readable station name still mounts at runtime via the
        /// door sign. U11 replaces these with the real campus.{id} buildings.
        /// </summary>
        private static void AddStationSite(Transform parent, string name, Color accent, Vector2 position)
        {
            var site = AddSprite(parent, name, Hub("hub_station_site.png"), position, new Vector2(1.2f, 1.1f), 238);
            site.GetComponent<SpriteRenderer>().color = Color.Lerp(Color.white, accent, 0.35f);

            var banner = new GameObject($"{name}Banner");
            banner.transform.SetParent(parent, false);
            banner.transform.localPosition = new Vector3(position.x, position.y + 0.18f, 0f);
            banner.AddComponent<DoorSign>().SetData("Soon", accent, 0f, 260, 1.1f, 1.2f, plate: true);
        }

        /// <summary>
        /// U12 (P18): makes a hub prop clickable through the SAME Physics2D
        /// raycast path the drag framework uses. The click radius is specified
        /// in WORLD units (kid-large targets) and divided out of the sprite's
        /// fitted local scale.
        /// </summary>
        private static void AddToy(GameObject host, HubToyKind kind, string cueId, float clickRadiusWorld)
        {
            var collider = host.AddComponent<CircleCollider2D>();
            var scale = Mathf.Max(Mathf.Abs(host.transform.localScale.x), 0.0001f);
            collider.radius = clickRadiusWorld / scale;
            host.AddComponent<HubToy>().Configure(kind, cueId);
        }

        private static Transform AddBand(Transform root, string name, float factor)
        {
            var band = new GameObject(name);
            band.transform.SetParent(root, false);
            band.AddComponent<ParallaxLayer>().Configure(factor);
            return band.transform;
        }

        private static GameObject AddDriftingCloud(Transform parent, string name, string kenneyPath, Vector2 position, Vector2 targetSize, int order, float speed, float phase)
        {
            var cloud = AddSprite(parent, name, Kenney(kenneyPath), position, targetSize, order);
            cloud.AddComponent<AmbientMotion>().Configure(AmbientMotionKind.Drift, speed, 0.06f, 0.7f, phase, 14f);
            return cloud;
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

        private static Sprite Kenney(string relativePath)
        {
            return LoadSprite($"{KenneyRoot}/{relativePath}");
        }

        private static Sprite Hub(string fileName)
        {
            return LoadSprite($"{HubArtFolder}/{fileName}");
        }

        private static Sprite Campus(string assetId)
        {
            return LoadSprite($"{CampusResourcesFolder}/{assetId}.png");
        }

        private static Sprite LoadSprite(string assetPath)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Sprite missing at '{assetPath}'. Run Career Quest/World/Generate Building Art " +
                    "(CareerQuestHubPrefabBuilder.GenerateBuildingArt) before building the prefab.");
            }

            return sprite;
        }

        // ------------------------------------------------------------------
        // Texture drawing helpers (technique mirrors CareerQuestSpriteKitGenerator)
        // ------------------------------------------------------------------

        private static Texture2D DrawWithPixels(int width, int height, Action<Color[], int, int> draw)
        {
            var pixels = NewPixels(width, height);
            draw(pixels, width, height);
            var texture = NewTexture(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private static Texture2D NewTexture(int width, int height)
        {
            return new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
        }

        private static Color[] NewPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            return pixels;
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
