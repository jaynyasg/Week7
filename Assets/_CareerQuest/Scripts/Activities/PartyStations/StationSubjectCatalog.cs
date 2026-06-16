using System.Collections.Generic;

namespace CareerQuest
{
    /// <summary>
    /// The drawn silhouette for a station's scene subject — the character the
    /// seed copy talks about (the dragon, the sleepy robot, the guest). Kept to
    /// a small reusable set; <see cref="StationSubjectView"/> composes each from
    /// shape primitives so no baked art / catalog entry is needed.
    /// </summary>
    public enum StationSubjectKind
    {
        Dragon,
        Critter,
        Robot,
        Cloud,
        Blob,
        Person
    }

    /// <summary>One station scene subject: a kid-facing name and a drawn kind.</summary>
    public readonly struct StationSubject
    {
        public StationSubject(StationSubjectKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public StationSubjectKind Kind { get; }
        public string Name { get; }
    }

    /// <summary>
    /// Design-review (2026-06-16): every party-station seed names a subject in
    /// its intro/success copy ("the hiccuping dragon", "the sleepy robot", "a
    /// hungry guest"), but the playfield only ever drew the toys — so the named
    /// character was missing from the screen. This catalog maps each seed to a
    /// drawn subject so <see cref="PartyStationController"/> can render it above
    /// the toys. Keyed by seed id (the subject differs per seed: the vet clinic
    /// is a dragon by default and a space hamster on the remix). The validator
    /// asserts this map covers every seed (<see cref="MissingSeedIds"/>).
    /// </summary>
    public static class StationSubjectCatalog
    {
        private static readonly Dictionary<string, StationSubject> Subjects = new()
        {
            // Robotics Rescue — the robot being rebuilt and launched home.
            ["robotics_garage.lunchbox_rescue"] = new(StationSubjectKind.Robot, "Rover the Lunchbox Bot"),
            ["robotics_garage.moon_cart"] = new(StationSubjectKind.Robot, "the Moon Cart"),
            // AI Lab Sort — the bubbles / signals being sorted.
            ["ai_lab.bubblegum_garden"] = new(StationSubjectKind.Blob, "the Bubble Bunch"),
            ["ai_lab.sock_satellite"] = new(StationSubjectKind.Critter, "the Sock Satellite"),
            // Community Kitchen — the guest you serve.
            ["community_kitchen.chef_detective"] = new(StationSubjectKind.Person, "a Hungry Guest"),
            ["community_kitchen.tiny_planet_picnic"] = new(StationSubjectKind.Person, "a Tiny Planet Pal"),
            // Music Remix — the parade cloud / the sleepy robot.
            ["music_studio.thunderstorm_parade"] = new(StationSubjectKind.Cloud, "the Thunder Cloud"),
            ["music_studio.robot_lullaby"] = new(StationSubjectKind.Robot, "the Sleepy Robot"),
            // Vet Clinic — the patient (user-reported: "help the dragon").
            ["vet_clinic.dragon_hiccups"] = new(StationSubjectKind.Dragon, "Hiccup the Dragon"),
            ["vet_clinic.space_hamster"] = new(StationSubjectKind.Critter, "the Space Hamster"),
            // Game Studio — the sidekick you build a quest for / the button boss.
            ["game_studio.sidekick_quest"] = new(StationSubjectKind.Critter, "your Sidekick"),
            ["game_studio.button_boss"] = new(StationSubjectKind.Robot, "the Button Boss"),
            // Weather Lab — the parade / town you shelter from the storm.
            ["weather_lab.thunder_parade"] = new(StationSubjectKind.Cloud, "the Parade Cloud"),
            ["weather_lab.bubblegum_flood"] = new(StationSubjectKind.Cloud, "the Bubblegum Cloud"),
            // Spaceport — the probe / mail bot you fly.
            ["spaceport.snack_probe"] = new(StationSubjectKind.Robot, "the Snack Probe"),
            ["spaceport.moon_mail"] = new(StationSubjectKind.Robot, "the Moon Mail Bot"),
            // Newsroom — the person at the heart of the story.
            ["newsroom.mystery_mural"] = new(StationSubjectKind.Person, "the Mural Maker"),
            ["newsroom.invention_scoop"] = new(StationSubjectKind.Person, "the Inventor"),
            // Green City — the neighbor you build for.
            ["green_city.solar_sandwich"] = new(StationSubjectKind.Person, "a City Neighbor"),
            ["green_city.windy_rooftop"] = new(StationSubjectKind.Person, "a Rooftop Neighbor")
        };

        /// <summary>The scene subject for a seed, if one is mapped.</summary>
        public static bool TryGet(string seedId, out StationSubject subject)
        {
            if (!string.IsNullOrEmpty(seedId))
            {
                return Subjects.TryGetValue(seedId, out subject);
            }

            subject = default;
            return false;
        }

        /// <summary>
        /// Validation seam: seed ids the catalog does NOT cover. Empty when every
        /// authored seed has a drawn subject.
        /// </summary>
        public static IReadOnlyList<string> MissingSeedIds()
        {
            var missing = new List<string>();
            foreach (var station in PartyStationDefinitions.All)
            {
                if (station?.Seeds == null)
                {
                    continue;
                }

                foreach (var seed in station.Seeds)
                {
                    if (seed != null && !Subjects.ContainsKey(seed.SeedId))
                    {
                        missing.Add(seed.SeedId);
                    }
                }
            }

            return missing;
        }
    }
}

