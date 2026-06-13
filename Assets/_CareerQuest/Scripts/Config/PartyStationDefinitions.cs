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
                new[] { "build", "rescue" },
                ToyPatternId.DragToSlot,
                "Bolt the Bench Buddy",
                "upbeat build coach",
                "A lunchbox robot lost its parts! Rebuild it and pick a rescue route.",
                new[]
                {
                    Obj(id, "battery_toast", "Battery Toast", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                    Obj(id, "wheel_sandwich", "Wheel Sandwich", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                    Obj(id, "sensor_sticker", "Sensor Sticker", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Reasoning"),
                    Obj(id, "route_cards", "Route Cards", PartyStationObjectRole.Clue, "rescue_flag", "react.glow", "Reasoning"),
                    Obj(id, "rescue_flag", "Rescue Flag", PartyStationObjectRole.Reaction, "route_cards", "react.cheer")
                },
                "Place three robot parts, then pick the route that matches the clue.",
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
                        "Place three robot parts, then pick the route that matches the clue.",
                        "Bolt beeps: the lunchbox robot lost its parts on the way to a rescue!",
                        "Try a part that matches the empty slot shape.",
                        "Watch the glowing slot — that part goes there next.",
                        "Beep-beep! The robot is rebuilt and rolling on the steady route!",
                        "Finish the rescue to earn the Tool Belt!",
                        "You rebuilt the lunchbox robot and chose the steady rescue route. You practiced Building + Reasoning. New gear: Tool Belt.",
                        "The rescued rover spins a happy little victory circle."),
                    new PartyStationSeedDefinition(
                        $"{id}.moon_cart",
                        "Moon Cart Tune-Up",
                        false,
                        "The moon cart wobbled to a stop! Fix three systems and cross the crater.",
                        new[]
                        {
                            Obj(id, "moon_wheel", "Moon Wheel", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                            Obj(id, "flashlight_eye", "Flashlight Eye", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Building"),
                            Obj(id, "antenna_straw", "Antenna Straw", PartyStationObjectRole.CoreTask, "", "react.pop", "Focus"),
                            Obj(id, "fuel_snack_pack", "Fuel Snack Pack", PartyStationObjectRole.Helper, "moon_wheel", "react.bounce"),
                            Obj(id, "crater_map", "Crater Map", PartyStationObjectRole.Clue, "antenna_straw", "react.glow", "Reasoning")
                        },
                        "Repair three moon-cart systems, then choose the safest crater crossing.",
                        "Bolt waves: this moon cart needs a tune-up before the crater crossing!",
                        "Check the cart for a wobbly spot that needs a part.",
                        "The crater map glows near the safest path — follow it.",
                        "The moon cart hums across the crater like a champ!",
                        "Tune up the cart to earn the Tool Belt!",
                        "You tuned the moon cart and guided it across the safest crater path. You practiced Building + Focus. New gear: Tool Belt.",
                        "The moon cart flashes its flashlight eye in thanks.")
                });
        }

        private static PartyStationDefinition AiLabSort()
        {
            var id = CareerQuestCatalog.AiLabId;
            return new PartyStationDefinition(
                id,
                "AI Lab Sort",
                new[] { "sort", "test" },
                ToyPatternId.SortToBin,
                "Pixel the Pattern Pal",
                "curious data helper",
                "Teach the bubblegum sorter by putting each example in its matching bin.",
                new[]
                {
                    // U5 sort tuning: fact and guess bubbles SORT APART — facts
                    // land in the Reasoning bin, guesses in the Creativity bin
                    // (a shared TraitHint would collapse the sort into one bin).
                    Obj(id, "blue_fact_bubbles", "Blue Fact Bubbles", PartyStationObjectRole.CoreTask, "", "react.pop", "Reasoning"),
                    Obj(id, "pink_guess_bubbles", "Pink Guess Bubbles", PartyStationObjectRole.CoreTask, "", "react.pop", "Creativity"),
                    Obj(id, "training_tray", "Training Tray", PartyStationObjectRole.CoreTask, "", "react.glow", "Science"),
                    Obj(id, "test_button", "Test Button", PartyStationObjectRole.Reaction, "training_tray", "react.sparkle"),
                    Obj(id, "mystery_bubble", "Mystery Bubble", PartyStationObjectRole.CoreTask, "", "react.bounce", "Science")
                },
                "Sort the examples into matching bins, then test one mystery example.",
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
                        "Sort the examples into matching bins, then test one mystery example.",
                        "Pixel grins: let's teach the bubblegum sorter which bubbles match!",
                        "Look at each bubble's color and shape before you pick a bin.",
                        "The matching bin glows when you hold a bubble close.",
                        "The sorter learned it! The mystery bubble landed just right.",
                        "Train the sorter to earn the Lab Goggles!",
                        "You trained the bubblegum sorter and tested a mystery example. You practiced Reasoning + Science. New gear: Lab Goggles.",
                        "Pixel does a proud little data dance."),
                    new PartyStationSeedDefinition(
                        $"{id}.sock_satellite",
                        "Sock Satellite Classifier",
                        false,
                        "Sort clear sock signals from fuzzy static so the satellite can launch.",
                        new[]
                        {
                            // U5 sort tuning: clear signals and fuzzy static must
                            // land in DIFFERENT bins for the separation to play.
                            Obj(id, "striped_sock_signals", "Striped Sock Signals", PartyStationObjectRole.CoreTask, "", "react.pop", "Communication"),
                            Obj(id, "star_stamps", "Star Stamps", PartyStationObjectRole.Clue, "striped_sock_signals", "react.glow", "Focus"),
                            Obj(id, "static_dots", "Static Dots", PartyStationObjectRole.CoreTask, "", "react.wobble", "Reasoning"),
                            Obj(id, "training_bins", "Training Bins", PartyStationObjectRole.CoreTask, "", "react.glow", "Science"),
                            Obj(id, "launch_check", "Launch Check", PartyStationObjectRole.Reaction, "training_bins", "react.cheer")
                        },
                        "Separate clear signals from static, then approve the clean launch check.",
                        "Pixel whispers: the sock satellite is getting fuzzy signals — help sort!",
                        "Stripes mean a clear signal; gray dots are just static.",
                        "The star stamps mark which socks are true signals.",
                        "Clean signals locked in — the sock satellite is ready to fly!",
                        "Clean up the signals to earn the Lab Goggles!",
                        "You cleaned up the sock-satellite signals and launched the good data. You practiced Reasoning + Focus. New gear: Lab Goggles.",
                        "The launch check light blinks a cheerful green.")
                });
        }

        private static PartyStationDefinition CommunityKitchenMatch()
        {
            var id = CareerQuestCatalog.CommunityKitchenId;
            return new PartyStationDefinition(
                id,
                "Community Kitchen Match",
                new[] { "match", "serve" },
                ToyPatternId.PickMatchingTrio,
                "Chef Sunny",
                "warm service coach",
                "Solve the soup clues and serve a bowl every guest can enjoy.",
                new[]
                {
                    Obj(id, "recipe_card", "Recipe Card", PartyStationObjectRole.Clue, "veggie_clue", "react.glow", "Reasoning"),
                    Obj(id, "veggie_clue", "Veggie Clue", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                    Obj(id, "spice_jar", "Spice Jar", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                    Obj(id, "serving_bowl", "Serving Bowl", PartyStationObjectRole.CoreTask, "", "react.bounce", "Helping"),
                    Obj(id, "kindness_swap", "Kindness Swap", PartyStationObjectRole.Helper, "serving_bowl", "react.cheer", "Collaboration")
                },
                "Match the recipe clue to the right ingredients, then make one kindness swap.",
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
                        "Match the recipe clue to the right ingredients, then make one kindness swap.",
                        "Chef Sunny sniffs: this mystery soup is missing its matching pieces!",
                        "Read the recipe card — it points to the right ingredients.",
                        "The matching ingredient glows when the clue fits.",
                        "What a bowl! Every guest gets a soup they can enjoy.",
                        "Serve the soup to earn the Chef Hat!",
                        "You solved the soup clues and served a guest-friendly bowl. You practiced Helping + Collaboration. New gear: Chef Hat.",
                        "A happy guest gives the soup two big thumbs up."),
                    new PartyStationSeedDefinition(
                        $"{id}.tiny_planet_picnic",
                        "Tiny Planet Picnic",
                        false,
                        "Pack picnic trays so every tiny planet neighbor gets a tasty match.",
                        new[]
                        {
                            Obj(id, "planet_lunchbox", "Planet Lunchbox", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                            Obj(id, "color_card", "Color Card", PartyStationObjectRole.Clue, "planet_lunchbox", "react.glow", "Reasoning"),
                            Obj(id, "crunchy_veggie", "Crunchy Veggie", PartyStationObjectRole.CoreTask, "", "react.bounce", "Helping"),
                            Obj(id, "warm_snack", "Warm Snack", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                            Obj(id, "thank_you_stamp", "Thank-You Stamp", PartyStationObjectRole.Reaction, "planet_lunchbox", "react.cheer")
                        },
                        "Match each picnic request to the best food trio, then stamp the tray.",
                        "Chef Sunny giggles: tiny planet neighbors ordered a picnic!",
                        "The color card shows which foods belong together.",
                        "Hold a food near a tray — the right tray glows.",
                        "Every tray matched! The picnic is a tiny planet party.",
                        "Pack the picnic to earn the Chef Hat!",
                        "You packed the tiny planet picnic and matched each tray with care. You practiced Helping + Creativity. New gear: Chef Hat.",
                        "The neighbors stamp a thank-you on every tray.")
                });
        }

        private static PartyStationDefinition MusicRemix()
        {
            var id = CareerQuestCatalog.MusicStudioId;
            return new PartyStationDefinition(
                id,
                "Music Remix",
                new[] { "remix", "compose" },
                ToyPatternId.ComposeSet,
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
                new[] { "predict", "protect" },
                ToyPatternId.SequenceCards,
                "Radar Rae",
                "alert safety planner",
                "Order the forecast clues, then set up shelter before the parade starts.",
                new[]
                {
                    Obj(id, "forecast_tiles", "Forecast Tiles", PartyStationObjectRole.Clue, "shelter_flag", "react.glow", "Science"),
                    Obj(id, "umbrella_sign", "Umbrella Sign", PartyStationObjectRole.CoreTask, "", "react.pop", "Helping"),
                    Obj(id, "route_cones", "Route Cones", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                    Obj(id, "calm_radio", "Calm Radio", PartyStationObjectRole.Helper, "umbrella_sign", "react.sparkle", "Communication"),
                    Obj(id, "shelter_flag", "Shelter Flag", PartyStationObjectRole.CoreTask, "", "react.cheer", "Helping")
                },
                "Order the forecast clues, then place the shelter tools before the parade starts.",
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
                        "Order the forecast clues, then place the shelter tools before the parade starts.",
                        "Radar Rae points: rain clouds are racing the parade — let's get ready!",
                        "Read the forecast tiles in order, first to last.",
                        "The next shelter spot glows on the parade route.",
                        "The parade marches on, dry and happy under your shelter plan!",
                        "Protect the parade to earn the Weather Goggles!",
                        "You read the forecast and protected the thunder parade route. You practiced Science + Helping. New gear: Weather Goggles.",
                        "The calm radio plays a sunny little jingle."),
                    new PartyStationSeedDefinition(
                        $"{id}.bubblegum_flood",
                        "Bubblegum Flood Map",
                        false,
                        "Match the water clues to safe map fixes, then send the helper update.",
                        new[]
                        {
                            Obj(id, "rain_gauge", "Rain Gauge", PartyStationObjectRole.Clue, "drain_tile", "react.glow", "Science"),
                            Obj(id, "drain_tile", "Drain Tile", PartyStationObjectRole.CoreTask, "", "react.pop", "Building"),
                            Obj(id, "bridge_block", "Bridge Block", PartyStationObjectRole.CoreTask, "", "react.bounce", "Building"),
                            Obj(id, "warning_badge", "Heads-Up Badge", PartyStationObjectRole.Reaction, "helper_radio", "react.sparkle"),
                            Obj(id, "helper_radio", "Helper Radio", PartyStationObjectRole.CoreTask, "", "react.cheer", "Communication")
                        },
                        "Match water clues to safe map fixes, then send the helper-radio update.",
                        "Radar Rae laughs: a bubblegum flood is bubbling across the map!",
                        "The rain gauge shows where the bubblegum rises first.",
                        "Glowing map squares show where a fix will help most.",
                        "Map fixed! The bubblegum drains away and the town stays cozy.",
                        "Fix the map to earn the Weather Goggles!",
                        "You mapped the bubblegum flood and chose safe fixes fast. You practiced Reasoning + Science. New gear: Weather Goggles.",
                        "The helper radio beeps a proud all-clear tune.")
                });
        }

        private static PartyStationDefinition SpaceportPilot()
        {
            var id = CareerQuestCatalog.SpaceportId;
            return new PartyStationDefinition(
                id,
                "Spaceport Pilot",
                new[] { "trace", "navigate" },
                ToyPatternId.TracePath,
                "Commander Orbit",
                "focused mission guide",
                "Trace the flight path: launch, orbit, deliver, then land the snack probe.",
                new[]
                {
                    Obj(id, "launch_checklist", "Launch Checklist", PartyStationObjectRole.Clue, "fuel_bead", "react.glow", "Focus"),
                    Obj(id, "fuel_bead", "Fuel Bead", PartyStationObjectRole.CoreTask, "", "react.pop", "Focus"),
                    Obj(id, "snack_crate", "Snack Crate", PartyStationObjectRole.CoreTask, "", "react.bounce", "Spatial Thinking"),
                    Obj(id, "orbit_arrow", "Orbit Arrow", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Spatial Thinking"),
                    Obj(id, "landing_pad", "Landing Pad", PartyStationObjectRole.Reaction, "orbit_arrow", "react.cheer")
                },
                "Trace launch, orbit, delivery, and landing in the correct order along the flight path.",
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
                        "Sequence launch, orbit, delivery, and landing in the correct order.",
                        "Commander Orbit salutes: the snack probe is fueled and ready!",
                        "Follow the launch checklist from the top.",
                        "The next mission step glows on the checklist.",
                        "Touchdown! The snack probe landed right on the pad.",
                        "Land the probe to earn the Mission Patch!",
                        "You launched the snack probe and landed it on the right pad. You practiced Focus + Spatial Thinking. New gear: Mission Patch.",
                        "Mission control claps as the probe doors pop open."),
                    new PartyStationSeedDefinition(
                        $"{id}.moon_mail",
                        "Moon Mail Delivery",
                        false,
                        "Sequence the mail route and repair the blocked step before docking.",
                        new[]
                        {
                            Obj(id, "mail_capsule", "Mail Capsule", PartyStationObjectRole.CoreTask, "", "react.pop", "Focus"),
                            Obj(id, "route_cards", "Route Cards", PartyStationObjectRole.Clue, "booster_tile", "react.glow", "Spatial Thinking"),
                            Obj(id, "booster_tile", "Booster Tile", PartyStationObjectRole.CoreTask, "", "react.bounce", "Spatial Thinking"),
                            Obj(id, "dock_beacon", "Dock Beacon", PartyStationObjectRole.Reaction, "mail_capsule", "react.sparkle"),
                            Obj(id, "repair_wrench", "Repair Wrench", PartyStationObjectRole.CoreTask, "", "react.cheer", "Building")
                        },
                        "Sequence the delivery route and repair one blocked step before docking.",
                        "Commander Orbit waves: moon mail is due — chart the route!",
                        "Lay the route cards in flight order.",
                        "A blocked step blinks — use the repair wrench there.",
                        "Mail delivered! The dock beacon glows a proud gold.",
                        "Deliver the mail to earn the Mission Patch!",
                        "You delivered moon mail and solved a blocked docking step. You practiced Focus + Leadership. New gear: Mission Patch.",
                        "A moon mailbox flips its flag up with a happy click.")
                });
        }

        private static PartyStationDefinition NewsroomStorySprint()
        {
            var id = CareerQuestCatalog.NewsroomId;
            return new PartyStationDefinition(
                id,
                "Newsroom Story Sprint",
                new[] { "investigate", "compose" },
                ToyPatternId.ComposeSet,
                "Scoop Rivera",
                "fact-checking reporter",
                "Match the checked facts to who, what, and where, then stamp the headline.",
                new[]
                {
                    Obj(id, "who_card", "Who Card", PartyStationObjectRole.CoreTask, "", "react.pop", "Communication"),
                    Obj(id, "what_photo", "What Photo", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Reasoning"),
                    Obj(id, "where_map", "Where Map", PartyStationObjectRole.CoreTask, "", "react.bounce", "Spatial Thinking"),
                    Obj(id, "quote_recorder", "Quote Recorder", PartyStationObjectRole.Clue, "who_card", "react.glow", "Communication"),
                    Obj(id, "fact_check_stamp", "Fact-Check Stamp", PartyStationObjectRole.Reaction, "what_photo", "react.cheer")
                },
                "Match verified facts to who, what, and where, then stamp a safe headline.",
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
                        "Match verified facts to who, what, and where, then stamp a safe headline.",
                        "Scoop Rivera gasps: a mystery mural appeared overnight — who made it?",
                        "Only stamp facts you can check twice.",
                        "The quote recorder replays the clue about who was there.",
                        "Headline stamped! The mural story is clear and true.",
                        "Print the story to earn the Press Badge!",
                        "You verified the mural mystery and wrote a clear headline. You practiced Communication + Reasoning. New gear: Press Badge.",
                        "The mural maker waves proudly from the front page."),
                    new PartyStationSeedDefinition(
                        $"{id}.invention_scoop",
                        "Playground Invention Scoop",
                        false,
                        "Arrange the checked timeline and publish only with the source badge.",
                        new[]
                        {
                            Obj(id, "witness_quote", "Witness Quote", PartyStationObjectRole.Clue, "timeline_cards", "react.glow", "Communication"),
                            Obj(id, "sketch_photo", "Sketch Photo", PartyStationObjectRole.CoreTask, "", "react.sparkle", "Creativity"),
                            Obj(id, "timeline_cards", "Timeline Cards", PartyStationObjectRole.CoreTask, "", "react.bounce", "Reasoning"),
                            Obj(id, "source_badge", "Source Badge", PartyStationObjectRole.CoreTask, "", "react.pop", "Communication"),
                            Obj(id, "publish_button", "Publish Button", PartyStationObjectRole.Reaction, "source_badge", "react.cheer")
                        },
                        "Arrange the verified timeline and publish only when the source badge is on.",
                        "Scoop Rivera grins: someone invented a bouncy seesaw — get the scoop!",
                        "Put the timeline cards in the order things happened.",
                        "No source badge, no story — clip it on before publishing.",
                        "Scoop published! Every fact has a source and a smile.",
                        "Publish the scoop to earn the Press Badge!",
                        "You organized the invention scoop and checked the source before publishing. You practiced Communication + Creativity. New gear: Press Badge.",
                        "The inventor pins your story to the playground board.")
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
