using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace CareerQuest
{
    /// <summary>
    /// Typography role per DESIGN.md: Display (Fredoka) for titles, room names, and big quest
    /// moments; Body (Lexend) for kid-facing instructions, HUD text, and button labels.
    /// </summary>
    public enum TypeRole
    {
        Display,
        Body
    }

    public enum TypeWeight
    {
        Regular,
        Medium,
        SemiBold,
        Bold
    }

    /// <summary>
    /// DESIGN.md type scale and TMP font asset resolution for Career Quest Campus.
    /// Font assets are baked by CareerQuestTmpSetup.BakeFonts into Resources.
    /// </summary>
    public static class TypeStyles
    {
        // Concrete sizes chosen inside the DESIGN.md ranges.
        public const int HeroTitle = 52;    // Hero title: 48-56 px
        public const int ScreenTitle = 38;  // Screen title: 36-42 px
        public const int RoomPrompt = 26;   // Room prompt: 24-30 px
        public const int ButtonLabel = 24;  // Button label: 22-28 px
        public const int Body = 18;         // HUD/body: 16-20 px
        public const int SmallLabel = 13;   // Small labels: 12-15 px

        public const string DisplayFamily = "Fredoka";
        public const string BodyFamily = "Lexend";
        public const string FontsResourceFolder = "CareerQuest/Fonts";

        private static readonly Dictionary<string, TMP_FontAsset> Cache = new();

        public static string ResourcePathFor(TypeRole role, TypeWeight weight)
        {
            var family = role == TypeRole.Display ? DisplayFamily : BodyFamily;
            return $"{FontsResourceFolder}/{family}-{weight} SDF";
        }

        /// <summary>
        /// Resolves a (role, weight) pair to its baked TMP font asset. Falls back to the
        /// TMP default font when the baked asset is missing so UI still renders.
        /// </summary>
        public static TMP_FontAsset Resolve(TypeRole role, TypeWeight weight)
        {
            var path = ResourcePathFor(role, weight);
            if (Cache.TryGetValue(path, out var cached) && cached != null)
            {
                return cached;
            }

            var font = Resources.Load<TMP_FontAsset>(path);
            if (font == null)
            {
                font = TMP_Settings.defaultFontAsset;
            }

            Cache[path] = font;
            return font;
        }
    }
}
