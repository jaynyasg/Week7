using System;

namespace CareerQuest
{
    /// <summary>
    /// U8 single source of truth for every audio cue ID the game plays. Call
    /// sites reference these constants (never raw strings) and the EditMode
    /// coverage test walks <see cref="All"/> asserting each ID resolves to a
    /// clip under Resources/Audio — so a cue added here without a curated clip
    /// fails the suite instead of silently no-op'ing forever.
    ///
    /// Clip files are curated from the Kenney CC0 packs by
    /// CareerQuestAudioCurator (editor utility) into Assets/Resources/Audio/.
    /// </summary>
    public static class AudioCueIds
    {
        // ---- UI tier ----
        public const string UiPress = "ui_press";

        // ---- Gameplay tier: drag framework (U6) ----
        public const string DragPickup = "drag_pickup";
        public const string DropAccept = "drop_accept";
        public const string DropReject = "drop_reject";

        // ---- Gameplay tier: ceremony / badges ----
        public const string BadgeStamp = "badge_stamp";
        public const string CeremonyDesignBuildSuccess = "ceremony_design_build_success";
        public const string CeremonyDesignBuildPractice = "ceremony_design_build_practice";
        public const string CeremonyHealthHeroSuccess = "ceremony_health_hero_success";
        public const string CeremonyHealthHeroPractice = "ceremony_health_hero_practice";
        public const string CeremonyLogicCourtSuccess = "ceremony_logic_court_success";
        public const string CeremonyLogicCourtPractice = "ceremony_logic_court_practice";

        // ---- Gameplay tier: reveal cinematic beats (U7) ----
        public const string RevealToken = "reveal_token";
        public const string RevealSweep = "reveal_sweep";
        public const string RevealUnlock = "reveal_unlock";

        // ---- Gameplay tier: world / hub ----
        public const string DoorEnter = "door_enter";
        public const string RoomWipe = "room_wipe";
        public const string Footstep = "footstep";

        /// <summary>U12-ready: synced one-button emote pop (P16).</summary>
        public const string EmotePop = "emote_pop";

        /// <summary>P19: campus-evolution city piece arrival fanfare.</summary>
        public const string CityPiecePop = "city_piece_pop";

        // ---- Ambient/music tier (looping; P4) ----
        public const string AmbientCampus = "ambient_campus";
        public const string AmbientDesignBuild = "ambient_design_build";
        public const string AmbientHealthHero = "ambient_health_hero";
        public const string AmbientLogicCourt = "ambient_logic_court";
        public const string AmbientGallery = "ambient_gallery";
        public const string AmbientReveal = "ambient_reveal";
        public const string AmbientOptional = "ambient_optional";
        public const string MusicCampus = "music_campus";

        /// <summary>
        /// The ceremony cue convention (`ceremony_{activityId}_{success|practice}`).
        /// FeedbackController generates its cue through here so the template can
        /// never drift from the registry constants above.
        /// </summary>
        public static string CeremonyCue(string activityId, bool success)
        {
            return $"ceremony_{activityId}_{(success ? "success" : "practice")}";
        }

        /// <summary>Loop cues (ambient_*/music_*) get streaming import settings.</summary>
        public static bool IsLoopCue(string cueId)
        {
            return !string.IsNullOrEmpty(cueId)
                && (cueId.StartsWith("ambient_", StringComparison.Ordinal)
                    || cueId.StartsWith("music_", StringComparison.Ordinal));
        }

        /// <summary>Every cue ID referenced anywhere in code — the coverage contract.</summary>
        public static readonly string[] All =
        {
            UiPress,
            DragPickup,
            DropAccept,
            DropReject,
            BadgeStamp,
            CeremonyDesignBuildSuccess,
            CeremonyDesignBuildPractice,
            CeremonyHealthHeroSuccess,
            CeremonyHealthHeroPractice,
            CeremonyLogicCourtSuccess,
            CeremonyLogicCourtPractice,
            RevealToken,
            RevealSweep,
            RevealUnlock,
            DoorEnter,
            RoomWipe,
            Footstep,
            EmotePop,
            CityPiecePop,
            AmbientCampus,
            AmbientDesignBuild,
            AmbientHealthHero,
            AmbientLogicCourt,
            AmbientGallery,
            AmbientReveal,
            AmbientOptional,
            MusicCampus
        };
    }
}
