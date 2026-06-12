using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// U13 (P5) Windows build identity, applied reproducibly:
    ///
    ///   1. Composes the 512px app icon from the U1-affirmed art direction —
    ///      the Design Build studio building as a toy-diorama emblem on a
    ///      Campus Sky rounded tile (same primitive-fill style as the owned
    ///      campus-building art, DESIGN.md palette) — and writes it to
    ///      <see cref="IconAssetPath"/> (review copy beside the other owned art).
    ///   2. PlayerSettings: productName/window title 'Career Quest Campus',
    ///      default + Standalone icons, splash background = Campus Sky with
    ///      the emblem added as a splash logo.
    ///
    /// Splash + license note: the project runs a Unity Personal license, so the
    /// Unity splash itself stays (show/showUnityLogo remain true — hiding it
    /// needs Pro). What Personal DOES allow and we set: the splash background
    /// color (Campus Sky), the dark-on-light Unity logo style, and ADDING our
    /// own logo to the sequence (custom splash logos are a Personal-tier
    /// feature; only removing Unity branding is Pro). A typeset Fredoka
    /// wordmark would need a font-rasterization step the headless pipeline
    /// doesn't have, so the emblem carries the splash identity.
    ///
    /// Headless entry point <see cref="Apply"/> always EditorApplication.Exits
    /// (0/1); the build pipeline also runs <see cref="ApplyIdentity"/> first so
    /// every Windows build carries the identity even if the standalone step was
    /// skipped.
    /// </summary>
    public static class CareerQuestPackaging
    {
        public const string ProductName = "Career Quest Campus";
        public const string IconAssetPath = "Assets/_CareerQuest/Art/UI/app_icon.png";

        // DESIGN.md palette.
        private static readonly Color CampusSky = new(0.616f, 0.886f, 1f);     // #9DE2FF
        private static readonly Color CampusGrass = new(0.545f, 0.82f, 0.486f); // #8BD17C
        private static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);   // #F3C45B
        private static readonly Color CreativeCoral = new(0.969f, 0.424f, 0.369f); // #F76C5E
        private static readonly Color WorkshopTeal = new(0.055f, 0.42f, 0.435f);   // #0E6B6F
        private static readonly Color Paper = new(1f, 0.969f, 0.878f);          // #FFF7E0
        private static readonly Color PaperShadow = new(0.851f, 0.714f, 0.435f); // #D9B66F
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);        // #19323C
        private static readonly Color Glass = new(0.83f, 0.96f, 1f);
        private static readonly Color SoftShadow = new(0.05f, 0.07f, 0.09f, 0.16f);

        [MenuItem("Career Quest/Packaging/Apply Build Identity")]
        public static void ApplyInteractive()
        {
            ApplyCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: applies the full build identity, then exits 0/1.</summary>
        public static void Apply()
        {
            ApplyCore(exitWhenDone: true);
        }

        /// <summary>Non-exiting core for the build pipeline (CareerQuestBuild calls this first).</summary>
        public static void ApplyIdentity()
        {
            ApplyCore(exitWhenDone: false);
        }

        private static void ApplyCore(bool exitWhenDone)
        {
            try
            {
                ComposeAppIcon();
                AssetDatabase.Refresh();
                ConfigureIconImporter(IconAssetPath);
                AssignPlayerSettings();

                AssetDatabase.SaveAssets();
                Debug.Log($"CQ_PACKAGING Apply: complete (productName='{ProductName}', icon='{IconAssetPath}').");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_PACKAGING Apply failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        // ------------------------------------------------------------------
        // PlayerSettings
        // ------------------------------------------------------------------

        private static void AssignPlayerSettings()
        {
            PlayerSettings.productName = ProductName; // window title = product name on Windows

            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconAssetPath);
            if (icon == null)
            {
                throw new InvalidOperationException($"App icon failed to import at '{IconAssetPath}'.");
            }

            // Default icon (one slot) + every Standalone application slot.
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            ApplyIconsFor(NamedBuildTarget.Standalone, IconKind.Application, icon);

            // Splash: Unity Personal splash stays (license); background +
            // logo-style + our emblem logo are the Personal-tier knobs.
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = true;
            PlayerSettings.SplashScreen.backgroundColor = CampusSky;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            PlayerSettings.SplashScreen.drawMode = PlayerSettings.SplashScreen.DrawMode.AllSequential;

            var emblem = AssetDatabase.LoadAssetAtPath<Sprite>(IconAssetPath);
            if (emblem != null)
            {
                PlayerSettings.SplashScreen.logos = new[]
                {
                    PlayerSettings.SplashScreenLogo.CreateWithUnityLogo(),
                    PlayerSettings.SplashScreenLogo.Create(2f, emblem)
                };
            }
            else
            {
                Debug.LogWarning("CQ_PACKAGING: icon imported without a Sprite — splash keeps background color only.");
            }
        }

        private static void ApplyIconsFor(NamedBuildTarget target, IconKind kind, Texture2D icon)
        {
            var sizes = PlayerSettings.GetIconSizes(target, kind);
            if (sizes == null || sizes.Length == 0)
            {
                return;
            }

            var icons = new Texture2D[sizes.Length];
            for (var i = 0; i < icons.Length; i++)
            {
                icons[i] = icon; // Unity scales the 512 source per slot
            }

            PlayerSettings.SetIcons(target, icons, kind);
        }

        // ------------------------------------------------------------------
        // Icon composition (owned-art style: primitive fills, DESIGN palette)
        // ------------------------------------------------------------------

        private static void ComposeAppIcon()
        {
            const int size = 512;
            var pixels = NewPixels(size, size);

            // Campus Sky rounded tile.
            FillRoundedRect(pixels, size, size, 0, 0, size, size, 96, CampusSky);

            // Sun, top-left: paper ring + Path Gold core.
            FillEllipse(pixels, size, size, 116, 396, 54, 54, new Color(1f, 1f, 1f, 0.55f));
            FillEllipse(pixels, size, size, 116, 396, 44, 44, PathGold);

            // Grass band with a lighter top lip (clipped by the tile corners
            // via rounded re-fill at the bottom).
            FillRoundedRect(pixels, size, size, 0, 0, size, 150, 96, CampusGrass);
            FillRect(pixels, size, size, 48, 138, size - 96, 12, Color.Lerp(CampusGrass, Color.white, 0.28f));

            // Path Gold walk path from the door to the tile edge.
            FillEllipse(pixels, size, size, 256, 96, 110, 44, PathGold);

            // Soft shadow grounding the studio.
            FillEllipse(pixels, size, size, 262, 128, 158, 30, SoftShadow);

            // Design Build studio: Creative Coral body, paper roof band,
            // glass windows, gold door — chunky toy silhouette.
            FillRoundedRect(pixels, size, size, 136, 128, 240, 220, 22, Color.Lerp(CreativeCoral, Ink, 0.18f)); // body shade edge
            FillRoundedRect(pixels, size, size, 142, 134, 228, 214, 20, CreativeCoral);

            // Roof: paper fascia + teal cap.
            FillRoundedRect(pixels, size, size, 124, 330, 264, 44, 14, Paper);
            FillRoundedRect(pixels, size, size, 124, 358, 264, 16, 8, PaperShadow);
            FillRoundedRect(pixels, size, size, 160, 374, 192, 30, 12, WorkshopTeal);

            // Flag pole + Path Gold pennant.
            FillRect(pixels, size, size, 252, 404, 8, 56, Ink);
            FillTriangleRight(pixels, size, size, 260, 436, 54, 26, PathGold);

            // Windows: paper frames + glass, with a shine line.
            foreach (var windowX in new[] { 170, 296 })
            {
                FillRoundedRect(pixels, size, size, windowX, 240, 46, 56, 10, Paper);
                FillRoundedRect(pixels, size, size, windowX + 5, 245, 36, 46, 8, Glass);
                FillRect(pixels, size, size, windowX + 9, 276, 28, 7, new Color(1f, 1f, 1f, 0.6f));
            }

            // Door: gold with a paper arch + ink handle.
            FillRoundedRect(pixels, size, size, 226, 134, 60, 84, 14, Paper);
            FillRoundedRect(pixels, size, size, 231, 134, 50, 76, 12, PathGold);
            FillEllipse(pixels, size, size, 270, 172, 5, 5, Ink);

            // Sticker sheen, top-left of the tile (handmade-toy finish).
            FillEllipse(pixels, size, size, 170, 452, 60, 22, new Color(1f, 1f, 1f, 0.22f));

            var texture = ToTexture(pixels, size, size);
            try
            {
                WritePng(texture, IconAssetPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        // ------------------------------------------------------------------
        // Primitive fill helpers (house style — see CareerQuestOptionalArtBuilder)
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
                    var insideX = Mathf.Clamp(localX, radius, rectWidth - radius - 1);
                    var insideY = Mathf.Clamp(localY, radius, rectHeight - radius - 1);
                    var dx = localX - insideX;
                    var dy = localY - insideY;
                    if (dx * dx + dy * dy > radius * radius)
                    {
                        continue;
                    }

                    pixels[py * width + px] = Blend(pixels[py * width + px], color);
                }
            }
        }

        private static void FillEllipse(Color[] pixels, int width, int height, int centerX, int centerY, int radiusX, int radiusY, Color color)
        {
            var xMin = Mathf.Clamp(centerX - radiusX, 0, width);
            var xMax = Mathf.Clamp(centerX + radiusX + 1, 0, width);
            var yMin = Mathf.Clamp(centerY - radiusY, 0, height);
            var yMax = Mathf.Clamp(centerY + radiusY + 1, 0, height);

            for (var py = yMin; py < yMax; py++)
            {
                for (var px = xMin; px < xMax; px++)
                {
                    var nx = (px - centerX) / (float)radiusX;
                    var ny = (py - centerY) / (float)radiusY;
                    if (nx * nx + ny * ny > 1f)
                    {
                        continue;
                    }

                    pixels[py * width + px] = Blend(pixels[py * width + px], color);
                }
            }
        }

        /// <summary>Right-pointing pennant: full height at x, tapering to a point at x+length.</summary>
        private static void FillTriangleRight(Color[] pixels, int width, int height, int x, int y, int length, int halfHeight, Color color)
        {
            for (var px = Mathf.Clamp(x, 0, width - 1); px < Mathf.Clamp(x + length, 0, width); px++)
            {
                var t = (px - x) / (float)length;
                var span = Mathf.RoundToInt(halfHeight * (1f - t));
                for (var py = Mathf.Clamp(y - span, 0, height - 1); py <= Mathf.Clamp(y + span, 0, height - 1); py++)
                {
                    pixels[py * width + px] = Blend(pixels[py * width + px], color);
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

            var color = (foreground * foreground.a + background * background.a * (1f - foreground.a)) / alpha;
            color.a = alpha;
            return color;
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

        private static void WritePng(Texture2D texture, string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, texture.EncodeToPNG());
        }

        private static void ConfigureIconImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                return;
            }

            // Sprite/Single so the same asset serves the PlayerSettings icon
            // (Texture2D) AND the splash logo (Sprite).
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
