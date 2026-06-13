using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U11 accessory art: composes the final 128x128 PNG for every avatar
    /// accessory (AccessoryRewardConfig) in the owner-affirmed Kenney-soft style
    /// (flat fills, soft shadow, warm DESIGN.md palette, a clear silhouette that
    /// reads on the body at gameplay scale). Each accessory draws from its own
    /// AssetCatalog Prop definition colors so the gear color-matches its station
    /// identity, and where a Kenney Game Icons glyph fits the prop cleanly it is
    /// composited on top (wrench / gear / music / star / medal, all CC0). Props
    /// with no clean glyph match are drawn from shape primitives — the generator
    /// never hard-depends on a glyph, so a missing icon degrades the look but
    /// never fails the build.
    ///
    /// Outputs land at Assets/Resources/CareerQuest/Prop/{id}.png (stable catalog
    /// ids — the only AssetCatalog change is flipping the 14 accessory keys to
    /// required:true so the player-facing fallback gate now polices them) plus
    /// review copies under Assets/_CareerQuest/Art/Accessories/. Idempotent:
    /// re-running overwrites exactly this builder's own output ids and never
    /// touches any other file. Headless entry point Generate() always
    /// EditorApplication.Exit(0/1)s.
    ///
    /// ORCHESTRATOR: run -executeMethod
    /// CareerQuest.Editor.CareerQuestAccessoryArtBuilder.Generate
    /// (alongside CareerQuestOptionalArtBuilder.Generate and
    /// CareerQuestCharacterArtCurator.Curate) before the EditMode suite — the
    /// SpriteFallbackGate (FullPlayerFacingCatalogResolvesToFinalArt) fails until
    /// these 14 PNGs exist on disk.
    /// </summary>
    public static class CareerQuestAccessoryArtBuilder
    {
        private const string PropResourcesFolder = "Assets/Resources/CareerQuest/Prop";
        private const string ReviewFolder = "Assets/_CareerQuest/Art/Accessories";
        private const string GameIconsRoot = "Assets/_CareerQuest/Art/Kenney/GameIcons/White";

        private const int Size = 128;

        // DESIGN.md palette.
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color Paper = new(1f, 0.969f, 0.878f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.18f);

        /// <summary>
        /// One accessory's draw recipe: which primitive shape carries it, plus an
        /// optional Kenney glyph composited on top. Colors come from the catalog
        /// Prop definition at draw time so gear stays color-matched to identity.
        /// </summary>
        private sealed class AccessorySpec
        {
            public string Id;
            public AccessoryShape Shape;
            public string GlyphFile; // optional; null = no glyph

            public AccessorySpec(string id, AccessoryShape shape, string glyphFile = null)
            {
                Id = id;
                Shape = shape;
                GlyphFile = glyphFile;
            }
        }

        private enum AccessoryShape
        {
            Belt,     // tool_belt
            Goggles,  // lab_goggles, weather_goggles
            Hat,      // chef_hat
            Mic,      // microphone
            Cape,     // care_cape, explorer_cape, reveal_flourish
            Book,     // sketchbook
            Patch,    // mission_patch, press_badge
            Hardhat,  // green_hardhat
            Sash,     // badge_sash
            Robe      // star_robe
        }

        private static readonly AccessorySpec[] Specs =
        {
            new("accessory.tool_belt", AccessoryShape.Belt, "wrench.png"),
            new("accessory.lab_goggles", AccessoryShape.Goggles),
            new("accessory.chef_hat", AccessoryShape.Hat),
            new("accessory.microphone", AccessoryShape.Mic, "musicOn.png"),
            new("accessory.care_cape", AccessoryShape.Cape),
            new("accessory.sketchbook", AccessoryShape.Book),
            new("accessory.weather_goggles", AccessoryShape.Goggles),
            new("accessory.mission_patch", AccessoryShape.Patch, "star.png"),
            new("accessory.press_badge", AccessoryShape.Patch, "medal1.png"),
            new("accessory.green_hardhat", AccessoryShape.Hardhat),
            new("accessory.badge_sash", AccessoryShape.Sash, "star.png"),
            new("accessory.explorer_cape", AccessoryShape.Cape),
            new("accessory.star_robe", AccessoryShape.Robe, "star.png"),
            new("accessory.reveal_flourish", AccessoryShape.Cape, "star.png")
        };

        [MenuItem("Career Quest/Art/Generate Accessory Art")]
        public static void GenerateInteractive()
        {
            GenerateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: generates accessory art, then exits 0/1.</summary>
        public static void Generate()
        {
            GenerateCore(exitWhenDone: true);
        }

        private static void GenerateCore(bool exitWhenDone)
        {
            try
            {
                Directory.CreateDirectory(PropResourcesFolder);
                Directory.CreateDirectory(ReviewFolder);

                var written = new List<string>();
                foreach (var spec in Specs)
                {
                    var texture = DrawAccessory(spec);
                    written.AddRange(WriteBoth(texture, spec.Id));
                    UnityEngine.Object.DestroyImmediate(texture);
                }

                AssetDatabase.Refresh();
                foreach (var path in written)
                {
                    ConfigureTextureImporter(path);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_ACCESSORY_ART Generate: complete ({Specs.Length} accessories).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_ACCESSORY_ART Generate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        // ------------------------------------------------------------------
        // Per-accessory drawing (Kenney-soft: flat fills, soft shadow, sticker
        // sheen; primary/accent come from the catalog Prop definition)
        // ------------------------------------------------------------------

        private static Texture2D DrawAccessory(AccessorySpec spec)
        {
            var pixels = NewPixels(Size, Size);
            var definition = AssetCatalog.GetDefinition(spec.Id);
            var body = definition != null ? definition.PrimaryColor : QuestGold;
            var accent = definition != null ? definition.AccentColor : Paper;
            var cx = Size / 2;

            switch (spec.Shape)
            {
                case AccessoryShape.Belt:
                    DrawBelt(pixels, body, accent);
                    break;
                case AccessoryShape.Goggles:
                    DrawGoggles(pixels, body, accent);
                    break;
                case AccessoryShape.Hat:
                    DrawChefHat(pixels, body, accent);
                    break;
                case AccessoryShape.Mic:
                    DrawMic(pixels, body, accent);
                    break;
                case AccessoryShape.Cape:
                    DrawCape(pixels, body, accent);
                    break;
                case AccessoryShape.Book:
                    DrawBook(pixels, body, accent);
                    break;
                case AccessoryShape.Patch:
                    DrawPatch(pixels, body, accent);
                    break;
                case AccessoryShape.Hardhat:
                    DrawHardhat(pixels, body, accent);
                    break;
                case AccessoryShape.Sash:
                    DrawSash(pixels, body, accent);
                    break;
                default:
                    DrawRobe(pixels, body, accent);
                    break;
            }

            // Optional Kenney glyph badge, centered, tinted toward Ink so it
            // reads on the colored prop. Skipped silently if the file is absent.
            if (!string.IsNullOrEmpty(spec.GlyphFile))
            {
                var glyphPath = $"{GameIconsRoot}/{spec.GlyphFile}";
                if (File.Exists(glyphPath))
                {
                    var glyphTint = Color.Lerp(body, Ink, 0.4f);
                    BlendGlyph(pixels, Size, Size, glyphPath, cx, GlyphCenterY(spec.Shape), GlyphSize(spec.Shape), glyphTint);
                }
            }

            return ToTexture(pixels, Size, Size);
        }

        private static readonly Color QuestGold = new(0.953f, 0.769f, 0.357f);

        private static int GlyphCenterY(AccessoryShape shape)
        {
            return shape switch
            {
                AccessoryShape.Patch => Size / 2,
                AccessoryShape.Sash => Size / 2,
                AccessoryShape.Mic => Size * 5 / 8,
                _ => Size / 2
            };
        }

        private static int GlyphSize(AccessoryShape shape)
        {
            return shape switch
            {
                AccessoryShape.Patch => 54,
                AccessoryShape.Mic => 34,
                AccessoryShape.Robe => 40,
                AccessoryShape.Sash => 36,
                _ => 44
            };
        }

        private static void DrawBelt(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, Size / 2 - 8, 50, 18, SoftShadow);
            FillRoundedRect(p, Size, Size, 18, Size / 2 - 14, Size - 36, 30, 8, body);
            FillRoundedRect(p, Size, Size, Size / 2 - 17, Size / 2 - 18, 34, 38, 8, Color.Lerp(accent, Color.white, 0.2f)); // buckle
            FillRoundedRect(p, Size, Size, Size / 2 - 11, Size / 2 - 12, 22, 26, 5, Color.Lerp(body, Ink, 0.3f));
            // Pouches.
            FillRoundedRect(p, Size, Size, 26, Size / 2 - 10, 18, 22, 5, Color.Lerp(body, Ink, 0.2f));
            FillRoundedRect(p, Size, Size, Size - 44, Size / 2 - 10, 18, 22, 5, Color.Lerp(body, Ink, 0.2f));
        }

        private static void DrawGoggles(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, Size / 2 - 6, 48, 16, SoftShadow);
            FillRoundedRect(p, Size, Size, 16, Size / 2 - 6, Size - 32, 12, 6, Color.Lerp(body, Ink, 0.25f)); // strap
            foreach (var dx in new[] { -24, 24 })
            {
                FillEllipse(p, Size, Size, Size / 2 + dx, Size / 2, 22, 22, Color.Lerp(body, Ink, 0.15f));
                FillEllipse(p, Size, Size, Size / 2 + dx, Size / 2, 16, 16, Color.Lerp(accent, Color.white, 0.35f));
                FillEllipse(p, Size, Size, Size / 2 + dx - 5, Size / 2 + 5, 5, 4, new Color(1f, 1f, 1f, 0.7f)); // shine
            }

            FillRect(p, Size, Size, Size / 2 - 4, Size / 2 - 4, 8, 8, Color.Lerp(body, Ink, 0.25f)); // bridge
        }

        private static void DrawChefHat(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 40, 38, 12, SoftShadow);
            FillRoundedRect(p, Size, Size, Size / 2 - 34, 40, 68, 22, 6, Color.Lerp(body, accent, 0.2f)); // band
            // Puffy top: three overlapping puffs.
            FillEllipse(p, Size, Size, Size / 2 - 24, 78, 24, 24, body);
            FillEllipse(p, Size, Size, Size / 2 + 24, 78, 24, 24, body);
            FillEllipse(p, Size, Size, Size / 2, 92, 30, 28, body);
            FillEllipse(p, Size, Size, Size / 2 - 12, 84, 8, 8, new Color(1f, 1f, 1f, 0.5f));
        }

        private static void DrawMic(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 34, 14, 8, SoftShadow);
            FillRoundedRect(p, Size, Size, Size / 2 - 7, 22, 14, 44, 6, Color.Lerp(body, Ink, 0.3f)); // handle
            FillEllipse(p, Size, Size, Size / 2, Size / 2 + 22, 26, 30, Color.Lerp(body, Ink, 0.1f)); // head
            FillEllipse(p, Size, Size, Size / 2, Size / 2 + 22, 20, 24, Color.Lerp(accent, Color.white, 0.2f));
            for (var i = -2; i <= 2; i++)
            {
                FillRect(p, Size, Size, Size / 2 + i * 7 - 1, Size / 2 + 4, 2, 36, Color.Lerp(body, Ink, 0.35f));
            }
        }

        private static void DrawCape(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 30, 44, 12, SoftShadow);
            // Trapezoid cape: wide hem, narrow collar — built from stacked rows.
            for (var y = 18; y < 104; y++)
            {
                var t = (y - 18) / 86f;
                var half = Mathf.RoundToInt(Mathf.Lerp(46f, 20f, t));
                var shade = Color.Lerp(body, Ink, 0.12f * (1f - t));
                FillRect(p, Size, Size, Size / 2 - half, y, half * 2, 1, shade);
            }

            // Collar clasp + trim.
            FillRoundedRect(p, Size, Size, Size / 2 - 22, 96, 44, 12, 5, Color.Lerp(accent, Color.white, 0.2f));
            FillEllipse(p, Size, Size, Size / 2, 102, 8, 8, QuestGold);
            // Scalloped hem accent.
            for (var i = -3; i <= 3; i++)
            {
                FillEllipse(p, Size, Size, Size / 2 + i * 13, 22, 7, 5, Color.Lerp(body, accent, 0.4f));
            }
        }

        private static void DrawBook(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 30, 40, 12, SoftShadow);
            FillRoundedRect(p, Size, Size, 26, 28, Size - 52, Size - 56, 8, Color.Lerp(body, Ink, 0.2f)); // cover
            FillRoundedRect(p, Size, Size, 33, 34, Size - 66, Size - 68, 6, Color.Lerp(accent, Color.white, 0.4f)); // page
            FillRect(p, Size, Size, Size / 2 - 2, 32, 5, Size - 64, Color.Lerp(body, Ink, 0.35f)); // spine
            // Sketch lines + a little drawn star.
            for (var i = 0; i < 4; i++)
            {
                FillRect(p, Size, Size, 40, 44 + i * 14, 28, 3, Color.Lerp(body, Ink, 0.2f));
            }

            FillEllipse(p, Size, Size, Size * 2 / 3, Size / 2, 9, 9, QuestGold);
        }

        private static void DrawPatch(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, Size / 2 - 4, 42, 42, SoftShadow);
            FillEllipse(p, Size, Size, Size / 2, Size / 2, 44, 44, Color.Lerp(body, Ink, 0.2f)); // stitched rim
            FillEllipse(p, Size, Size, Size / 2, Size / 2, 38, 38, body);
            FillEllipse(p, Size, Size, Size / 2, Size / 2, 30, 30, Color.Lerp(accent, Color.white, 0.35f));
            // Stitch ticks around the rim.
            for (var a = 0; a < 16; a++)
            {
                var ang = a / 16f * Mathf.PI * 2f;
                var x = Size / 2 + Mathf.RoundToInt(Mathf.Cos(ang) * 41f);
                var y = Size / 2 + Mathf.RoundToInt(Mathf.Sin(ang) * 41f);
                FillRect(p, Size, Size, x - 1, y - 1, 3, 3, Color.Lerp(accent, Color.white, 0.5f));
            }
        }

        private static void DrawHardhat(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 44, 44, 12, SoftShadow);
            FillRoundedRect(p, Size, Size, 20, 50, Size - 40, 12, 6, Color.Lerp(body, accent, 0.2f)); // brim
            FillEllipse(p, Size, Size, Size / 2, 62, 36, 30, body); // dome (lower half clipped by brim visually)
            FillRect(p, Size, Size, Size / 2 - 4, 50, 8, 44, Color.Lerp(body, Ink, 0.2f)); // center ridge
            FillRect(p, Size, Size, Size / 2 - 24, 56, 6, 34, Color.Lerp(body, Ink, 0.15f));
            FillRect(p, Size, Size, Size / 2 + 18, 56, 6, 34, Color.Lerp(body, Ink, 0.15f));
            FillEllipse(p, Size, Size, Size / 2 - 10, 80, 8, 6, new Color(1f, 1f, 1f, 0.4f)); // shine
        }

        private static void DrawSash(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, Size / 2 - 6, 46, 14, SoftShadow);
            // Diagonal band corner-to-corner.
            for (var y = 8; y < Size - 8; y++)
            {
                var center = Mathf.RoundToInt(Mathf.Lerp(Size - 24, 24, (y - 8) / (float)(Size - 16)));
                FillRect(p, Size, Size, center - 16, y, 32, 1, body);
                FillRect(p, Size, Size, center - 16, y, 4, 1, Color.Lerp(body, Color.white, 0.3f)); // edge highlight
            }

            // Rosette where the sash crosses center.
            FillEllipse(p, Size, Size, Size / 2, Size / 2, 16, 16, Color.Lerp(accent, Color.white, 0.2f));
            FillEllipse(p, Size, Size, Size / 2, Size / 2, 10, 10, QuestGold);
        }

        private static void DrawRobe(Color[] p, Color body, Color accent)
        {
            FillEllipse(p, Size, Size, Size / 2 + 3, 22, 46, 12, SoftShadow);
            // Floor-length robe: wide hem to shoulders.
            for (var y = 14; y < 110; y++)
            {
                var t = (y - 14) / 96f;
                var half = Mathf.RoundToInt(Mathf.Lerp(48f, 22f, t));
                var shade = Color.Lerp(body, Ink, 0.14f * (1f - t));
                FillRect(p, Size, Size, Size / 2 - half, y, half * 2, 1, shade);
            }

            // Star-trim collar + a sprinkle of stars.
            FillRoundedRect(p, Size, Size, Size / 2 - 24, 100, 48, 12, 5, Color.Lerp(accent, Color.white, 0.25f));
            DrawStar(p, Size / 2 - 18, 60, 5, QuestGold);
            DrawStar(p, Size / 2 + 16, 44, 4, QuestGold);
            DrawStar(p, Size / 2 - 6, 30, 4, Color.Lerp(accent, Color.white, 0.4f));
        }

        private static void DrawStar(Color[] p, int cx, int cy, int r, Color color)
        {
            // Tiny five-ish point star approximated by a plus + diagonal dots.
            FillRect(p, Size, Size, cx - 1, cy - r, 3, r * 2, color);
            FillRect(p, Size, Size, cx - r, cy - 1, r * 2, 3, color);
            FillRect(p, Size, Size, cx - r + 1, cy - r + 1, 2, 2, color);
            FillRect(p, Size, Size, cx + r - 2, cy + r - 2, 2, 2, color);
        }

        // ------------------------------------------------------------------
        // Glyph compositing + drawing helpers (mirrors CareerQuestOptionalArtBuilder)
        // ------------------------------------------------------------------

        private static void BlendGlyph(Color[] pixels, int width, int height, string glyphPath, int centerX, int centerY, int targetSize, Color tint)
        {
            var glyph = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            try
            {
                if (!glyph.LoadImage(File.ReadAllBytes(glyphPath)))
                {
                    return;
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

        private static IEnumerable<string> WriteBoth(Texture2D texture, string id)
        {
            var resourcePath = $"{PropResourcesFolder}/{id}.png";
            var reviewPath = $"{ReviewFolder}/{id}.png";
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
