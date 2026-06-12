using System;
using UnityEditor;
using UnityEngine;

namespace CareerQuest.Editor
{
    /// <summary>
    /// Applies the Career Quest sprite import settings to every texture under the
    /// CareerQuest art paths so imported packs and authored art never need manual
    /// inspector edits. Mirrors CareerQuestSpriteKitGenerator.ConfigureTextureImporter.
    /// </summary>
    public sealed class CareerQuestTexturePostprocessor : AssetPostprocessor
    {
        private static readonly string[] ManagedPathPrefixes =
        {
            "Assets/_CareerQuest/Art/",
            "Assets/Resources/CareerQuest/"
        };

        private void OnPreprocessTexture()
        {
            if (!IsManagedPath(assetPath))
            {
                return;
            }

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
        }

        private static bool IsManagedPath(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            var normalized = path.Replace('\\', '/');
            foreach (var prefix in ManagedPathPrefixes)
            {
                if (normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
