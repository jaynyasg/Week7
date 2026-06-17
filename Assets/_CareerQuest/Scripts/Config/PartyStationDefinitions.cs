using System.Collections.Generic;
using System.Linq;

namespace CareerQuest
{
    /// <summary>
    /// Static catalog of all 10 Party Pack stations (U1, design doc: Station
    /// Seed Bible). Content is authored from docs/designs/party-campus-pack.md
    /// and validated by PartyStationValidator: every station has one default
    /// and one alternate seed, 4-6 active interactables per seed with at least
    /// 4 task/clue-chain objects, a tiny guide identity, and the full
    /// intro/hint/escalation/success/reward-preview/result-summary/NPC-reaction
    /// copy surface. Object sprite keys use the intentional
    /// "prop.party.{stationId}.{objectId}" placeholder convention until the
    /// U4/U5 renderer art pass.
    /// </summary>
    public static class PartyStationDefinitions
    {
        private static readonly PartyStationDefinition[] Stations =
        {
            RoboticsRescue(),
            AiLabSort(),
            CommunityKitchenMatch(),
            MusicRemix(),
            VetClinicDiagnose(),
            GameStudioCompose(),
            WeatherLabRescue(),
            SpaceportPilot(),
            NewsroomStorySprint(),
            GreenCityBuilder()
        };

        public static IReadOnlyList<PartyStationDefinition> All => Stations;

        public static bool TryGetById(string stationId, out PartyStationDefinition definition)
        {
            definition = Stations.FirstOrDefault(candidate => candidate.Id == stationId);
            return definition != null;
        }

        public static PartyStationDefinition GetById(string stationId)
        {
            return Stations.First(candidate => candidate.Id == stationId);
        }

        private static PartyStationDefinition RoboticsRescue()
        {
            var id = CareerQuestCatalog.RoboticsGarageId;
            return new PartyStationDefinition(
                id,
                "Robotics Rescue",
                new[] { "launch", "rescue" },
                ToyPatternId.ShootTarget,
                "Bolt the Bench Buddy",
                "upbeat build coach",
                "The lunchbox robot is rebuilt — launch each part to the rescue spot!",
                new[]
                {
                    Obj(id, "battery_toast", "Battery Toast", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                    Obj(id, "wheel_sandwich", "Wheel Sandwich", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                    Obj(id, "sensor_sticker", "Sensor Sticker", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Reasoning"),
                    Obj(id, "route_cards", "Route Beacon", PartyStationObjectRole.Clue, "rescue_flag", "react.glow", "Reasoning"),
                    Obj(id, "rescue_flag", "Rescue Flag", PartyStationObjectRole.Reaction, "route_cards", "react.cheer")
                },
                "Pull back and launch each robot part to land it on the rescue spot.",
                new[]
                {
                    new TraitDelta("Building", 5),
                    new TraitDelta("Reasoning", 4),
                    new TraitDelta("Collaboration", 3)
                },
                "accessory.tool_belt",
                new[] { "robotics_engineer", "ai_engineer", "inventor", "mechanic" },
                "badge.robotics_garage",
                "campus.robotics_garage",
                "prop.city_piece_garage",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.lunchbox_rescue",
                        "Lunchbox Robot Rescue",
                        true,
                        "",
                        null,
                        "Pull back and launch each robot part to land it on the rescue spot.",
                        "Bolt beeps: the robot is built — launch every part to the rescue spot!",
                        "Pull a part back from the pad, then let go to launch it.",
                        "Aim for the glowing rescue spot, then release to fling it home.",
                        "Beep-beep! Every part landed — the rescue rover rolls out!",
                        "Finish the rescue to earn the Tool Belt!",
                        "You launched every part onto the rescue spot and sent the rescue rover. You practiced Building + Reasoning. New gear: Tool Belt.",
                        "The rescued rover spins a happy little victory circle."),
                    new PartyStationSeedDefinition(
                        $"{id}.moon_cart",
                        "Moon Cart Launch",
                        false,
                        "The moon cart parts scattered! Launch each one onto the rescue spot to rebuild it.",
                        new[]
                        {
                            Obj(id, "moon_wheel", "Moon Wheel", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                            Obj(id, "flashlight_eye", "Flashlight Eye", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Building"),
                            Obj(id, "antenna_straw", "Antenna Straw", PartyStationObjectRole.CoreTask, "", "react.pop", "Focus"),
                            Obj(id, "fuel_snack_pack", "Fuel Snack Pack", PartyStationObjectRole.Helper, "moon_wheel", "react.bounce"),
                            Obj(id, "crater_map", "Crater Beacon", PartyStationObjectRole.Clue, "antenna_straw", "react.glow", "Reasoning")
                        },
                        "Pull back and launch each moon-cart part onto the rescue spot.",
                        "Bolt waves: launch the moon-cart parts onto the rescue spot to rebuild it!",
                        "Pull a part back from the pad, then let go to launch it.",
                        "Aim for the glowing rescue spot, then release to fling it home.",
                        "Every part landed — the moon cart hums back to life!",
                        "Tune up the cart to earn the Tool Belt!",
                        "You launched every moon-cart part onto the rescue spot. You practiced Building + Focus. New gear: Tool Belt.",
                        "The moon cart flashes its flashlight eye in thanks.")
                });
        }

        private static PartyStationDefinition AiLabSort()
        {
            var id = CareerQuestCatalog.AiLabId;
            return new PartyStationDefinition(
                id,
                "AI Lab Sort",
                new[] { "deduce", "test" },
                ToyPatternId.DeduceAnswer,
                "Pixel the Pattern Pal",
                "curious data helper",
                "Read the clue, then cross out the sort rules that get it wrong.",
                new[]
                {
                    // DeduceAnswer: the wrong sort rules are the false candidates
                    // (CoreTask) crossed out by elimination; the right rule is the
                    // Clue answer that survives. Clue: the bubbles sort by color.
                    Obj(id, "size_rule", "Size Rule", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                    Obj(id, "loud_rule", "Loud Rule", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                    Obj(id, "random_rule", "Random Rule", PartyStationObjectRole.CoreTask, "", "react.wobble", "Reasoning"),
                    Obj(id, "shape_rule", "Shape Rule", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                    Obj(id, "color_rule", "Color Rule", PartyStationObjectRole.Clue, "", "react.glow", "Science"),
                    Obj(id, "test_button", "Test Button", PartyStationObjectRole.Reaction, "color_rule", "react.sparkle")
                },
                "Cross out the rules that missort until the right sort rule survives.",
                new[]
                {
                    new TraitDelta("Reasoning", 5),
                    new TraitDelta("Science", 4),
                    new TraitDelta("Building", 3)
                },
                "accessory.lab_goggles",
                new[] { "ai_engineer", "data_scientist", "scientist", "game_designer" },
                "badge.ai_lab",
                "campus.space_lab",
                "prop.city_piece_lab",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.bubblegum_garden",
                        "Bubblegum Data Garden",
                        true,
                        "",
                        null,
                        "Cross out the rules that missort until the right sort rule survives.",
                        "Pixel grins: the bubbles sort by color — which rule gets it right?",
                        "Cross out a rule that sorts them wrong.",
                        "Size and loudness don't match the colors — rule those out.",
                        "The sorter learned it! The color rule sorts every bubble right.",
                        "Train the sorter to earn the Lab Goggles!",
                        "You crossed out the wrong rules and kept the color sort rule. You practiced Reasoning + Science. New gear: Lab Goggles.",
                        "Pixel does a proud little data dance."),
                    new PartyStationSeedDefinition(
                        $"{id}.sock_satellite",
                        "Sock Satellite Classifier",
                        false,
                        "Read the signal clue, then cross out the rules that mislabel it.",
                        new[]
                        {
                            // Cross out the rules that mislabel static as signal;
                            // the stripe rule (Clue answer) is the only one that
                            // spots the true signals.
                            Obj(id, "speed_rule", "Speed Rule", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                            Obj(id, "guess_rule", "Guess Rule", PartyStationObjectRole.CoreTask, "", "react.wobble", "Reasoning"),
                            Obj(id, "shape_rule", "Shape Rule", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                            Obj(id, "loud_rule", "Loud Rule", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                            Obj(id, "stripe_rule", "Stripe Rule", PartyStationObjectRole.Clue, "", "react.glow", "Focus"),
                            Obj(id, "launch_check", "Launch Check", PartyStationObjectRole.Reaction, "stripe_rule", "react.cheer")
                        },
                        "Cross out the rules that mislabel signals until the stripe rule survives.",
                        "Pixel whispers: clear signals have stripes — which rule spots them?",
                        "Cross out a rule that labels static as signal.",
                        "Speed and guessing won't find the stripes — rule those out.",
                        "Clean signals locked in — the stripe rule sorts them right!",
                        "Clean up the signals to earn the Lab Goggles!",
                        "You crossed out the wrong rules and kept the stripe sort rule. You practiced Reasoning + Focus. New gear: Lab Goggles.",
                        "The launch check light blinks a cheerful green.")
                });
        }

        private static PartyStationDefinition CommunityKitchenMatch()
        {
            var id = CareerQuestCatalog.CommunityKitchenId;
            return new PartyStationDefinition(
                id,
                "Community Kitchen Pour",
                new[] { "pour", "serve" },
                ToyPatternId.PourToLine,
                "Chef Sunny",
                "warm service coach",
                "Pour each ingredient to the line, then serve a bowl every guest can enjoy.",
                new[]
                {
                    Obj(id, "water_pour", "Water Pour", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                    Obj(id, "broth_pour", "Broth Pour", PartyStationObjectRole.CoreTask, "", "react.bounce", "Helping"),
                    Obj(id, "veggie_pour", "Veggie Pour", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                    Obj(id, "milk_pour", "Milk Pour", PartyStationObjectRole.CoreTask, "", "react.glow", "Helping"),
                    Obj(id, "kindness_swap", "Kindness Swap", PartyStationObjectRole.Helper, "water_pour", "react.cheer", "Collaboration")
                },
                "Pour each ingredient to the line, then make one kindness swap.",
                new[]
                {
                    new TraitDelta("Helping", 5),
                    new TraitDelta("Collaboration", 4),
                    new TraitDelta("Creativity", 3)
                },
                "accessory.chef_hat",
                new[] { "chef", "community_organizer", "doctor" },
                "badge.community_kitchen",
                "campus.community_kitchen",
                "prop.city_piece_kitchen",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.chef_detective",
                        "Chef Detective Soup",
                        true,
                        "",
                        null,
                        "Pour each ingredient to the line, then make one kindness swap.",
                        "Chef Sunny smiles: this soup needs each cup poured just to the line!",
                        "Hold a cup to pour it, and stop right at the line.",
                        "Each cup glows when it reaches the line.",
                        "What a bowl! Every guest gets a soup they can enjoy.",
                        "Serve the soup to earn the Chef Hat!",
                        "You poured every cup to the line and served a guest-friendly bowl. You practiced Helping plus Collaboration. New gear: Chef Hat.",
                        "A happy guest gives the soup two big thumbs up."),
                    new PartyStationSeedDefinition(
                        $"{id}.tiny_planet_picnic",
                        "Tiny Planet Picnic",
                        false,
                        "Pour each picnic drink to the line so every tiny planet neighbor gets a tasty cup.",
                        new[]
                        {
                            Obj(id, "juice_pour", "Juice Pour", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                            Obj(id, "soup_pour", "Soup Pour", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                            Obj(id, "snack_pour", "Snack Pour", PartyStationObjectRole.CoreTask, "", "react.bounce", "Helping"),
                            Obj(id, "cocoa_pour", "Cocoa Pour", PartyStationObjectRole.CoreTask, "", "react.glow", "Helping"),
                            Obj(id, "thank_you_stamp", "Thank-You Stamp", PartyStationObjectRole.Reaction, "juice_pour", "react.cheer")
                        },
                        "Pour each picnic drink to the line, then stamp the tray.",
                        "Chef Sunny giggles: tiny planet neighbors ordered a picnic!",
                        "Hold a cup to pour, and stop at the line.",
                        "Each cup glows when it fills to the line.",
                        "Every cup poured! The picnic is a tiny planet party.",
                        "Pack the picnic to earn the Chef Hat!",
                        "You poured the tiny planet picnic cups to the line with care. You practiced Helping plus Creativity. New gear: Chef Hat.",
                        "The neighbors stamp a thank-you on every tray.")
                });
        }

        private static PartyStationDefinition MusicRemix()
        {
            var id = CareerQuestCatalog.MusicStudioId;
            return new PartyStationDefinition(
                id,
                "Music Remix",
                new[] { "remix", "beat" },
                ToyPatternId.RhythmTap,
                "DJ Tempo",
                "rhythmic creative coach",
                "Layer the storm sounds into a parade beat and keep the tempo steady.",
                new[]
                {
                    Obj(id, "drum_cloud", "Drum Cloud", PartyStationObjectRole.CoreTask, "", "react.bounce", "Creativity"),
                    Obj(id, "rain_shaker", "Rain Shaker", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                    Obj(id, "horn_burst", "Horn Burst", PartyStationObjectRole.CoreTask, "", "react.pop", "Communication"),
                    Obj(id, "tempo_dial", "Tempo Dial", PartyStationObjectRole.Meter, "", "react.meter_shift", "Focus"),
                    Obj(id, "spotlight_button", "Spotlight Button", PartyStationObjectRole.Reaction, "drum_cloud", "react.cheer")
                },
                "Layer four sounds in the requested mood order, then lock the parade tempo.",
                new[]
                {
                    new TraitDelta("Creativity", 5),
                    new TraitDelta("Communication", 4),
                    new TraitDelta("Focus", 3)
                },
                "accessory.microphone",
                new[] { "musician", "artist", "teacher" },
                "badge.music_studio",
                "campus.music_studio",
                "prop.city_piece_art_tower",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.thunderstorm_parade",
                        "Thunderstorm Parade Beats",
                        true,
                        "",
                        null,
                        "Layer four sounds in the requested mood order, then lock the parade tempo.",
                        "DJ Tempo spins: the thunderstorm wants to march in the parade!",
                        "Start with the drum cloud, then stack the next sound on top.",
                        "The next sound in the mood order glows — drop it in.",
                        "That beat booms! The parade stomps right on tempo.",
                        "Lock the beat to earn the Microphone!",
                        "You mixed a thunderstorm parade beat and kept the tempo steady. You practiced Creativity + Focus. New gear: Microphone.",
                        "The drum cloud rumbles a happy encore."),
                    new PartyStationSeedDefinition(
                        $"{id}.robot_lullaby",
                        "Robot Lullaby Remix",
                        false,
                        "Soften every sound layer until the sleepy robot hums along.",
                        new[]
                        {
                            Obj(id, "beep_pad", "Beep Pad", PartyStationObjectRole.CoreTask, "", "react.pop", "Creativity"),
                            Obj(id, "hum_ribbon", "Hum Ribbon", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Communication"),
                            Obj(id, "soft_cymbal", "Soft Cymbal", PartyStationObjectRole.CoreTask, "", "react.bounce", "Creativity"),
                            Obj(id, "speed_dial", "Speed Dial", PartyStationObjectRole.Meter, "", "react.meter_shift", "Focus"),
                            Obj(id, "finish_button", "Finish Button", PartyStationObjectRole.Reaction, "speed_dial", "react.cheer")
                        },
                        "Soften the sound layers and set the tempo to match the calm cue.",
                        "DJ Tempo whispers: this little robot needs a gentle lullaby.",
                        "Quiet sounds first — let each layer hum softly.",
                        "Tap the speed dial until the calm cue glows green.",
                        "Shhh... the robot is humming along, cozy and calm.",
                        "Finish the lullaby to earn the Microphone!",
                        "You remixed a gentle robot lullaby and balanced the layers. You practiced Creativity + Communication. New gear: Microphone.",
                        "The sleepy robot blinks slow, happy lights.")
                });
        }

        private static PartyStationDefinition VetClinicDiagnose()
        {
            var id = CareerQuestCatalog.VetClinicId;
            return new PartyStationDefinition(
                id,
                "Vet Clinic Diagnose",
                new[] { "diagnose", "care" },
                ToyPatternId.MatchAndCare,
                "Nurse Nova",
                "calm care guide",
                "Read the care clues and pick a gentle plan for the hiccuping dragon.",
                new[]
                {
                    Obj(id, "symptom_cards", "Care Clue Cards", PartyStationObjectRole.Clue, "care_tool", "react.glow", "Science"),
                    Obj(id, "water_bowl", "Water Bowl", PartyStationObjectRole.Helper, "comfort_blanket", "react.bounce"),
                    Obj(id, "comfort_blanket", "Comfort Blanket", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Helping"),
                    Obj(id, "temperature_sticker", "Cozy Temp Sticker", PartyStationObjectRole.CoreTask, "", "react.pop", "Science"),
                    Obj(id, "care_tool", "Gentle Care Tool", PartyStationObjectRole.CoreTask, "", "react.cheer", "Helping")
                },
                "Match the care clues to a gentle plan, then pick the comfort item that fits.",
                new[]
                {
                    new TraitDelta("Helping", 5),
                    new TraitDelta("Science", 4),
                    new TraitDelta("Communication", 3)
                },
                "accessory.care_cape",
                new[] { "veterinarian", "doctor", "counselor", "marine_biologist" },
                "badge.vet_clinic",
                "campus.vet_clinic",
                "prop.city_piece_vet_clinic",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.dragon_hiccups",
                        "Dragon Hiccup Clinic",
                        true,
                        "",
                        null,
                        "Match the care clues to a gentle plan, then pick the comfort item that fits.",
                        "Nurse Nova smiles: this dragon has the hiccups — let's help gently.",
                        "Check the care clue cards to see what the dragon needs.",
                        "The right care tool glows when the clue matches.",
                        "Hic... hooray! The dragon feels comfy and calm again.",
                        "Care for the dragon to earn the Care Cape!",
                        "You diagnosed the dragon hiccups and picked a gentle care plan. You practiced Helping + Science. New gear: Care Cape.",
                        "The dragon puffs a tiny thank-you smoke ring."),
                    new PartyStationSeedDefinition(
                        $"{id}.space_hamster",
                        "Space Hamster Tummy Check",
                        false,
                        "Connect the clues and plan the kindest tummy check for the hamster.",
                        new[]
                        {
                            Obj(id, "food_token", "Food Token", PartyStationObjectRole.Clue, "care_chart", "react.glow", "Science"),
                            Obj(id, "movement_card", "Movement Card", PartyStationObjectRole.Clue, "rest_pod", "react.glow", "Reasoning"),
                            Obj(id, "rest_pod", "Rest Pod", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Helping"),
                            Obj(id, "sound_clue", "Sound Clue", PartyStationObjectRole.Clue, "care_chart", "react.pop", "Communication"),
                            Obj(id, "care_chart", "Care Chart", PartyStationObjectRole.CoreTask, "", "react.cheer", "Helping")
                        },
                        "Connect the clues to the tummy-check plan, then choose the safest comfort step.",
                        "Nurse Nova waves: our space hamster needs a cozy tummy check.",
                        "Each clue card points to one part of the care chart.",
                        "Follow the glowing line from clue to chart.",
                        "All checked! The space hamster curls up happy in the rest pod.",
                        "Finish the check-up to earn the Care Cape!",
                        "You read the space-hamster clues and made a kind care plan. You practiced Helping + Communication. New gear: Care Cape.",
                        "The hamster does a slow, floaty happy spin.")
                });
        }

        private static PartyStationDefinition GameStudioCompose()
        {
            var id = CareerQuestCatalog.GameStudioId;
            return new PartyStationDefinition(
                id,
                "Game Studio Compose",
                new[] { "compose", "pitch" },
                ToyPatternId.ComposeSet,
                "Captain Loop",
                "playful design lead",
                "Pick a goal, an obstacle, and a rule that fit, then run the playtest.",
                new[]
                {
                    Obj(id, "hero_token", "Hero Token", PartyStationObjectRole.CoreTask, "", "react.pop", "Creativity"),
                    Obj(id, "obstacle_tile", "Obstacle Tile", PartyStationObjectRole.CoreTask, "", "react.wobble", "Reasoning"),
                    Obj(id, "rule_card", "Rule Card", PartyStationObjectRole.Clue, "obstacle_tile", "react.glow", "Reasoning"),
                    Obj(id, "powerup_sketch", "Power-Up Sketch", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                    Obj(id, "playtest_button", "Playtest Button", PartyStationObjectRole.Reaction, "hero_token", "react.cheer")
                },
                "Choose a goal, obstacle, and rule that fit together, then run the playtest.",
                new[]
                {
                    new TraitDelta("Creativity", 5),
                    new TraitDelta("Reasoning", 4),
                    new TraitDelta("Communication", 3)
                },
                "accessory.sketchbook",
                new[] { "game_designer", "animator", "entrepreneur", "journalist" },
                "badge.game_studio",
                "campus.game_studio",
                "prop.city_piece_game_studio",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.sidekick_quest",
                        "Sidekick Quest Builder",
                        true,
                        "",
                        null,
                        "Choose a goal, obstacle, and rule that fit together, then run the playtest.",
                        "Captain Loop salutes: let's build a quest for our tiny sidekick!",
                        "A good quest needs a goal, one obstacle, and one fair rule.",
                        "Pieces that fit together glow when you line them up.",
                        "Playtest passed! The sidekick quest is fun and fair.",
                        "Ship the quest to earn the Sketchbook!",
                        "You built a sidekick quest with a clear goal and rule. You practiced Creativity + Reasoning. New gear: Sketchbook.",
                        "The sidekick hops through the level, cheering."),
                    new PartyStationSeedDefinition(
                        $"{id}.button_boss",
                        "Button Boss Battle",
                        false,
                        "Compose a fair boss challenge with a retry rule, then pitch your idea.",
                        new[]
                        {
                            Obj(id, "boss_mood_card", "Boss Mood Card", PartyStationObjectRole.Clue, "challenge_tile", "react.glow", "Reasoning"),
                            Obj(id, "challenge_tile", "Challenge Tile", PartyStationObjectRole.CoreTask, "", "react.wobble", "Creativity"),
                            Obj(id, "controller_sketch", "Controller Sketch", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                            Obj(id, "retry_card", "Retry Card", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                            Obj(id, "pitch_mic", "Pitch Mic", PartyStationObjectRole.Reaction, "controller_sketch", "react.cheer")
                        },
                        "Compose a fair challenge loop, add a retry rule, then pitch the game idea.",
                        "Captain Loop grins: the button boss needs a battle that's tough but fair.",
                        "Check the boss mood card before picking the challenge.",
                        "A retry card keeps the battle fair — add one to the loop.",
                        "Great pitch! The button boss battle is challenging and kind.",
                        "Pitch the boss battle to earn the Sketchbook!",
                        "You designed a fair boss battle and pitched the core loop. You practiced Creativity + Communication. New gear: Sketchbook.",
                        "The button boss bows and presses its own happy button.")
                });
        }

        private static PartyStationDefinition WeatherLabRescue()
        {
            var id = CareerQuestCatalog.WeatherLabId;
            return new PartyStationDefinition(
                id,
                "Weather Lab Rescue",
                new[] { "trace", "protect" },
                ToyPatternId.TracePath,
                "Radar Rae",
                "alert safety planner",
                "Trace the shelter route in order before the parade starts.",
                new[]
                {
                    Obj(id, "forecast_tiles", "Forecast Tiles", PartyStationObjectRole.Clue, "shelter_flag", "react.glow", "Science"),
                    Obj(id, "umbrella_sign", "Umbrella Sign", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                    Obj(id, "route_cones", "Route Cones", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                    Obj(id, "calm_radio", "Calm Radio", PartyStationObjectRole.Helper, "umbrella_sign", "react.sparkle", "Communication"),
                    Obj(id, "shelter_flag", "Shelter Flag", PartyStationObjectRole.CoreTask, "", "react.cheer", "Helping")
                },
                "Trace the route from the forecast through the shelter stops, in order.",
                new[]
                {
                    new TraitDelta("Science", 5),
                    new TraitDelta("Reasoning", 4),
                    new TraitDelta("Helping", 3)
                },
                "accessory.weather_goggles",
                new[] { "meteorologist", "emergency_planner", "environmental_scientist" },
                "badge.weather_lab",
                "campus.weather_lab",
                "prop.city_piece_weather_lab",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.thunder_parade",
                        "Thunder Parade Shelter",
                        true,
                        "",
                        null,
                        "Trace the route from the forecast through the shelter stops, in order.",
                        "Radar Rae points: rain clouds race the parade — trace the shelter route!",
                        "Trace the route stops in order, first to last.",
                        "The next shelter stop glows on the parade route — trace to it.",
                        "The parade marches on, dry and happy along your traced route!",
                        "Protect the parade to earn the Weather Goggles!",
                        "You traced the forecast and shelter route to protect the parade. You practiced Science + Helping. New gear: Weather Goggles.",
                        "The calm radio plays a sunny little jingle."),
                    new PartyStationSeedDefinition(
                        $"{id}.bubblegum_flood",
                        "Bubblegum Flood Map",
                        false,
                        "Trace the safe route across the flood map, in order.",
                        new[]
                        {
                            Obj(id, "rain_gauge", "Rain Gauge", PartyStationObjectRole.Clue, "drain_tile", "react.glow", "Science"),
                            Obj(id, "drain_tile", "Drain Tile", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                            Obj(id, "bridge_block", "Bridge Block", PartyStationObjectRole.CoreTask, "", "react.bounce", "Building"),
                            Obj(id, "warning_badge", "Heads-Up Badge", PartyStationObjectRole.Reaction, "helper_radio", "react.sparkle"),
                            Obj(id, "helper_radio", "Helper Radio", PartyStationObjectRole.CoreTask, "", "react.cheer", "Communication")
                        },
                        "Trace the route from the rain gauge through the safe map fixes, in order.",
                        "Radar Rae laughs: a bubblegum flood is spreading — trace the safe route!",
                        "Trace the route from the rain gauge, in order.",
                        "Glowing map squares show the next stop — trace to it.",
                        "Route traced! The bubblegum drains away and the town stays cozy.",
                        "Fix the map to earn the Weather Goggles!",
                        "You traced a safe route across the bubblegum flood map. You practiced Reasoning + Science. New gear: Weather Goggles.",
                        "The helper radio beeps a proud all-clear tune.")
                });
        }

        private static PartyStationDefinition SpaceportPilot()
        {
            var id = CareerQuestCatalog.SpaceportId;
            return new PartyStationDefinition(
                id,
                "Spaceport Connect",
                new[] { "wire", "connect" },
                ToyPatternId.WireUp,
                "Commander Orbit",
                "focused mission guide",
                "Connect each cable to its matching port to power up the snack probe.",
                new[]
                {
                    Obj(id, "moon_rover", "Moon Rover", PartyStationObjectRole.CoreTask, "rover_dock", "react.pop", "Spatial Thinking"),
                    Obj(id, "rover_dock", "Rover Dock", PartyStationObjectRole.CoreTask, "moon_rover", "react.bounce", "Focus"),
                    Obj(id, "signal_beam", "Signal Beam", PartyStationObjectRole.CoreTask, "dish_array", "react.sparkle", "Spatial Thinking"),
                    Obj(id, "dish_array", "Dish Array", PartyStationObjectRole.CoreTask, "signal_beam", "react.glow", "Focus"),
                    Obj(id, "launch_lamp", "Launch Lamp", PartyStationObjectRole.Reaction, "moon_rover", "react.cheer")
                },
                "Connect each cable to its matching port, in any order.",
                new[]
                {
                    new TraitDelta("Focus", 5),
                    new TraitDelta("Spatial Thinking", 4),
                    new TraitDelta("Leadership", 3)
                },
                "accessory.mission_patch",
                new[] { "pilot", "mission_planner" },
                "badge.spaceport",
                "campus.spaceport",
                "prop.city_piece_spaceport",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.snack_probe",
                        "Cosmic Snack Probe",
                        true,
                        "",
                        null,
                        "Connect each cable to its matching port to power up the probe.",
                        "Commander Orbit salutes: connect the probe cables to power it up!",
                        "Draw a wire from a cable to its matching port.",
                        "Matching ports glow when you hold a cable near them.",
                        "Power on! Every cable found its matching port.",
                        "Power the probe to earn the Mission Patch!",
                        "You connected every cable to its matching port. You practiced Focus plus Spatial Thinking. New gear: Mission Patch.",
                        "Mission control claps as the probe lights blink on."),
                    new PartyStationSeedDefinition(
                        $"{id}.moon_mail",
                        "Moon Mail Delivery",
                        false,
                        "Connect each mail line to its matching slot before the drop.",
                        new[]
                        {
                            Obj(id, "mail_pod", "Mail Pod", PartyStationObjectRole.CoreTask, "pod_slot", "react.pop", "Focus"),
                            Obj(id, "pod_slot", "Pod Slot", PartyStationObjectRole.CoreTask, "mail_pod", "react.bounce", "Focus"),
                            Obj(id, "relay_wire", "Relay Wire", PartyStationObjectRole.CoreTask, "relay_hub", "react.sparkle", "Spatial Thinking"),
                            Obj(id, "relay_hub", "Relay Hub", PartyStationObjectRole.CoreTask, "relay_wire", "react.glow", "Spatial Thinking"),
                            Obj(id, "ready_beacon", "Ready Beacon", PartyStationObjectRole.Reaction, "mail_pod", "react.cheer")
                        },
                        "Connect each mail line to its matching slot, in any order.",
                        "Commander Orbit waves: moon mail is due - link the relays!",
                        "Draw a wire from each line to its matching slot.",
                        "The matching slot glows when you hold a line near it.",
                        "Linked! Every mail line reached its slot.",
                        "Deliver the mail to earn the Mission Patch!",
                        "You linked every mail line to its slot. You practiced Focus plus Leadership. New gear: Mission Patch.",
                        "A moon mailbox flips its flag up with a happy click.")
                });
        }

        private static PartyStationDefinition NewsroomStorySprint()
        {
            var id = CareerQuestCatalog.NewsroomId;
            return new PartyStationDefinition(
                id,
                "Newsroom Story Scan",
                new[] { "scan", "investigate" },
                ToyPatternId.ScanReveal,
                "Scoop Rivera",
                "fact-checking reporter",
                "Scan the scene to reveal each clue, then tap what you find.",
                new[]
                {
                    Obj(id, "smudged_print", "Smudged Print", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                    Obj(id, "torn_note", "Torn Note", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Reasoning"),
                    Obj(id, "faint_footprint", "Faint Footprint", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                    Obj(id, "hidden_label", "Hidden Label", PartyStationObjectRole.CoreTask, "", "react.glow", "Communication"),
                    Obj(id, "headline_stamp", "Headline Stamp", PartyStationObjectRole.Reaction, "smudged_print", "react.cheer")
                },
                "Scan the scene to reveal every clue, then tap each one for the story.",
                new[]
                {
                    new TraitDelta("Communication", 5),
                    new TraitDelta("Reasoning", 4),
                    new TraitDelta("Creativity", 3)
                },
                "accessory.press_badge",
                new[] { "journalist", "lawyer", "teacher" },
                "badge.newsroom",
                "campus.newsroom",
                "prop.city_piece_newsroom",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.mystery_mural",
                        "Mystery Mural News",
                        true,
                        "",
                        null,
                        "Scan the scene to reveal every clue, then tap each one for the story.",
                        "Scoop Rivera grins: the mural has hidden clues - scan to find them!",
                        "Slide the magnifier over the scene to reveal a clue.",
                        "Clues sparkle when the magnifier passes over them.",
                        "Headline ready! You found every clue for a clear story.",
                        "Print the story to earn the Press Badge!",
                        "You scanned the scene and revealed every clue for a clear headline. You practiced Communication plus Reasoning. New gear: Press Badge.",
                        "The art club waves proudly from the front page."),
                    new PartyStationSeedDefinition(
                        $"{id}.invention_scoop",
                        "Playground Invention Scoop",
                        false,
                        "Scan the workshop to reveal each clue, then tap what you find.",
                        new[]
                        {
                            Obj(id, "blurry_photo", "Blurry Photo", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                            Obj(id, "secret_memo", "Secret Memo", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Reasoning"),
                            Obj(id, "buried_clip", "Buried Clip", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                            Obj(id, "faded_quote", "Faded Quote", PartyStationObjectRole.CoreTask, "", "react.glow", "Communication"),
                            Obj(id, "publish_button", "Publish Button", PartyStationObjectRole.Reaction, "blurry_photo", "react.cheer")
                        },
                        "Scan the workshop to reveal every clue, then tap each one.",
                        "Scoop Rivera beams: a bouncy seesaw scoop with clues to uncover!",
                        "Slide the magnifier over the workshop to reveal a clue.",
                        "Each clue sparkles when the magnifier finds it.",
                        "Published! You uncovered every clue for the scoop.",
                        "Publish the scoop to earn the Press Badge!",
                        "You scanned the workshop and revealed every clue to publish. You practiced Communication plus Creativity. New gear: Press Badge.",
                        "The inventor pins your finished story to the playground board.")
                });
        }

        private static PartyStationDefinition GreenCityBuilder()
        {
            var id = CareerQuestCatalog.GreenCityId;
            return new PartyStationDefinition(
                id,
                "Green City Builder",
                new[] { "balance", "build" },
                ToyPatternId.BalanceMeters,
                "Grid Green",
                "practical systems planner",
                "Place four city pieces while keeping both meters happy and green.",
                new[]
                {
                    Obj(id, "solar_tile", "Solar Tile", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Science"),
                    Obj(id, "garden_block", "Garden Block", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                    Obj(id, "bike_path", "Bike Path", PartyStationObjectRole.CoreTask, "", "react.bounce", "Building"),
                    Obj(id, "water_wheel", "Water Wheel", PartyStationObjectRole.CoreTask, "", "react.pop", "Science"),
                    Obj(id, "budget_meter", "Budget Meter", PartyStationObjectRole.Meter, "", "react.meter_shift", "Reasoning"),
                    Obj(id, "happy_meter", "Happy Meter", PartyStationObjectRole.Meter, "", "react.meter_shift", "Collaboration")
                },
                "Place four city pieces while keeping both meters in the green zone.",
                new[]
                {
                    new TraitDelta("Building", 5),
                    new TraitDelta("Science", 4),
                    new TraitDelta("Collaboration", 3)
                },
                "accessory.green_hardhat",
                new[] { "renewable_energy_engineer", "city_planner", "architect" },
                "badge.green_city",
                "campus.green_city",
                "prop.city_piece_green_city",
                new[]
                {
                    new PartyStationSeedDefinition(
                        $"{id}.solar_sandwich",
                        "Solar Sandwich City",
                        true,
                        "",
                        null,
                        "Place four city pieces while keeping both meters in the green zone.",
                        "Grid Green unrolls the map: let's build a sunny city for everyone!",
                        "Watch both meters every time you place a piece.",
                        "If a meter dips, try a piece that fills the gap.",
                        "City complete! Both meters glow green and the town cheers.",
                        "Balance the city to earn the Green Hardhat!",
                        "You built a solar sandwich city and balanced the community meters. You practiced Building + Collaboration. New gear: Green Hardhat.",
                        "Tiny neighbors picnic happily in the new garden block."),
                    new PartyStationSeedDefinition(
                        $"{id}.windy_rooftop",
                        "Windy Rooftop Rescue",
                        false,
                        "Adjust the rooftop build until energy and neighbor meters both pass.",
                        new[]
                        {
                            Obj(id, "wind_turbine", "Wind Turbine", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Science"),
                            Obj(id, "shade_roof", "Shade Roof", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                            Obj(id, "battery_block", "Battery Block", PartyStationObjectRole.CoreTask, "", "react.bounce", "Building"),
                            Obj(id, "street_tree", "Street Tree", PartyStationObjectRole.CoreTask, "", "react.pop", "Collaboration"),
                            Obj(id, "energy_meter", "Energy Meter", PartyStationObjectRole.Meter, "", "react.meter_shift", "Science"),
                            Obj(id, "neighbor_meter", "Neighbor Meter", PartyStationObjectRole.Meter, "", "react.meter_shift", "Helping")
                        },
                        "Adjust the rooftop build until the energy and neighbor meters both pass.",
                        "Grid Green points up: this windy rooftop could power the block!",
                        "The wind turbine loves the breezy corner.",
                        "A glowing meter shows which side needs balance next.",
                        "Rooftop ready! Energy is up and the neighbors love the shade.",
                        "Tune the rooftop to earn the Green Hardhat!",
                        "You tuned the windy rooftop plan and kept both meters healthy. You practiced Building + Science. New gear: Green Hardhat.",
                        "A neighbor hangs a tiny windsock that spins with joy.")
                });
        }

        private static PartyStationObjectDefinition Obj(
            string stationId,
            string objectId,
            string displayName,
            PartyStationObjectRole role,
            string targetId,
            string reactionKey,
            string traitHint = "")
        {
            return new PartyStationObjectDefinition(
                objectId,
                displayName,
                role,
                $"prop.party.{stationId}.{objectId}",
                targetId,
                reactionKey,
                traitHint);
        }
    }
}
