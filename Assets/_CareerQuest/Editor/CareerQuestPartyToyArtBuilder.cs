using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// Part B (#4) party-station toy art: composes the final 128x128 PNG for every
    /// party-station seed object (AssetCatalog.PartyToyEntries) in the owner-affirmed
    /// Kenney-soft style (flat fills, soft shadow, sticker sheen, warm DESIGN.md
    /// palette). Each toy is a cohesive station-colored token (so a child reads it as
    /// a distinct object, not the old identical tinted dot) with a CC0 Kenney Game
    /// Icons glyph composited on top where a keyword fits the toy — power for
    /// batteries/fuel, gear for wheels/robots, music for sound layers, target for
    /// routes/launches, basket for kitchen, question/check for deduce candidates, etc.
    /// Toys with no clean keyword degrade to a clean glyph-less token (never a fail).
    ///
    /// This is the "Kenney now" interim pass: the durable pipeline is the catalog
    /// keys + this builder + the fallback seam. A later AI-image pass can overwrite
    /// the same {key}.png files with literal toy illustrations — no rewiring.
    ///
    /// Outputs land at Assets/Resources/CareerQuest/Prop/{key}.png (the same stable
    /// catalog ids the renderer resolves). Idempotent: re-running overwrites exactly
    /// this builder's own toy keys and never touches any other file. Headless entry
    /// point Generate() always EditorApplication.Exit(0/1)s.
    ///
    /// ORCHESTRATOR: run -executeMethod
    /// CareerQuest.Editor.CareerQuestPartyToyArtBuilder.Generate
    /// (alongside the other art builders) before the EditMode suite — PartyToyArtTests
    /// fails until these PNGs exist on disk.
    /// </summary>
    public static class CareerQuestPartyToyArtBuilder
    {
        private const string PropResourcesFolder = "Assets/Resources/CareerQuest/Prop";
        private const string GameIconsRoot = "Assets/_CareerQuest/Art/Kenney/GameIcons/White";

        private const int Size = 128;

        // DESIGN.md palette.
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color Paper = new(1f, 0.969f, 0.878f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.18f);

        /// <summary>Keyword -> Kenney glyph file. First match (substring of the object id) wins.</summary>
        private static readonly (string[] Keywords, string GlyphFile)[] GlyphMap =
        {
            (new[] { "battery", "fuel", "power", "solar", "energy", "charge", "flashlight", "light" }, "power.png"),
            (new[] { "music", "beat", "horn", "drum", "sound", "tempo", "remix", "note", "melody", "sax", "shaker", "cymbal" }, "musicOn.png"),
            (new[] { "route", "cone", "navigate", "path", "orbit", "launch", "rocket", "probe", "satellite", "map", "goal", "rescue", "flag", "aim", "checklist" }, "target.png"),
            (new[] { "wheel", "gear", "robot", "bolt", "sensor", "antenna", "cart", "machine", "drain", "bridge", "block", "tile", "straw", "moon" }, "gear.png"),
            (new[] { "storm", "rain", "weather", "forecast", "cloud", "umbrella", "shelter", "radar", "gauge", "radio", "warning", "alert" }, "warning.png"),
            (new[] { "soup", "food", "snack", "crate", "ingredient", "bowl", "chef", "kitchen", "spice", "veggie", "pot", "recipe", "serve" }, "shoppingBasket.png"),
            (new[] { "city", "garden", "park", "green", "house", "build" }, "home.png"),
            (new[] { "fact", "check", "answer", "verified", "stamp", "publish", "true", "club", "scoop", "color_rule", "stripe", "headline" }, "checkmark.png"),
            (new[] { "rumor", "guess", "mystery", "clue", "tip", "rule", "deduce", "photo", "anon", "blurry", "question", "old_date", "size_rule", "loud", "random", "speed", "shape" }, "question.png"),
            (new[] { "star", "sparkle", "badge", "reward", "sketch", "hero", "powerup", "power_up", "press", "medal", "trophy" }, "star.png"),
        };

        [MenuItem("Career Quest/Art/Generate Party Toy Art")]
        public static void GenerateInteractive()
        {
            GenerateCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: generates party toy art, then exits 0/1.</summary>
        public static void Generate()
        {
            GenerateCore(exitWhenDone: true);
        }

        private static void GenerateCore(bool exitWhenDone)
        {
            try
            {
                Directory.CreateDirectory(PropResourcesFolder);

                var written = new List<string>();
                var count = 0;
                foreach (var (key, _, stationId) in AssetCatalog.PartyToyEntries())
                {
                    var texture = DrawToy(key, AssetCatalog.PartyToyColor(stationId));
                    var path = $"{PropResourcesFolder}/{key}.png";
                    WritePng(texture, path);
                    written.Add(path);
                    UnityEngine.Object.DestroyImmediate(texture);
                    count++;
                }

                AssetDatabase.Refresh();
                foreach (var path in written)
                {
                    ConfigureTextureImporter(path);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_PARTY_TOY_ART Generate: complete ({count} toys).");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_PARTY_TOY_ART Generate failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        // ------------------------------------------------------------------
        // Per-toy drawing (Kenney-soft token: flat body, soft shadow, sticker
        // sheen, optional keyword glyph; body color is the station identity).
        // ------------------------------------------------------------------

        private static Texture2D DrawToy(string key, Color body)
        {
            var pixels = NewPixels(Size, Size);
            var cx = Size / 2;
            var cy = Size / 2 + 4;
            var outline = Color.Lerp(body, Ink, 0.55f);
            var highlight = Color.Lerp(body, Color.white, 0.22f);
            var variant = (int)(StableHash(key) % 3);

            // Grounding shadow.
            FillEllipse(pixels, Size, Size, cx + 2, 26, 38, 11, SoftShadow);

            switch (variant)
            {
                case 0: // rounded square
                    FillRoundedRect(pixels, Size, Size, cx - 49, cy - 49, 98, 98, 26, outline);
                    FillRoundedRect(pixels, Size, Size, cx - 45, cy - 45, 90, 90, 24, body);
                    FillRoundedRect(pixels, Size, Size, cx - 45, cy + 5, 90, 40, 24, highlight);
                    break;
                case 1: // disc
                    FillEllipse(pixels, Size, Size, cx, cy, 50, 50, outline);
                    FillEllipse(pixels, Size, Size, cx, cy, 46, 46, body);
                    FillEllipse(pixels, Size, Size, cx, cy + 16, 40, 28, highlight);
                    break;
                default: // tall rounded card
                    FillRoundedRect(pixels, Size, Size, cx - 42, cy - 52, 84, 104, 18, outline);
                    FillRoundedRect(pixels, Size, Size, cx - 38, cy - 48, 76, 96, 16, body);
                    FillRoundedRect(pixels, Size, Size, cx - 38, cy + 4, 76, 44, 16, highlight);
                    break;
            }

            // Sticker sheen, upper-left.
            FillEllipse(pixels, Size, Size, cx - 18, cy + 22, 11, 7, new Color(1f, 1f, 1f, 0.35f));

            // Optional keyword glyph, near-paper so it reads on the colored body.
            var glyphFile = GlyphFor(key);
            if (glyphFile != null)
            {
                var glyphPath = $"{GameIconsRoot}/{glyphFile}";
                if (File.Exists(glyphPath))
                {
                    BlendGlyph(pixels, Size, Size, glyphPath, cx, cy, 50, new Color(1f, 0.99f, 0.95f, 0.95f));
                }
            }

            return ToTexture(pixels, Size, Size);
        }

        private static string GlyphFor(string key)
        {
            // Compare against the object id (the part after the last dot).
            var lastDot = key.LastIndexOf('.');
            var objectId = (lastDot >= 0 ? key.Substring(lastDot + 1) : key).ToLowerInvariant();
            foreach (var (keywords, glyphFile) in GlyphMap)
            {
                foreach (var keyword in keywords)
                {
                    if (objectId.IndexOf(keyword, StringComparison.Ordinal) >= 0)
                    {
                        return glyphFile;
                    }
                }
            }

            return null; // clean glyph-less token
        }

        /// <summary>Deterministic FNV-1a hash so the shape variant is stable per key.</summary>
        private static uint StableHash(string text)
        {
            uint hash = 2166136261u;
            foreach (var c in text)
            {
                hash ^= c;
                hash *= 16777619u;
            }

            return hash;
        }

        // ------------------------------------------------------------------
        // Glyph compositing + pixel helpers (mirrors CareerQuestAccessoryArtBuilder)
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
