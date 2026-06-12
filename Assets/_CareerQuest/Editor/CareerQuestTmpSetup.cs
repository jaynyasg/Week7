using System;
using System.IO;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace CareerQuest.Editor
{
    /// <summary>
    /// Headless TMP bootstrap for Career Quest (U2 typography unit).
    /// Run order: ImportEssentials first (one-time), then BakeFonts. Both exit the editor.
    /// </summary>
    public static class CareerQuestTmpSetup
    {
        private const string TmpSettingsAssetPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";
        private const string UguiPackageJsonPath = "Packages/com.unity.ugui/package.json";
        private const string EssentialsPackageRelativePath = "Package Resources/TMP Essential Resources.unitypackage";
        private const string FontOutputFolder = "Assets/Resources/CareerQuest/Fonts";

        private static readonly (string TtfPath, string AssetName)[] FontBakeJobs =
        {
            ("Assets/Fonts/Fredoka/Fredoka-Regular.ttf", "Fredoka-Regular"),
            ("Assets/Fonts/Fredoka/Fredoka-Medium.ttf", "Fredoka-Medium"),
            ("Assets/Fonts/Fredoka/Fredoka-SemiBold.ttf", "Fredoka-SemiBold"),
            ("Assets/Fonts/Fredoka/Fredoka-Bold.ttf", "Fredoka-Bold"),
            ("Assets/Fonts/Lexend/Lexend-Regular.ttf", "Lexend-Regular"),
            ("Assets/Fonts/Lexend/Lexend-Medium.ttf", "Lexend-Medium"),
            ("Assets/Fonts/Lexend/Lexend-SemiBold.ttf", "Lexend-SemiBold"),
            ("Assets/Fonts/Lexend/Lexend-Bold.ttf", "Lexend-Bold")
        };

        private static int _importPollTicks;

        [MenuItem("Career Quest/TMP/Import Essential Resources")]
        public static void ImportEssentialsInteractive()
        {
            ImportEssentialsCore(exitWhenDone: false);
        }

        [MenuItem("Career Quest/TMP/Bake Font Assets")]
        public static void BakeFontsInteractive()
        {
            BakeFontsCore(exitWhenDone: false);
        }

        /// <summary>Headless entry point: imports TMP Essential Resources, then exits 0/1.</summary>
        public static void ImportEssentials()
        {
            ImportEssentialsCore(exitWhenDone: true);
        }

        /// <summary>Headless entry point: bakes the 8 static SDF font assets, then exits 0/1.</summary>
        public static void BakeFonts()
        {
            BakeFontsCore(exitWhenDone: true);
        }

        private static void ImportEssentialsCore(bool exitWhenDone)
        {
            try
            {
                if (EssentialsImported())
                {
                    Debug.Log("CQ_TMP_SETUP ImportEssentials: TMP Settings already present, nothing to do.");
                    ExitIfHeadless(exitWhenDone, 0);
                    return;
                }

                var packagePath = ResolveEssentialsPackagePath();
                if (string.IsNullOrEmpty(packagePath) || !File.Exists(packagePath))
                {
                    Debug.LogError($"CQ_TMP_SETUP ImportEssentials: could not locate TMP Essential Resources unitypackage (looked under {UguiPackageJsonPath}).");
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                Debug.Log($"CQ_TMP_SETUP ImportEssentials: importing '{packagePath}'.");
                AssetDatabase.ImportPackage(packagePath, false);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                if (EssentialsImported())
                {
                    Debug.Log("CQ_TMP_SETUP ImportEssentials: import complete.");
                    ExitIfHeadless(exitWhenDone, 0);
                    return;
                }

                if (!exitWhenDone)
                {
                    Debug.LogWarning("CQ_TMP_SETUP ImportEssentials: import still pending; re-check after the editor finishes importing.");
                    return;
                }

                // Import ran asynchronously; poll the asset database from the editor update loop.
                _importPollTicks = 0;
                EditorApplication.update += PollEssentialsImport;
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_TMP_SETUP ImportEssentials failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        private static void PollEssentialsImport()
        {
            _importPollTicks++;
            if (EssentialsImported())
            {
                EditorApplication.update -= PollEssentialsImport;
                Debug.Log($"CQ_TMP_SETUP ImportEssentials: import completed after {_importPollTicks} editor ticks.");
                EditorApplication.Exit(0);
                return;
            }

            if (_importPollTicks % 100 == 0)
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            }

            if (_importPollTicks > 3000)
            {
                EditorApplication.update -= PollEssentialsImport;
                Debug.LogError("CQ_TMP_SETUP ImportEssentials: timed out waiting for TMP Settings asset to appear.");
                EditorApplication.Exit(1);
            }
        }

        private static void BakeFontsCore(bool exitWhenDone)
        {
            try
            {
                if (!EssentialsImported())
                {
                    Debug.LogError("CQ_TMP_SETUP BakeFonts: TMP Essential Resources are not imported. Run ImportEssentials first.");
                    ExitIfHeadless(exitWhenDone, 1);
                    return;
                }

                EnsureFolder(FontOutputFolder);

                var characterSet = BuildCharacterSet();
                var bakedCount = 0;
                var skippedCount = 0;

                foreach (var (ttfPath, assetName) in FontBakeJobs)
                {
                    if (BakeOne(ttfPath, assetName, characterSet))
                    {
                        bakedCount++;
                    }
                    else
                    {
                        skippedCount++;
                    }
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"CQ_TMP_SETUP BakeFonts: complete. baked={bakedCount} skipped(existing)={skippedCount}.");
                ExitIfHeadless(exitWhenDone, 0);
            }
            catch (Exception exception)
            {
                Debug.LogError($"CQ_TMP_SETUP BakeFonts failed: {exception}");
                ExitIfHeadless(exitWhenDone, 1);
            }
        }

        /// <returns>True when a new asset was baked, false when an existing asset was kept (idempotent skip).</returns>
        private static bool BakeOne(string ttfPath, string assetName, string characterSet)
        {
            var outputPath = $"{FontOutputFolder}/{assetName} SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(outputPath);
            if (existing != null)
            {
                Debug.Log($"CQ_TMP_SETUP BakeFonts: '{outputPath}' already exists, skipping.");
                return false;
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException($"Source font missing at '{ttfPath}'.");
            }

            // Create as Dynamic so TryAddCharacters can rasterize, then freeze to Static below.
            // Sampling 72 / padding 9 / 1024x1024 fits ASCII + Latin-1 printable; multi-atlas guards overflow.
            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                72,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                true);

            if (fontAsset == null)
            {
                throw new InvalidOperationException($"TMP_FontAsset.CreateFontAsset returned null for '{ttfPath}'.");
            }

            fontAsset.name = $"{assetName} SDF";

            if (!fontAsset.TryAddCharacters(characterSet, out var missingCharacters) &&
                !string.IsNullOrEmpty(missingCharacters))
            {
                Debug.LogWarning($"CQ_TMP_SETUP BakeFonts: '{assetName}' missing glyphs for: {missingCharacters}");
            }

            AssetDatabase.CreateAsset(fontAsset, outputPath);

            for (var i = 0; i < fontAsset.atlasTextures.Length; i++)
            {
                var texture = fontAsset.atlasTextures[i];
                if (texture == null)
                {
                    continue;
                }

                texture.name = i == 0 ? $"{assetName} Atlas" : $"{assetName} Atlas {i}";
                AssetDatabase.AddObjectToAsset(texture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                fontAsset.material.name = $"{assetName} Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            // Freeze the baked atlas: static assets never rasterize at runtime and drop the source font reference.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            EditorUtility.SetDirty(fontAsset);
            Debug.Log($"CQ_TMP_SETUP BakeFonts: baked '{outputPath}' ({fontAsset.characterTable.Count} characters, {fontAsset.atlasTextures.Length} atlas texture(s)).");
            return true;
        }

        private static string BuildCharacterSet()
        {
            var builder = new StringBuilder(220);

            // ASCII printable.
            for (var code = 0x20; code <= 0x7E; code++)
            {
                builder.Append((char)code);
            }

            // Latin-1 printable.
            for (var code = 0xA0; code <= 0xFF; code++)
            {
                builder.Append((char)code);
            }

            // Punctuation used in game copy (dashes, curly quotes, ellipsis, bullet).
            builder.Append('–').Append('—')
                .Append('‘').Append('’')
                .Append('“').Append('”')
                .Append('…').Append('•');

            return builder.ToString();
        }

        private static bool EssentialsImported()
        {
            if (File.Exists(TmpSettingsAssetPath))
            {
                return true;
            }

            return AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsAssetPath) != null;
        }

        private static string ResolveEssentialsPackagePath()
        {
            var packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssetPath(UguiPackageJsonPath);
            if (packageInfo == null || string.IsNullOrEmpty(packageInfo.resolvedPath))
            {
                return null;
            }

            var direct = Path.Combine(packageInfo.resolvedPath, EssentialsPackageRelativePath);
            if (File.Exists(direct))
            {
                return direct;
            }

            // Layout changed? Search the resolved package directory for the essentials package.
            foreach (var candidate in Directory.GetFiles(packageInfo.resolvedPath, "*.unitypackage", SearchOption.AllDirectories))
            {
                if (Path.GetFileName(candidate).IndexOf("Essential", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return candidate;
                }
            }

            return null;
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
