using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U11 optional-surface art: composes final PNGs for every optional-room
    /// player-facing surface in the owner-affirmed Kenney-soft style (flat
    /// fills, no hard outlines on soft surfaces, warm palette, soft shadows):
    ///
    ///   - 4 optional-room badges (badge.ai_lab / music_studio /
    ///     robotics_garage / community_kitchen): DESIGN.md Badge Chip —
    ///     circular sticker, career-color ring, simple tool glyph composited
    ///     from the Kenney Game Icons pack (CC0), sticker sheen. 256x256.
    ///   - 4 optional-room interiors (room.{activityId}): diorama backdrop with
    ///     a themed station (console / keyboard / robot bench / kitchen
    ///     counter) so each room reads from props before text. 512x288.
    ///   - 2 campus-evolution city pieces (prop.city_piece_garage / _kitchen)
    ///     drawn in the existing city-piece family style so the skyline row
    ///     stays coherent. 128x128.
    ///
    /// Outputs land at Assets/Resources/CareerQuest/{Category}/{id}.png (stable
    /// catalog ids — zero AssetCatalog change) plus review copies under
    /// Assets/_CareerQuest/Art/. Idempotent: re-running overwrites exactly this
    /// builder's own output ids and never touches any other file — curated core
    /// art cannot be clobbered. Headless entry point Generate() always
    /// EditorApplication.Exit(0/1)s.
    /// </summary>
    public static class CareerQuestOptionalArtBuilder
    {
        private const string BadgeResourcesFolder = "Assets/Resources/CareerQuest/Badge";
        private const string RoomResourcesFolder = "Assets/Resources/CareerQuest/Room";
        private const string PropResourcesFolder = "Assets/Resources/CareerQuest/Prop";
        private const string BadgeReviewFolder = "Assets/_CareerQuest/Art/Badges";
        private const string RoomReviewFolder = "Assets/_CareerQuest/Art/Rooms";
        private const string PropReviewFolder = "Assets/_CareerQuest/Art/Rooms";
        private const string GameIconsRoot = "Assets/_CareerQuest/Art/Kenney/GameIcons/White";

        // DESIGN.md palette.
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color Paper = new(1f, 0.969f, 0.878f);
        private static readonly Color PaperShadow = new(0.851f, 0.714f, 0.435f);
        private static readonly Color Glass = new(0.83f, 0.96f, 1f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.14f);
        private static readonly Color Outline = new(0.06f, 0.08f, 0.1f, 1f); // city-piece family only

        private static readonly Color ScienceBlue = new(0.29f, 0.616f, 0.922f);
        private static readonly Color MusicLilac = new(0.62f, 0.522f, 0.863f);
        private static readonly Color WorkshopTeal = new(0.055f, 0.42f, 0.435f);
        private static readonly Color KitchenLeaf = new(0.55f, 0.82f, 0.5f);

        // U11 net-new station identity colors (vet/game/weather/spaceport/newsroom/green city).
        private static readonly Color CareMint = new(0.36f, 0.78f, 0.6f);
        private static readonly Color PlayCoral = new(0.94f, 0.34f, 0.28f);
        private static readonly Color SkyBlue = new(0.28f, 0.66f, 0.94f);
        private static readonly Color OrbitBlue = new(0.32f, 0.5f, 0.85f);
        private static readonly Color NewsOrange = new(0.96f, 0.62f, 0.18f);
        private static readonly Color CityGreen = new(0.25f, 0.64f, 0.3f);

        private sealed class BadgeSpec
        {
            public string Id;
            public Color Career;
            public string GlyphFile;

            public BadgeSpec(string id, Color career, string glyphFile)
            {
                Id = id;
                Career = career;
                GlyphFile = glyphFile;
            }
        }

        private static readonly BadgeSpec[] Badges =
        {
            new("badge.ai_lab", ScienceBlue, "gear.png"),
            new("badge.music_studio", MusicLilac, "musicOn.png"),
            new("badge.robotics_garage", WorkshopTeal, "wrench.png"),
            new("badge.community_kitchen", KitchenLeaf, "shoppingBasket.png"),
            // U11 net-new station badges.
            new("badge.vet_clinic", CareMint, "home.png"),
            new("badge.game_studio", PlayCoral, "gamepad.png"),
            new("badge.weather_lab", SkyBlue, "warning.png"),
            new("badge.spaceport", OrbitBlue, "target.png"),
            new("badge.newsroom", NewsOrange, "checkmark.png"),
            new("badge.green_city", CityGreen, "star.png")
        };

        private static readonly (string Id, Color Career)[] Rooms =
        {
            ("room.ai_lab", ScienceBlue),
            ("room.music_studio", MusicLilac),
            ("room.robotics_garage", WorkshopTeal),
            ("room.community_kitchen", KitchenLeaf),
            // U11 net-new station interiors.
            ("room.vet_clinic", CareMint),
            ("room.game_studio", PlayCoral),
            ("room.weather_lab", SkyBlue),
            ("room.spaceport", OrbitBlue),
            ("room.newsroom", NewsOrange),
            ("room.green_city", CityGreen)
        };

        [MenuItem("Career Quest/Art/Generate Optional Surface Art")]
        public static void GenerateInteractive()
        {
            GenerateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: generates optional-surface art, then exits 0/1.</summary>
        public static void Generate()
        {
            GenerateCore(exitWhenDone: true);
        }

        private static void GenerateCore(bool exitWhenDone)
        {
            try
            {
                var missing = new List<string>();
                foreach (var badge in Badges)
                {
                    if (!File.Exists($"{GameIconsRoot}/{badge.GlyphFile}"))
                    {
                        missing.Add($"{GameIconsRoot}/{badge.GlyphFile}");
                    }
                }

                if (missing.Count > 0)
                {
                    Debug.LogError(
                        "CQ_OPTIONAL_ART Generate failed — missing Kenney Game Icons glyphs:\n" +
                        string.Join("\n", missing));
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                Directory.CreateDirectory(BadgeResourcesFolder);
                Directory.CreateDirectory(RoomResourcesFolder);
                Directory.CreateDirectory(PropResourcesFolder);
                Directory.CreateDirectory(BadgeReviewFolder);
                Directory.CreateDirectory(RoomReviewFolder);

                var written = new List<string>();

                foreach (var badge in Badges)
                {
                    var texture = DrawBadgeChip(badge);
                    written.AddRange(WriteBoth(texture, badge.Id, BadgeResourcesFolder, BadgeReviewFolder));
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                foreach (var (id, career) in Rooms)
                {
                    var texture = DrawRoomInterior(id, career);
                    written.AddRange(WriteBoth(texture, id, RoomResourcesFolder, RoomReviewFolder));
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                foreach (var id in new[]
                {
                    "prop.city_piece_garage", "prop.city_piece_kitchen",
                    "prop.city_piece_vet_clinic", "prop.city_piece_game_studio",
                    "prop.city_piece_weather_lab", "prop.city_piece_spaceport",
                    "prop.city_piece_newsroom", "prop.city_piece_green_city"
                })
                {
                    var texture = DrawCityPiece(id);
                    written.AddRange(WriteBoth(texture, id, PropResourcesFolder, PropReviewFolder));
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                AssetDatabase.Refresh();
                foreach (var path in written)
                {
                    ConfigureTextureImporter(path);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_OPTIONAL_ART Generate: complete ({Badges.Length} badges, {Rooms.Length} rooms, 8 city pieces).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_OPTIONAL_ART Generate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        // ------------------------------------------------------------------
        // Badge chips (DESIGN.md Badge Chip: circular sticker, career ring,
        // tool glyph; Kenney-soft — no hard outline ring)
        // ------------------------------------------------------------------

        private static Texture2D DrawBadgeChip(BadgeSpec spec)
        {
            const int size = 256;
            var pixels = NewPixels(size, size);
            var cx = size / 2;
            var cy = size / 2;

            // Soft offset shadow grounds the sticker on any surface.
            FillEllipse(pixels, size, size, cx + 4, cy - 6, 116, 116, SoftShadow);

            // Paper sticker rim → career-color ring → light career-tint face.
            FillEllipse(pixels, size, size, cx, cy, 118, 118, Paper);
            FillEllipse(pixels, size, size, cx, cy, 106, 106, spec.Career);
            FillEllipse(pixels, size, size, cx, cy, 84, 84, Color.Lerp(spec.Career, Color.white, 0.78f));

            // Tool glyph from the Kenney Game Icons pack, tinted toward Ink for
            // contrast on the light face.
            var glyphTint = Color.Lerp(spec.Career, Ink, 0.35f);
            BlendGlyph(pixels, size, size, $"{GameIconsRoot}/{spec.GlyphFile}", cx, cy, 112, glyphTint);

            // Sticker sheen, top-left.
            FillEllipse(pixels, size, size, cx - 52, cy + 56, 26, 16, new Color(1f, 1f, 1f, 0.38f));

            return ToTexture(pixels, size, size);
        }

        // ------------------------------------------------------------------
        // Optional-room interiors (Kenney-soft diorama backdrops)
        // ------------------------------------------------------------------

        private static Texture2D DrawRoomInterior(string id, Color career)
        {
            const int w = 512;
            const int h = 288;
            var pixels = NewPixels(w, h);

            var wall = Color.Lerp(career, Color.white, 0.66f);
            var wallShade = Color.Lerp(wall, career, 0.16f);
            var floor = Color.Lerp(PaperShadow, Color.white, 0.42f);
            var floorShade = Color.Lerp(floor, PaperShadow, 0.4f);

            // Wall + a darker wainscot band; floor with a lighter top lip.
            FillRect(pixels, w, h, 0, 0, w, h, wall);
            FillRect(pixels, w, h, 0, (int)(h * 0.30f), w, (int)(h * 0.06f), wallShade);
            FillRect(pixels, w, h, 0, 0, w, (int)(h * 0.30f), floor);
            FillRect(pixels, w, h, 0, 0, w, (int)(h * 0.08f), floorShade);
            FillRect(pixels, w, h, 0, (int)(h * 0.29f), w, 4, Color.Lerp(career, Color.white, 0.3f));

            // Two soft windows with a shine line.
            foreach (var winCenter in new[] { (int)(w * 0.16f), (int)(w * 0.84f) })
            {
                var winW = (int)(w * 0.14f);
                var winH = (int)(h * 0.24f);
                var winY = (int)(h * 0.58f);
                FillRoundedRect(pixels, w, h, winCenter - winW / 2, winY, winW, winH, winW / 5, Color.Lerp(career, Color.white, 0.42f));
                FillRoundedRect(pixels, w, h, winCenter - winW / 2 + 5, winY + 5, winW - 10, winH - 10, winW / 5, Glass);
                FillRect(pixels, w, h, winCenter - winW / 2 + 9, winY + winH - winH / 4, winW - 18, winH / 9, new Color(1f, 1f, 1f, 0.5f));
            }

            // Career-color pennant string across the top (handmade classroom feel).
            for (var i = 0; i < 7; i++)
            {
                var px = (int)(w * (0.14f + i * 0.12f));
                var flag = i % 2 == 0 ? career : Color.Lerp(career, Paper, 0.55f);
                FillTriangleDown(pixels, w, h, px, (int)(h * 0.955f), (int)(w * 0.035f), (int)(h * 0.085f), flag);
            }

            FillRect(pixels, w, h, (int)(w * 0.08f), (int)(h * 0.955f), (int)(w * 0.84f), 3, Color.Lerp(career, Ink, 0.25f));

            // Center-stage soft shadow where the station sits.
            FillEllipse(pixels, w, h, w / 2, (int)(h * 0.16f), (int)(w * 0.26f), (int)(h * 0.05f), SoftShadow);

            switch (id)
            {
                case "room.ai_lab":
                    DrawAiLabStation(pixels, w, h, career);
                    break;
                case "room.music_studio":
                    DrawMusicStation(pixels, w, h, career);
                    break;
                case "room.robotics_garage":
                    DrawRoboticsStation(pixels, w, h, career);
                    break;
                case "room.vet_clinic":
                    DrawVetStation(pixels, w, h, career);
                    break;
                case "room.game_studio":
                    DrawGameStudioStation(pixels, w, h, career);
                    break;
                case "room.weather_lab":
                    DrawWeatherStation(pixels, w, h, career);
                    break;
                case "room.spaceport":
                    DrawSpaceportStation(pixels, w, h, career);
                    break;
                case "room.newsroom":
                    DrawNewsroomStation(pixels, w, h, career);
                    break;
                case "room.green_city":
                    DrawGreenCityStation(pixels, w, h, career);
                    break;
                default:
                    DrawKitchenStation(pixels, w, h, career);
                    break;
            }

            // Warm light wash unifies the diorama.
            FillRect(pixels, w, h, 0, 0, w, h, new Color(1f, 0.9f, 0.56f, 0.08f));
            return ToTexture(pixels, w, h);
        }

        private static void DrawAiLabStation(Color[] pixels, int w, int h, Color career)
        {
            // Console desk with a big training screen and an antenna.
            var deskX = (int)(w * 0.32f);
            var deskW = (int)(w * 0.36f);
            FillRoundedRect(pixels, w, h, deskX, (int)(h * 0.16f), deskW, (int)(h * 0.16f), 10, Color.Lerp(career, Ink, 0.35f));
            FillRoundedRect(pixels, w, h, deskX + 6, (int)(h * 0.25f), deskW - 12, (int)(h * 0.05f), 6, Color.Lerp(career, Color.white, 0.25f));

            var screenX = (int)(w * 0.35f);
            var screenW = (int)(w * 0.30f);
            FillRoundedRect(pixels, w, h, screenX, (int)(h * 0.34f), screenW, (int)(h * 0.30f), 12, Color.Lerp(career, Ink, 0.45f));
            FillRoundedRect(pixels, w, h, screenX + 7, (int)(h * 0.37f), screenW - 14, (int)(h * 0.24f), 9, new Color(0.62f, 0.86f, 0.96f, 1f));

            // Constellation dots + connecting bar = the "model" being trained.
            FillEllipse(pixels, w, h, (int)(w * 0.41f), (int)(h * 0.50f), 6, 6, Color.white);
            FillEllipse(pixels, w, h, (int)(w * 0.50f), (int)(h * 0.55f), 6, 6, Color.white);
            FillEllipse(pixels, w, h, (int)(w * 0.58f), (int)(h * 0.46f), 6, 6, Color.white);
            FillRect(pixels, w, h, (int)(w * 0.41f), (int)(h * 0.49f), (int)(w * 0.17f), 3, new Color(1f, 1f, 1f, 0.75f));

            // Antenna with a science-blue beacon.
            FillRect(pixels, w, h, (int)(w * 0.49f), (int)(h * 0.64f), 6, (int)(h * 0.12f), Color.Lerp(career, Ink, 0.3f));
            FillEllipse(pixels, w, h, (int)(w * 0.495f) + 3, (int)(h * 0.78f), 10, 10, Color.Lerp(career, Color.white, 0.3f));
        }

        private static void DrawMusicStation(Color[] pixels, int w, int h, Color career)
        {
            // Keyboard stand: white key bar + black keys, on lilac legs.
            var keyX = (int)(w * 0.30f);
            var keyW = (int)(w * 0.40f);
            var keyY = (int)(h * 0.30f);
            FillRoundedRect(pixels, w, h, keyX, keyY, keyW, (int)(h * 0.12f), 8, Paper);
            for (var i = 0; i < 9; i++)
            {
                FillRect(pixels, w, h, keyX + 14 + i * (keyW - 28) / 9, keyY + (int)(h * 0.05f), (int)(w * 0.018f), (int)(h * 0.06f), Color.Lerp(career, Ink, 0.5f));
            }

            FillRect(pixels, w, h, keyX + 16, (int)(h * 0.18f), 8, (int)(h * 0.12f), Color.Lerp(career, Ink, 0.3f));
            FillRect(pixels, w, h, keyX + keyW - 24, (int)(h * 0.18f), 8, (int)(h * 0.12f), Color.Lerp(career, Ink, 0.3f));

            // Floating notes.
            FillEllipse(pixels, w, h, (int)(w * 0.40f), (int)(h * 0.58f), 9, 7, career);
            FillRect(pixels, w, h, (int)(w * 0.415f), (int)(h * 0.58f), 4, (int)(h * 0.12f), career);
            FillEllipse(pixels, w, h, (int)(w * 0.55f), (int)(h * 0.66f), 9, 7, Color.Lerp(career, Ink, 0.25f));
            FillRect(pixels, w, h, (int)(w * 0.565f), (int)(h * 0.66f), 4, (int)(h * 0.12f), Color.Lerp(career, Ink, 0.25f));

            // Speaker box stage-right.
            FillRoundedRect(pixels, w, h, (int)(w * 0.70f), (int)(h * 0.16f), (int)(w * 0.10f), (int)(h * 0.22f), 8, Color.Lerp(career, Ink, 0.4f));
            FillEllipse(pixels, w, h, (int)(w * 0.75f), (int)(h * 0.27f), (int)(w * 0.028f), (int)(w * 0.028f), Color.Lerp(career, Color.white, 0.4f));
        }

        private static void DrawRoboticsStation(Color[] pixels, int w, int h, Color career)
        {
            // Workbench with a friendly half-built robot on top.
            var benchX = (int)(w * 0.30f);
            var benchW = (int)(w * 0.40f);
            FillRoundedRect(pixels, w, h, benchX, (int)(h * 0.16f), benchW, (int)(h * 0.14f), 10, Color.Lerp(PaperShadow, Ink, 0.18f));
            FillRect(pixels, w, h, benchX + 6, (int)(h * 0.27f), benchW - 12, (int)(h * 0.03f), Color.Lerp(PaperShadow, Color.white, 0.3f));

            // Robot body + head with glass eyes and an antenna.
            FillRoundedRect(pixels, w, h, (int)(w * 0.44f), (int)(h * 0.30f), (int)(w * 0.12f), (int)(h * 0.18f), 12, Color.Lerp(career, Color.white, 0.28f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.455f), (int)(h * 0.50f), (int)(w * 0.09f), (int)(h * 0.13f), 10, Color.Lerp(career, Color.white, 0.45f));
            FillEllipse(pixels, w, h, (int)(w * 0.478f), (int)(h * 0.565f), 7, 7, Glass);
            FillEllipse(pixels, w, h, (int)(w * 0.522f), (int)(h * 0.565f), 7, 7, Glass);
            FillRect(pixels, w, h, (int)(w * 0.498f), (int)(h * 0.63f), 4, (int)(h * 0.05f), career);
            FillEllipse(pixels, w, h, (int)(w * 0.50f), (int)(h * 0.69f), 6, 6, Color.Lerp(career, Color.white, 0.3f));

            // Spare wheel + toolbox beside the bench.
            FillEllipse(pixels, w, h, (int)(w * 0.36f), (int)(h * 0.34f), 14, 14, Color.Lerp(career, Ink, 0.4f));
            FillEllipse(pixels, w, h, (int)(w * 0.36f), (int)(h * 0.34f), 6, 6, Color.Lerp(career, Color.white, 0.5f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.60f), (int)(h * 0.30f), (int)(w * 0.07f), (int)(h * 0.07f), 6, Color.Lerp(career, Ink, 0.25f));
        }

        private static void DrawKitchenStation(Color[] pixels, int w, int h, Color career)
        {
            // Serving counter with a big soup pot and rising steam.
            var counterX = (int)(w * 0.30f);
            var counterW = (int)(w * 0.40f);
            FillRoundedRect(pixels, w, h, counterX, (int)(h * 0.16f), counterW, (int)(h * 0.16f), 10, Color.Lerp(career, Ink, 0.28f));
            FillRect(pixels, w, h, counterX + 6, (int)(h * 0.28f), counterW - 12, (int)(h * 0.04f), Paper);

            FillRoundedRect(pixels, w, h, (int)(w * 0.43f), (int)(h * 0.33f), (int)(w * 0.14f), (int)(h * 0.16f), 12, Color.Lerp(Ink, career, 0.25f));
            FillRect(pixels, w, h, (int)(w * 0.42f), (int)(h * 0.47f), (int)(w * 0.16f), (int)(h * 0.035f), Color.Lerp(Ink, Color.white, 0.35f));
            FillEllipse(pixels, w, h, (int)(w * 0.50f), (int)(h * 0.52f), 8, 5, Color.Lerp(Ink, Color.white, 0.35f));

            // Steam puffs.
            FillEllipse(pixels, w, h, (int)(w * 0.47f), (int)(h * 0.58f), 9, 9, new Color(1f, 1f, 1f, 0.5f));
            FillEllipse(pixels, w, h, (int)(w * 0.52f), (int)(h * 0.64f), 11, 11, new Color(1f, 1f, 1f, 0.4f));
            FillEllipse(pixels, w, h, (int)(w * 0.48f), (int)(h * 0.71f), 13, 13, new Color(1f, 1f, 1f, 0.3f));

            // Shelf with ingredient jars.
            FillRect(pixels, w, h, (int)(w * 0.64f), (int)(h * 0.58f), (int)(w * 0.18f), 5, Color.Lerp(PaperShadow, Ink, 0.2f));
            foreach (var (offset, jarColor) in new[] { (0.66f, career), (0.71f, Color.Lerp(career, Paper, 0.5f)), (0.76f, Color.Lerp(career, Ink, 0.25f)) })
            {
                FillRoundedRect(pixels, w, h, (int)(w * offset), (int)(h * 0.60f), (int)(w * 0.035f), (int)(h * 0.08f), 5, jarColor);
            }
        }

        private static void DrawVetStation(Color[] pixels, int w, int h, Color career)
        {
            // Exam table with a friendly pet + a wall health cross.
            FillRoundedRect(pixels, w, h, (int)(w * 0.30f), (int)(h * 0.16f), (int)(w * 0.40f), (int)(h * 0.13f), 10, Color.Lerp(PaperShadow, Ink, 0.16f));
            FillRect(pixels, w, h, (int)(w * 0.31f), (int)(h * 0.26f), (int)(w * 0.38f), (int)(h * 0.03f), Paper);

            // Pet: round body, head, two ears.
            FillEllipse(pixels, w, h, (int)(w * 0.50f), (int)(h * 0.36f), (int)(w * 0.07f), (int)(h * 0.08f), Color.Lerp(career, Color.white, 0.35f));
            FillEllipse(pixels, w, h, (int)(w * 0.57f), (int)(h * 0.44f), (int)(w * 0.045f), (int)(w * 0.045f), Color.Lerp(career, Color.white, 0.45f));
            FillTriangleDown(pixels, w, h, (int)(w * 0.555f), (int)(h * 0.52f), (int)(w * 0.02f), (int)(h * 0.05f), Color.Lerp(career, Ink, 0.2f));
            FillTriangleDown(pixels, w, h, (int)(w * 0.59f), (int)(h * 0.52f), (int)(w * 0.02f), (int)(h * 0.05f), Color.Lerp(career, Ink, 0.2f));
            FillEllipse(pixels, w, h, (int)(w * 0.585f), (int)(h * 0.45f), 3, 3, Ink);

            // Health-cross sign on the wall.
            FillRoundedRect(pixels, w, h, (int)(w * 0.36f), (int)(h * 0.62f), (int)(w * 0.10f), (int)(h * 0.16f), 8, Paper);
            FillRect(pixels, w, h, (int)(w * 0.395f), (int)(h * 0.655f), (int)(w * 0.03f), (int)(h * 0.09f), career);
            FillRect(pixels, w, h, (int)(w * 0.375f), (int)(h * 0.685f), (int)(w * 0.07f), (int)(h * 0.03f), career);
        }

        private static void DrawGameStudioStation(Color[] pixels, int w, int h, Color career)
        {
            // Big play screen with a pixel hero + a gamepad on the desk.
            FillRoundedRect(pixels, w, h, (int)(w * 0.31f), (int)(h * 0.16f), (int)(w * 0.38f), (int)(h * 0.12f), 8, Color.Lerp(career, Ink, 0.35f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.34f), (int)(h * 0.31f), (int)(w * 0.32f), (int)(h * 0.30f), 12, Color.Lerp(career, Ink, 0.45f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.355f), (int)(h * 0.34f), (int)(w * 0.29f), (int)(h * 0.24f), 9, new Color(0.12f, 0.16f, 0.22f, 1f));

            // Pixel hero (blocky) + a coin.
            foreach (var (ox, oy, c) in new[] { (0.46f, 0.42f, career), (0.49f, 0.42f, career), (0.46f, 0.45f, Color.Lerp(career, Color.white, 0.4f)), (0.49f, 0.45f, Color.Lerp(career, Color.white, 0.4f)), (0.475f, 0.48f, career) })
            {
                FillRect(pixels, w, h, (int)(w * ox), (int)(h * oy), (int)(w * 0.025f), (int)(h * 0.04f), c);
            }
            FillEllipse(pixels, w, h, (int)(w * 0.57f), (int)(h * 0.50f), 7, 7, new Color(0.98f, 0.82f, 0.3f, 1f));

            // Gamepad on the desk.
            FillRoundedRect(pixels, w, h, (int)(w * 0.43f), (int)(h * 0.18f), (int)(w * 0.14f), (int)(h * 0.06f), 8, Color.Lerp(career, Color.white, 0.3f));
            FillEllipse(pixels, w, h, (int)(w * 0.46f), (int)(h * 0.21f), 4, 4, Ink);
            FillEllipse(pixels, w, h, (int)(w * 0.54f), (int)(h * 0.21f), 4, 4, Ink);
        }

        private static void DrawWeatherStation(Color[] pixels, int w, int h, Color career)
        {
            // Forecast screen: sun behind a cloud with rain.
            FillRoundedRect(pixels, w, h, (int)(w * 0.32f), (int)(h * 0.30f), (int)(w * 0.36f), (int)(h * 0.32f), 12, Color.Lerp(career, Ink, 0.42f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.335f), (int)(h * 0.33f), (int)(w * 0.33f), (int)(h * 0.26f), 9, Color.Lerp(SkyBlue, Color.white, 0.55f));
            FillRoundedRect(pixels, w, h, (int)(w * 0.33f), (int)(h * 0.16f), (int)(w * 0.34f), (int)(h * 0.12f), 8, Color.Lerp(career, Ink, 0.3f));

            // Sun.
            FillEllipse(pixels, w, h, (int)(w * 0.44f), (int)(h * 0.52f), (int)(w * 0.04f), (int)(w * 0.04f), new Color(0.99f, 0.84f, 0.32f, 1f));
            // Cloud (overlapping puffs).
            FillEllipse(pixels, w, h, (int)(w * 0.52f), (int)(h * 0.48f), (int)(w * 0.05f), (int)(h * 0.05f), Color.white);
            FillEllipse(pixels, w, h, (int)(w * 0.57f), (int)(h * 0.46f), (int)(w * 0.04f), (int)(h * 0.045f), Color.white);
            FillEllipse(pixels, w, h, (int)(w * 0.485f), (int)(h * 0.46f), (int)(w * 0.035f), (int)(h * 0.04f), Color.white);
            // Rain.
            foreach (var rx in new[] { 0.50f, 0.54f, 0.58f })
            {
                FillRect(pixels, w, h, (int)(w * rx), (int)(h * 0.37f), 3, (int)(h * 0.05f), SkyBlue);
            }
        }

        private static void DrawSpaceportStation(Color[] pixels, int w, int h, Color career)
        {
            // Launch pad + rocket aimed at a rescue-target ring.
            FillRoundedRect(pixels, w, h, (int)(w * 0.34f), (int)(h * 0.16f), (int)(w * 0.20f), (int)(h * 0.05f), 6, Color.Lerp(PaperShadow, Ink, 0.2f));

            // Rocket body + nose + fins + flame.
            FillRoundedRect(pixels, w, h, (int)(w * 0.41f), (int)(h * 0.24f), (int)(w * 0.06f), (int)(h * 0.26f), 10, Color.Lerp(career, Color.white, 0.4f));
            FillTriangleDown(pixels, w, h, (int)(w * 0.44f) - (int)(w * 0.04f) / 2, (int)(h * 0.60f), (int)(w * 0.04f), (int)(h * 0.08f), Color.Lerp(PlayCoral, Color.white, 0.2f));
            FillEllipse(pixels, w, h, (int)(w * 0.44f), (int)(h * 0.40f), 6, 6, Glass);
            FillTriangleDown(pixels, w, h, (int)(w * 0.40f), (int)(h * 0.30f), (int)(w * 0.03f), (int)(h * 0.06f), Color.Lerp(career, Ink, 0.2f));
            FillTriangleDown(pixels, w, h, (int)(w * 0.46f), (int)(h * 0.30f), (int)(w * 0.03f), (int)(h * 0.06f), Color.Lerp(career, Ink, 0.2f));
            FillTriangleDown(pixels, w, h, (int)(w * 0.425f), (int)(h * 0.20f), (int)(w * 0.03f), (int)(h * 0.05f), new Color(0.99f, 0.66f, 0.22f, 1f));

            // Rescue-target ring stage-right.
            FillEllipse(pixels, w, h, (int)(w * 0.62f), (int)(h * 0.50f), (int)(w * 0.05f), (int)(w * 0.05f), Color.Lerp(career, Color.white, 0.45f));
            FillEllipse(pixels, w, h, (int)(w * 0.62f), (int)(h * 0.50f), (int)(w * 0.03f), (int)(w * 0.03f), Paper);
            FillEllipse(pixels, w, h, (int)(w * 0.62f), (int)(h * 0.50f), (int)(w * 0.012f), (int)(w * 0.012f), PlayCoral);
        }

        private static void DrawNewsroomStation(Color[] pixels, int w, int h, Color career)
        {
            // News desk + headline board + a standing microphone.
            FillRoundedRect(pixels, w, h, (int)(w * 0.30f), (int)(h * 0.16f), (int)(w * 0.40f), (int)(h * 0.14f), 10, Color.Lerp(career, Ink, 0.32f));
            FillRect(pixels, w, h, (int)(w * 0.31f), (int)(h * 0.27f), (int)(w * 0.38f), (int)(h * 0.03f), Paper);

            // Headline board with three text bars.
            FillRoundedRect(pixels, w, h, (int)(w * 0.33f), (int)(h * 0.33f), (int)(w * 0.30f), (int)(h * 0.28f), 10, Paper);
            FillRect(pixels, w, h, (int)(w * 0.345f), (int)(h * 0.36f), (int)(w * 0.27f), (int)(h * 0.05f), career);
            foreach (var by in new[] { 0.45f, 0.50f, 0.55f })
            {
                FillRect(pixels, w, h, (int)(w * 0.355f), (int)(h * by), (int)(w * 0.24f), (int)(h * 0.018f), Color.Lerp(Ink, Paper, 0.3f));
            }

            // Standing microphone.
            FillRect(pixels, w, h, (int)(w * 0.66f), (int)(h * 0.17f), 4, (int)(h * 0.30f), Color.Lerp(career, Ink, 0.3f));
            FillEllipse(pixels, w, h, (int)(w * 0.665f) + 2, (int)(h * 0.49f), 8, 10, Color.Lerp(career, Ink, 0.45f));
        }

        private static void DrawGreenCityStation(Color[] pixels, int w, int h, Color career)
        {
            // Eco skyline of blocks + two balance gauges + a solar panel.
            foreach (var (bx, bh, c) in new[] { (0.34f, 0.20f, career), (0.40f, 0.30f, Color.Lerp(career, Ink, 0.2f)), (0.46f, 0.24f, Color.Lerp(career, Color.white, 0.25f)) })
            {
                FillRoundedRect(pixels, w, h, (int)(w * bx), (int)(h * 0.16f), (int)(w * 0.05f), (int)(h * bh), 5, c);
            }
            // Leaf on the tallest block.
            FillEllipse(pixels, w, h, (int)(w * 0.425f), (int)(h * 0.50f), (int)(w * 0.025f), (int)(h * 0.05f), Color.Lerp(KitchenLeaf, Color.white, 0.2f));

            // Two balance gauges (the BalanceMeters verb), needles in the green.
            foreach (var gx in new[] { 0.56f, 0.64f })
            {
                FillEllipse(pixels, w, h, (int)(w * gx), (int)(h * 0.40f), (int)(w * 0.035f), (int)(w * 0.035f), Paper);
                FillEllipse(pixels, w, h, (int)(w * gx), (int)(h * 0.40f), (int)(w * 0.03f), (int)(w * 0.03f), Color.Lerp(career, Color.white, 0.4f));
                FillRect(pixels, w, h, (int)(w * gx), (int)(h * 0.40f), 3, (int)(h * 0.05f), Ink);
            }

            // Solar panel on a small stand.
            FillRoundedRect(pixels, w, h, (int)(w * 0.55f), (int)(h * 0.18f), (int)(w * 0.10f), (int)(h * 0.05f), 4, Color.Lerp(SkyBlue, Ink, 0.3f));
        }

        // ------------------------------------------------------------------
        // City pieces (existing prop.city_piece_* family style: chunky outline)
        // ------------------------------------------------------------------

        private static Texture2D DrawCityPiece(string id)
        {
            const int w = 128;
            const int h = 128;
            var pixels = NewPixels(w, h);
            var definition = AssetCatalog.GetDefinition(id);
            var body = definition != null ? definition.PrimaryColor : WorkshopTeal;
            var roof = definition != null ? definition.AccentColor : Ink;

            FillEllipse(pixels, w, h, w / 2, h / 7, w / 3, h / 13, SoftShadow);
            FillRect(pixels, w, h, w / 4, h / 5, w / 2, h / 2, Outline);
            FillRect(pixels, w, h, w / 4 + 5, h / 5 + 5, w / 2 - 10, h / 2 - 10, body);
            FillRect(pixels, w, h, w / 5, h * 7 / 10, w * 3 / 5, h / 10, Outline);
            FillRect(pixels, w, h, w / 5 + 4, h * 7 / 10 + 4, w * 3 / 5 - 8, h / 10 - 8, roof);

            if (id.Contains("garage"))
            {
                // Wide garage door with slat lines.
                FillRect(pixels, w, h, w / 2 - w / 6, h / 5 + 4, w / 3, h / 4, Color.Lerp(body, Color.white, 0.4f));
                for (var i = 1; i < 4; i++)
                {
                    FillRect(pixels, w, h, w / 2 - w / 6 + 3, h / 5 + 4 + i * h / 16, w / 3 - 6, 3, Color.Lerp(body, Outline, 0.4f));
                }

                FillRect(pixels, w, h, w / 3, h * 3 / 5, w / 10, h / 12, Glass);
                FillRect(pixels, w, h, w * 17 / 30, h * 3 / 5, w / 10, h / 12, Glass);
            }
            else
            {
                // Kitchen: striped awning band, round serving window, steam puff.
                var awnY = h * 11 / 20;
                for (var i = 0; i < 5; i++)
                {
                    var stripe = i % 2 == 0 ? roof : Paper;
                    FillRect(pixels, w, h, w / 4 + 4 + i * (w / 2 - 8) / 5, awnY, (w / 2 - 8) / 5, h / 14, stripe);
                }

                FillEllipse(pixels, w, h, w / 2, h * 2 / 5, w / 9, w / 9, Glass);
                FillEllipse(pixels, w, h, w * 2 / 3, h * 5 / 6, w / 14, w / 14, new Color(1f, 1f, 1f, 0.6f));
            }

            return ToTexture(pixels, w, h);
        }

        // ------------------------------------------------------------------
        // Glyph compositing
        // ------------------------------------------------------------------

        private static void BlendGlyph(Color[] pixels, int width, int height, string glyphPath, int centerX, int centerY, int targetSize, Color tint)
        {
            var glyph = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                // LoadImage yields a readable texture regardless of importer settings.
                if (!glyph.LoadImage(File.ReadAllBytes(glyphPath)))
                {
                    throw new InvalidOperationException($"Could not decode glyph PNG at '{glyphPath}'.");
                }

                var half = targetSize / 2;
                for (var y = -half; y < half; y++)
                {
                    for (var x = -half; x < half; x++)
                    {
                        var px = centerX + x;
                        var py = centerY + y;
                        if (px < 0 || px >= width || py < 0 || py >= height)
                        {
                            continue;
                        }

                        var u = (x + half) / (float)targetSize;
                        var v = (y + half) / (float)targetSize;
                        var sample = glyph.GetPixelBilinear(u, v);
                        if (sample.a <= 0.01f)
                        {
                            continue;
                        }

                        var color = new Color(tint.r, tint.g, tint.b, sample.a * tint.a);
                        pixels[py * width + px] = Blend(pixels[py * width + px], color);
                    }
                }
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(glyph);
            }
        }

        // ------------------------------------------------------------------
        // Drawing helpers (technique mirrors CareerQuestHubPrefabBuilder)
        // ------------------------------------------------------------------

        private static Color[] NewPixels(int width, int height)
        {
            var pixels = new Color[width * height];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = Color.clear;
            }

            return pixels;
        }

        private static Texture2D ToTexture(Color[] pixels, int width, int height)
        {
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

        /// <summary>Downward-pointing pennant triangle (apex below the top edge).</summary>
        private static void FillTriangleDown(Color[] pixels, int width, int height, int centerX, int topY, int halfWidth, int triHeight, Color color)
        {
            for (var row = 0; row < triHeight; row++)
            {
                var y = topY - row;
                if (y < 0 || y >= height)
                {
                    continue;
                }

                // Wide at the string (row 0), tapering to the point below.
                var rowHalf = Mathf.RoundToInt(halfWidth * (1f - (float)row / triHeight));
                FillRect(pixels, width, height, centerX - rowHalf, y, rowHalf * 2, 1, color);
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

        private static IEnumerable<string> WriteBoth(Texture2D texture, string id, string resourcesFolder, string reviewFolder)
        {
            var resourcePath = $"{resourcesFolder}/{id}.png";
            var reviewPath = $"{reviewFolder}/{id}.png";
            WritePng(texture, resourcePath);
            WritePng(texture, reviewPath);
            return new[] { resourcePath, reviewPath };
        }

        private static void WritePng(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void ConfigureTextureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
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
