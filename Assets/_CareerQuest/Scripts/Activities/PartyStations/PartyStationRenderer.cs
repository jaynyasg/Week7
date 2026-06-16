using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace CareerQuest
{
    /// <summary>
    /// U4 definition-driven station visuals: tray/target layout, station
    /// theming, and the placeholder toy-token pass over the playfield
    /// <see cref="ToyInteractionKit"/> mounts.
    ///
    /// Placeholder policy (design doc: intentional "prop.party." keys until the
    /// station art pass): object sprite keys resolve through AssetCatalog and
    /// use the cataloged sprite only when it is FINAL art. Anything else
    /// renders as a handmade toy token — a tinted shape with a small world-TMP
    /// label (DoorSign convention) — so stations never surface the magenta
    /// missing checker or a ".fallback" sprite in player-facing art scans.
    /// </summary>
    public static class PartyStationRenderer
    {
        public const string SetRootName = "PartyStationSet";
        public const string TrayBoardName = "PartyStationTrayBoard";
        public const string WorkbenchName = "PartyStationWorkbench";
        public const string ZonePadName = "StationZonePad";
        public const string ZoneLabelName = "StationZoneLabel";
        public const string TokenLabelName = "ToyTokenLabel";
        public const string TaskRingName = "ToyTaskRing";
        public const string TraceRouteName = "StationTraceRoute";
        public const string TraceStepLabelName = "StationTraceStep";
        public const string LaunchPadName = "StationLaunchPad";
        public const string LaunchGoalName = "StationLaunchGoal";
        public const string LaunchToyName = "StationLaunchToy";
        public const string DeduceBoardName = "StationDeduceBoard";
        public const string DeduceCardName = "StationDeduceCard";
        public const string DeduceClueName = "StationDeduceClue";

        /// <summary>Optional poke-toys render at this alpha so the actionable set stands out.</summary>
        public const float OptionalToyAlpha = 0.45f;

        public const float TrayY = -2.35f;
        public const float TargetY = 0.55f;
        public const float TraySpacing = 1.4f;
        public const float TargetSpacing = 1.7f;

        private static readonly Color PathGold = new(0.953f, 0.769f, 0.357f);
        private static readonly Color PaperColor = new(1f, 0.97f, 0.88f, 0.92f);
        private static readonly Color PadColor = new(1f, 0.97f, 0.88f, 0.6f);
        private static readonly Color InkColor = new(0.098f, 0.196f, 0.235f);

        /// <summary>Station identity color: the badge art's primary color, Path Gold fallback.</summary>
        public static Color AccentFor(PartyStationDefinition definition)
        {
            return definition != null && AssetCatalog.TryGetDefinition(definition.BadgeArtKey, out var badge)
                ? badge.PrimaryColor
                : PathGold;
        }

        /// <summary>True when this sprite key has no final art yet (token pass applies).</summary>
        public static bool IsPlaceholderToySprite(string spriteKey)
        {
            return !AssetCatalog.ResolveSprite(spriteKey).IsFinalArt;
        }

        /// <summary>
        /// Toy sprite resolution with placeholder fallback: cataloged final art
        /// passes through; placeholder keys get the shared token base shape
        /// (tinted + labeled later by <see cref="DecoratePlayfield"/>).
        /// </summary>
        public static Sprite ResolveToySprite(string spriteKey)
        {
            var resolution = AssetCatalog.ResolveSprite(spriteKey);
            return resolution.IsFinalArt ? resolution.Sprite : CampusWorldSprites.Circle;
        }

        /// <summary>Deterministic token tint per toy: hue-stepped around the station accent.</summary>
        public static Color TokenColorFor(Color accent, int objectIndex)
        {
            Color.RGBToHSV(accent, out var hue, out var saturation, out var value);
            var steppedHue = Mathf.Repeat(hue + objectIndex * 0.11f, 1f);
            return Color.HSVToRGB(steppedHue, Mathf.Clamp01(saturation * 0.85f), Mathf.Clamp01(Mathf.Max(value, 0.72f)));
        }

        /// <summary>Tray slots centered along the bottom band (kit trayPositionFor seam).</summary>
        public static Vector3 TrayPosition(int index, int count)
        {
            return SpreadPosition(index, count, TraySpacing, TrayY);
        }

        /// <summary>Target zones centered across the middle band (kit targetPositionFor seam).</summary>
        public static Vector3 TargetPosition(int index, int count)
        {
            return SpreadPosition(index, count, TargetSpacing, TargetY);
        }

        /// <summary>
        /// Seed-independent station set dressing mounted with the room scene:
        /// tray board under the toy row, workbench strip under the targets,
        /// both in the station's accent. Torn down with the world like every
        /// other room prop.
        /// </summary>
        public static Transform MountStationSet(Transform worldRoot, PartyStationDefinition definition)
        {
            if (worldRoot == null || definition == null)
            {
                return null;
            }

            var accent = AccentFor(definition);
            var root = new GameObject(SetRootName).transform;
            root.SetParent(worldRoot, false);

            AddShape(root, TrayBoardName, new Vector3(0f, TrayY - 0.1f, 0f), new Vector3(7f, 1.05f, 1f), PaperColor, 2);
            AddShape(root, $"{TrayBoardName}Stripe", new Vector3(0f, TrayY - 0.58f, 0f), new Vector3(7f, 0.08f, 1f), accent, 3);
            AddShape(root, WorkbenchName, new Vector3(0f, TargetY - 0.62f, 0f), new Vector3(7.6f, 0.32f, 1f), PaperColor, 2);
            AddShape(root, $"{WorkbenchName}Stripe", new Vector3(0f, TargetY - 0.76f, 0f), new Vector3(7.6f, 0.06f, 1f), accent, 3);
            return root;
        }

        /// <summary>
        /// Post-mount decoration over the kit playfield: a paper pad + label
        /// under every drop zone (kid-readable, non-color-only) and the toy
        /// token pass (tint + label) on every placeholder-art piece. Children
        /// ride the kit's transient objects, so teardown stays kit-owned.
        /// </summary>
        public static void DecoratePlayfield(ToyInteractionKit kit, ToyPatternRules rules, Color accent)
        {
            if (kit == null || rules == null || !kit.IsMounted)
            {
                return;
            }

            foreach (var targetId in rules.TargetIds)
            {
                var zone = kit.ZoneFor(targetId);
                if (zone == null)
                {
                    continue;
                }

                AddShape(zone.transform, ZonePadName, new Vector3(0f, 0f, 0f), new Vector3(1.25f, 1.05f, 1f), PadColor, ToyInteractionKit.ZoneSortingOrder - 2);
                AddShape(zone.transform, $"{ZonePadName}Ring", new Vector3(0f, -0.46f, 0f), new Vector3(1.25f, 0.07f, 1f), accent, ToyInteractionKit.ZoneSortingOrder - 1);
                AddWorldLabel(zone.transform, ZoneLabelName, TargetLabelFor(rules, targetId), new Vector3(0f, -0.66f, 0f), 1.7f, ToyInteractionKit.ZoneSortingOrder + 40);
            }

            var objects = rules.Objects;
            for (var index = 0; index < objects.Count; index++)
            {
                var definition = objects[index];
                var piece = kit.PieceFor(definition.ObjectId);
                if (piece == null)
                {
                    continue;
                }

                // Clarity (design review #2): the quest completes on the
                // chain-role toys (CoreTask/Clue/Meter); reaction-only toys
                // (Helper/Wildcard/Reaction/Bonus) are optional pokes that never
                // advance the "X/N toys placed" counter. With every toy drawn
                // identically, players drag the decoys, see no progress, and
                // think the game won't end. Mark the task toys with an accent
                // halo and fade the optional pokes so the actionable set reads
                // at a glance. Presentation only — no rules/scoring change.
                //
                // Part B (#4): the halo/fade/label key off ROLE, not placeholder
                // status, so they still read on final toy art. Only the tinting
                // is placeholder-specific — a real sprite keeps its own colors
                // (just dimmed when it is an optional poke).
                var isTaskToy = definition.IsChainRole;
                var isPlaceholder = IsPlaceholderToySprite(definition.SpriteKey);

                var renderer = piece.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    if (isPlaceholder)
                    {
                        var color = TokenColorFor(accent, index);
                        if (!isTaskToy)
                        {
                            color.a *= OptionalToyAlpha;
                        }

                        renderer.color = color;
                    }
                    else if (!isTaskToy)
                    {
                        var dimmed = renderer.color;
                        dimmed.a *= OptionalToyAlpha;
                        renderer.color = dimmed;
                    }
                }

                if (isTaskToy)
                {
                    // Soft accent disc behind the toy (one sort below the piece)
                    // so required toys read as "do these" without changing the
                    // piece geometry or its drag hit area.
                    var halo = new GameObject(TaskRingName, typeof(SpriteRenderer));
                    halo.transform.SetParent(piece.transform, false);
                    halo.transform.localScale = new Vector3(1.42f, 1.42f, 1f);
                    var haloRenderer = halo.GetComponent<SpriteRenderer>();
                    haloRenderer.sprite = CampusWorldSprites.Circle;
                    var haloColor = accent;
                    haloColor.a = 0.32f;
                    haloRenderer.color = haloColor;
                    haloRenderer.sortingOrder = ToyInteractionKit.PieceSortingOrder - 1;
                }

                AddWorldLabel(piece.transform, TokenLabelName, definition.DisplayName, new Vector3(0f, -0.68f, 0f), 1.5f, ToyInteractionKit.PieceSortingOrder + 2);
            }
        }

        /// <summary>
        /// U5 pointer-first meter widgets (R19): one tap-to-tune dial per meter
        /// zone, fed by the controller's authoritative value provider and
        /// submitting through the SAME drop seam the tests drive. Widgets ride
        /// the kit's zone objects, so teardown stays kit-owned.
        /// </summary>
        public static void MountMeterWidgets(
            ToyInteractionKit kit,
            ToyPatternRules rules,
            Color accent,
            Func<string, int> valueFor,
            Func<string, int, DropSubmitResult> submitMeter)
        {
            if (kit == null || rules == null || !kit.IsMounted || valueFor == null || submitMeter == null)
            {
                return;
            }

            foreach (var meterId in rules.MeterObjectIds)
            {
                var zone = kit.ZoneFor(ToyPatternRules.MeterTargetPrefix + meterId);
                if (zone == null)
                {
                    continue;
                }

                var localMeterId = meterId;
                StationMeterWidget.Mount(
                    zone.gameObject,
                    localMeterId,
                    accent,
                    () => valueFor(localMeterId),
                    value => submitMeter(localMeterId, value));
            }
        }

        /// <summary>
        /// Design-review #3 TracePath route: lays the ordered waypoint zones
        /// along an ascending flight path with a connecting route line and step
        /// numbers, hides the (unused) tray pieces, and makes each waypoint a
        /// tap target over the SAME drop seam. The player taps the route stops
        /// in order — the rules reject out-of-order taps and bounce gently — so
        /// it reads as tracing a path, a distinct verb from dragging tokens to
        /// pads. Pointer-first with non-color cues (numbers + the drawn line),
        /// no harsh fail (R19). Widgets ride the kit's zone objects, so teardown
        /// stays kit-owned.
        /// </summary>
        public static void MountTraceRoute(
            ToyInteractionKit kit,
            ToyPatternRules rules,
            Color accent,
            Func<string, DropSubmitResult> submitWaypoint)
        {
            if (kit == null || rules == null || !kit.IsMounted || submitWaypoint == null)
            {
                return;
            }

            var order = rules.DraggableObjectIds;
            var count = order.Count;
            if (count == 0)
            {
                return;
            }

            // TracePath taps the route; the tray toys are not dragged. Hide
            // every tray piece (waypoints AND reaction pokes) so only the route
            // stops are interactable.
            foreach (var piece in kit.Pieces.Values)
            {
                if (piece != null)
                {
                    piece.gameObject.SetActive(false);
                }
            }

            var positions = new Vector3[count];
            for (var i = 0; i < count; i++)
            {
                var objectId = order[i];
                var zone = kit.ZoneFor(ToyPatternRules.WaypointTargetPrefix + objectId);
                if (zone == null)
                {
                    continue;
                }

                var t = count <= 1 ? 0.5f : i / (float)(count - 1);
                var position = new Vector3(Mathf.Lerp(-3.4f, 3.4f, t), 0.35f + Mathf.Sin(t * Mathf.PI) * 0.85f, 0f);
                zone.transform.localPosition = position;
                positions[i] = position;

                // Real toy at the stop when it has final art, so the route reads
                // as "trace the rocket -> the fuel -> the snack", not numbered dots.
                // The number cue (above) and zone label (below) still frame it.
                var stopArt = ToyArtFor(rules, objectId);
                if (stopArt != null)
                {
                    var stopToy = new GameObject($"{TraceStepLabelName}{i}Toy", typeof(SpriteRenderer));
                    stopToy.transform.SetParent(zone.transform, false);
                    stopToy.transform.localScale = new Vector3(0.62f, 0.62f, 1f);
                    var stopToyRenderer = stopToy.GetComponent<SpriteRenderer>();
                    stopToyRenderer.sprite = stopArt;
                    stopToyRenderer.color = Color.white;
                    stopToyRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder + 5;
                }

                // Step number above the stop (non-color order cue, R19).
                AddWorldLabel(zone.transform, $"{TraceStepLabelName}{i}", (i + 1).ToString(), new Vector3(0f, 0.52f, 0f), 2.4f, ToyInteractionKit.ZoneSortingOrder + 43);

                var localId = objectId;
                var waypoint = zone.gameObject.AddComponent<StationWaypoint>();
                waypoint.Configure(localId, accent, () => submitWaypoint(localId));
            }

            // Route line: thin accent segments between consecutive stops, drawn
            // under the zones so the stops sit on the path.
            var routeRoot = new GameObject(TraceRouteName).transform;
            routeRoot.SetParent(kit.Root, false);
            for (var i = 0; i < count - 1; i++)
            {
                var a = positions[i];
                var b = positions[i + 1];
                var delta = b - a;
                var length = delta.magnitude;
                if (length < 0.001f)
                {
                    continue;
                }

                var segment = new GameObject($"{TraceRouteName}Seg{i}", typeof(SpriteRenderer));
                segment.transform.SetParent(routeRoot, false);
                segment.transform.localPosition = (a + b) * 0.5f;
                segment.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
                segment.transform.localScale = new Vector3(length, 0.08f, 1f);
                var segmentRenderer = segment.GetComponent<SpriteRenderer>();
                segmentRenderer.sprite = CampusWorldSprites.Square;
                var lineColor = accent;
                lineColor.a = 0.5f;
                segmentRenderer.color = lineColor;
                segmentRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder - 3;
            }
        }

        /// <summary>
        /// Design-review #3 ShootTarget range: parks the (single) shared goal
        /// zone at the top as the "Rescue Spot" target ring, lays a launch pad at
        /// the bottom, hides the kit's tray pieces, and fans the chain toys across
        /// the pad as pull-back-and-release launchers over the SAME drop seam. The
        /// player drags a toy back from the pad and lets go to fling it at the
        /// goal — aim + power, a distinct verb from dragging a token onto a pad.
        /// A short shot just bounces back (no harsh fail, R19); a shot that lands
        /// in the goal submits through the host-validated seam. Non-color cues:
        /// the target ring (shape), the pad (position), the aim guide while
        /// dragging (motion). Launchers ride their own objects under the kit root,
        /// so kit teardown still owns them.
        /// </summary>
        public static void MountLaunchRange(
            ToyInteractionKit kit,
            ToyPatternRules rules,
            Color accent,
            Func<string, DropSubmitResult> submitShot)
        {
            if (kit == null || rules == null || !kit.IsMounted || submitShot == null)
            {
                return;
            }

            var order = rules.DraggableObjectIds;
            var count = order.Count;
            if (count == 0)
            {
                return;
            }

            // ShootTarget launches the toys; the tray pieces are not dragged onto
            // pads. Hide every kit piece so only the pad launchers are interactable.
            foreach (var piece in kit.Pieces.Values)
            {
                if (piece != null)
                {
                    piece.gameObject.SetActive(false);
                }
            }

            // The one shared goal zone becomes the rescue-spot target ring at the
            // top. Repositioning the kit zone keeps its drop seam + label intact.
            var goalLocal = new Vector3(0f, 2.45f, 0f);
            var goalRadius = 0.85f;
            var goalZone = kit.ZoneFor(ToyPatternRules.GoalTargetId);
            if (goalZone != null)
            {
                goalZone.transform.localPosition = goalLocal;

                // Concentric target ring (shape cue): an accent disc with a paper
                // bullseye, drawn under the launch toys so a landed shot reads.
                var ring = new GameObject(LaunchGoalName, typeof(SpriteRenderer));
                ring.transform.SetParent(goalZone.transform, false);
                ring.transform.localScale = new Vector3(goalRadius * 2f, goalRadius * 2f, 1f);
                var ringRenderer = ring.GetComponent<SpriteRenderer>();
                ringRenderer.sprite = CampusWorldSprites.Circle;
                var ringColor = accent;
                ringColor.a = 0.45f;
                ringRenderer.color = ringColor;
                ringRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder - 2;

                var bullseye = new GameObject($"{LaunchGoalName}Inner", typeof(SpriteRenderer));
                bullseye.transform.SetParent(goalZone.transform, false);
                bullseye.transform.localScale = new Vector3(goalRadius, goalRadius, 1f);
                var bullRenderer = bullseye.GetComponent<SpriteRenderer>();
                bullRenderer.sprite = CampusWorldSprites.Circle;
                bullRenderer.color = PaperColor;
                bullRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder - 1;
            }

            // Launch pad at the bottom: a wide paper plate the toys sit on.
            var padLocal = new Vector3(0f, -2.3f, 0f);
            var pad = new GameObject(LaunchPadName, typeof(SpriteRenderer));
            pad.transform.SetParent(kit.Root, false);
            pad.transform.localPosition = padLocal;
            pad.transform.localScale = new Vector3(4.6f, 0.5f, 1f);
            var padRenderer = pad.GetComponent<SpriteRenderer>();
            padRenderer.sprite = CampusWorldSprites.Square;
            padRenderer.color = PadColor;
            padRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder - 3;

            // Fan the chain toys across the pad, each its own pull-back launcher.
            for (var i = 0; i < count; i++)
            {
                var objectId = order[i];
                var t = count <= 1 ? 0.5f : i / (float)(count - 1);
                var origin = new Vector3(Mathf.Lerp(-1.9f, 1.9f, t), padLocal.y + 0.55f, 0f);

                var toy = new GameObject($"{LaunchToyName}{i}", typeof(SpriteRenderer));
                toy.transform.SetParent(kit.Root, false);
                toy.transform.localPosition = origin;
                toy.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
                var toyRenderer = toy.GetComponent<SpriteRenderer>();
                // Launch the real toy (a battery, a wheel) when it has final art;
                // fall back to the accent token otherwise.
                var launchArt = ToyArtFor(rules, objectId);
                toyRenderer.sprite = launchArt != null ? launchArt : CampusWorldSprites.Circle;
                toyRenderer.color = launchArt != null ? Color.white : accent;
                toy.transform.localScale = launchArt != null ? new Vector3(0.95f, 0.95f, 1f) : toy.transform.localScale;
                toyRenderer.sortingOrder = ToyInteractionKit.PieceSortingOrder;

                AddWorldLabel(toy.transform, TokenLabelName, ObjectDisplayName(rules, objectId), new Vector3(0f, -0.62f, 0f), 1.4f, ToyInteractionKit.PieceSortingOrder + 2);

                var localId = objectId;
                var launcher = toy.AddComponent<StationLauncher>();
                launcher.Configure(localId, accent, origin, goalLocal, goalRadius, () => submitShot(localId));
            }
        }

        /// <summary>
        /// Design-review #3 DeduceAnswer board: lays every candidate (the false
        /// CoreTask cards AND the one true Clue answer) in a row, hides the kit's
        /// tray pieces, and makes each card a tap-to-cross-out target over the
        /// SAME drop seam. Tapping a false candidate crosses it out (the rules
        /// accept it); tapping the true answer bounces gently (the rules reject it
        /// — it has no cross zone), so the player deduces by elimination until one
        /// card survives. The board is answer-agnostic: it draws an X on accept
        /// and shakes on reject, and the survivor is whatever stays uncrossed.
        /// Pointer-first, non-color cues (the X mark + a clue banner), no harsh
        /// fail (R19). Cards ride their own objects under the kit root, so kit
        /// teardown still owns them.
        /// </summary>
        public static void MountDeduceBoard(
            ToyInteractionKit kit,
            ToyPatternRules rules,
            Color accent,
            Func<string, DropSubmitResult> submitCandidate)
        {
            if (kit == null || rules == null || !kit.IsMounted || submitCandidate == null)
            {
                return;
            }

            // The candidates are every chain object: the false CoreTask cards
            // (the eliminate-chain) plus the one true Clue answer (the survivor).
            var candidates = new List<string>();
            foreach (var definition in rules.Objects)
            {
                if (definition != null
                    && (definition.Role == PartyStationObjectRole.CoreTask
                        || definition.Role == PartyStationObjectRole.Clue))
                {
                    candidates.Add(definition.ObjectId);
                }
            }

            var count = candidates.Count;
            if (count == 0)
            {
                return;
            }

            // DeduceAnswer taps the cards; the tray pieces are not dragged. Hide
            // every kit piece so only the candidate cards are interactable.
            foreach (var piece in kit.Pieces.Values)
            {
                if (piece != null)
                {
                    piece.gameObject.SetActive(false);
                }
            }

            var boardRoot = new GameObject(DeduceBoardName).transform;
            boardRoot.SetParent(kit.Root, false);

            // Clue banner (non-color cue: text rule the player deduces against).
            AddWorldLabel(boardRoot, DeduceClueName, "Cross out the ones that don't fit!", new Vector3(0f, 2.3f, 0f), 2.1f, ToyInteractionKit.ZoneSortingOrder + 44);

            var cardSize = new Vector2(1.7f, 2.1f);
            for (var i = 0; i < count; i++)
            {
                var objectId = candidates[i];
                var t = count <= 1 ? 0.5f : i / (float)(count - 1);
                var position = new Vector3(Mathf.Lerp(-3.4f, 3.4f, t), 0.2f, 0f);

                // Card root stays unit-scaled (so labels/X marks aren't stretched);
                // the paper face is a sized child and the collider matches it.
                var card = new GameObject($"{DeduceCardName}{i}", typeof(BoxCollider2D));
                card.transform.SetParent(boardRoot, false);
                card.transform.localPosition = position;
                card.GetComponent<BoxCollider2D>().size = cardSize;

                var face = new GameObject("Face", typeof(SpriteRenderer));
                face.transform.SetParent(card.transform, false);
                face.transform.localScale = new Vector3(cardSize.x, cardSize.y, 1f);
                var faceRenderer = face.GetComponent<SpriteRenderer>();
                faceRenderer.sprite = CampusWorldSprites.Square;
                faceRenderer.color = PaperColor;
                faceRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder;

                // Real toy on the card face when it has final art, so each
                // candidate reads as a picture you cross out, not a name string.
                // Added AFTER the face so StationCandidate.MarkCrossed still dims
                // the face (its first child SpriteRenderer). The X mark (+6) draws
                // over the toy (+1); the name drops to a caption below the picture.
                var cardArt = ToyArtFor(rules, objectId);
                if (cardArt != null)
                {
                    var cardToy = new GameObject($"{DeduceCardName}{i}Toy", typeof(SpriteRenderer));
                    cardToy.transform.SetParent(card.transform, false);
                    cardToy.transform.localPosition = new Vector3(0f, 0.32f, 0f);
                    cardToy.transform.localScale = new Vector3(1.15f, 1.15f, 1f);
                    var cardToyRenderer = cardToy.GetComponent<SpriteRenderer>();
                    cardToyRenderer.sprite = cardArt;
                    cardToyRenderer.color = Color.white;
                    cardToyRenderer.sortingOrder = ToyInteractionKit.ZoneSortingOrder + 1;
                }

                var nameLabelY = cardArt != null ? -0.74f : 0f;
                AddWorldLabel(card.transform, TokenLabelName, ObjectDisplayName(rules, objectId), new Vector3(0f, nameLabelY, 0f), 1.5f, ToyInteractionKit.ZoneSortingOrder + 2);

                var localId = objectId;
                var candidate = card.AddComponent<StationCandidate>();
                candidate.Configure(localId, accent, cardSize, () => submitCandidate(localId));
            }
        }

        /// <summary>
        /// Kid-readable label for a rule target, derived from the seed objects
        /// (slot/mark/meter targets name their toy; shared targets name the
        /// pattern verb). Never an internal target id string.
        /// </summary>
        public static string TargetLabelFor(ToyPatternRules rules, string targetId)
        {
            if (rules == null || string.IsNullOrEmpty(targetId))
            {
                return string.Empty;
            }

            if (targetId.StartsWith(ToyPatternRules.SlotTargetPrefix, System.StringComparison.Ordinal))
            {
                return ObjectDisplayName(rules, targetId.Substring(ToyPatternRules.SlotTargetPrefix.Length));
            }

            if (targetId.StartsWith(ToyPatternRules.MarkTargetPrefix, System.StringComparison.Ordinal))
            {
                return ObjectDisplayName(rules, targetId.Substring(ToyPatternRules.MarkTargetPrefix.Length));
            }

            if (targetId.StartsWith(ToyPatternRules.MeterTargetPrefix, System.StringComparison.Ordinal))
            {
                return ObjectDisplayName(rules, targetId.Substring(ToyPatternRules.MeterTargetPrefix.Length));
            }

            if (targetId.StartsWith(ToyPatternRules.WaypointTargetPrefix, System.StringComparison.Ordinal))
            {
                return ObjectDisplayName(rules, targetId.Substring(ToyPatternRules.WaypointTargetPrefix.Length));
            }

            if (targetId.StartsWith(ToyPatternRules.CrossTargetPrefix, System.StringComparison.Ordinal))
            {
                return ObjectDisplayName(rules, targetId.Substring(ToyPatternRules.CrossTargetPrefix.Length));
            }

            if (targetId.StartsWith(ToyPatternRules.BinTargetPrefix, System.StringComparison.Ordinal))
            {
                var group = targetId.Substring(ToyPatternRules.BinTargetPrefix.Length).Replace('_', ' ');
                return group.Length == 0 ? "Bin" : char.ToUpperInvariant(group[0]) + group.Substring(1);
            }

            switch (targetId)
            {
                case ToyPatternRules.TrioTrayTargetId:
                    return "Match Tray";
                case ToyPatternRules.SequenceTargetId:
                    return "Next Step";
                case ToyPatternRules.ComposeTargetId:
                    return "Mix Spot";
                case ToyPatternRules.CareTargetId:
                    return "Care Spot";
                case ToyPatternRules.BuildTargetId:
                    return "Build Spot";
                case ToyPatternRules.GoalTargetId:
                    return "Rescue Spot";
                default:
                    return "Drop Spot";
            }
        }

        private static string ObjectDisplayName(ToyPatternRules rules, string objectId)
        {
            foreach (var definition in rules.Objects)
            {
                if (definition.ObjectId == objectId)
                {
                    return definition.DisplayName;
                }
            }

            return objectId.Replace('_', ' ');
        }

        /// <summary>
        /// The toy SpriteKey for an object id, if it has real FINAL art. Returns
        /// null when the object is unknown or still a placeholder, so the
        /// new-verb playfields (launcher / trace stop / deduce card) layer the
        /// real toy on top of their affordance only when art exists, and fall
        /// back to the bare token shape otherwise.
        /// </summary>
        private static Sprite ToyArtFor(ToyPatternRules rules, string objectId)
        {
            foreach (var definition in rules.Objects)
            {
                if (definition.ObjectId == objectId && !IsPlaceholderToySprite(definition.SpriteKey))
                {
                    return ResolveToySprite(definition.SpriteKey);
                }
            }

            return null;
        }

        private static Vector3 SpreadPosition(int index, int count, float spacing, float y)
        {
            var safeCount = Mathf.Max(1, count);
            var x = (index - (safeCount - 1) * 0.5f) * spacing;
            return new Vector3(x, y, 0f);
        }

        internal static void AddShape(Transform parent, string name, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            var shape = new GameObject(name, typeof(SpriteRenderer));
            shape.transform.SetParent(parent, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;
            var renderer = shape.GetComponent<SpriteRenderer>();
            renderer.sprite = CampusWorldSprites.Square;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        internal static void AddWorldLabel(Transform parent, string name, string text, Vector3 localPosition, float fontSize, int sortingOrder)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            var labelObject = new GameObject(name, typeof(TextMeshPro));
            labelObject.transform.SetParent(parent, false);
            labelObject.transform.localPosition = localPosition;

            var label = labelObject.GetComponent<TextMeshPro>();
            label.rectTransform.sizeDelta = new Vector2(2f, 0.5f);
            label.font = TypeStyles.Resolve(TypeRole.Body, TypeWeight.SemiBold);
            label.fontSize = fontSize;
            label.color = InkColor;
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.text = text;
            label.GetComponent<MeshRenderer>().sortingOrder = sortingOrder;
        }
    }

    /// <summary>
    /// U5 pointer-first meter dial (R19): rides a meter drop zone and turns it
    /// into a tap-to-tune toy. Every tap steps the requested value by
    /// <see cref="TapStep"/> (wrapping past the top back to the bottom, so the
    /// green band is always reachable — never a fail state) and submits through
    /// the station's TrySubmitDrop seam; the widget renders the authoritative
    /// value it reads back, never an optimistic local one.
    ///
    /// Non-color-only cues (R19): the green band is a raised notch on the
    /// track (shape), the needle position shows the value (position), the
    /// "In the green!" stamp appears on success (text), and the zone pulses
    /// while out of band (motion). The pointer handler is a thin wrapper over
    /// <see cref="Tap"/>, the seam the tests drive (house idiom).
    /// </summary>
    public class StationMeterWidget : MonoBehaviour, IPointerClickHandler
    {
        public const int TapStep = 15;
        public const float TrackWidth = 1.05f;

        public const string TrackName = "MeterTrack";
        public const string GreenBandName = "MeterGreenBand";
        public const string NeedleName = "MeterNeedle";
        public const string CheckLabelName = "MeterCheckLabel";
        public const string TapRingName = "MeterTapRing";

        private static readonly Color TrackInk = new(0.27f, 0.36f, 0.4f, 0.55f);
        private static readonly Color BandGreen = new(0.45f, 0.78f, 0.5f, 0.95f);
        private static readonly Color NeedleInk = new(0.098f, 0.196f, 0.235f);

        private Func<int> _valueFor;
        private Func<int, DropSubmitResult> _submit;
        private Color _accent;
        private SpriteRenderer _needle;
        private SpriteRenderer _tapRing;
        private GameObject _checkLabel;
        private int _renderedValue = int.MinValue;
        private float _pulseElapsed;

        /// <summary>Real-time clock toggle (house idiom). Tests set false and drive Tick.</summary>
        public bool AutoTick { get; set; } = true;

        public string MeterId { get; private set; }

        public int Value => _valueFor?.Invoke() ?? ToyPatternRules.MeterStartValue;

        public bool IsInGreen => Value >= ToyPatternRules.MeterGreenMin && Value <= ToyPatternRules.MeterGreenMax;

        public static StationMeterWidget Mount(
            GameObject zoneObject,
            string meterId,
            Color accent,
            Func<int> valueFor,
            Func<int, DropSubmitResult> submit)
        {
            if (zoneObject == null)
            {
                return null;
            }

            var widget = zoneObject.AddComponent<StationMeterWidget>();
            widget.MeterId = meterId;
            widget._valueFor = valueFor;
            widget._submit = submit;
            widget._accent = accent;
            widget.BuildVisuals();
            widget.RefreshVisuals();
            return widget;
        }

        /// <summary>
        /// Pure tap rule: step up by <see cref="TapStep"/>, wrapping past the
        /// max back to the min. The band (21 wide) is wider than the step, so
        /// repeated taps always land inside it from any start value.
        /// </summary>
        public static int NextTapValue(int current)
        {
            var next = current + TapStep;
            return next > ToyPatternRules.MeterMax ? ToyPatternRules.MeterMin : next;
        }

        /// <summary>
        /// THE meter interaction seam: one tap-step submitted through the
        /// station drop seam. The pointer handler and the tests share it.
        /// Returns false when the surface rejected the adjustment (locked).
        /// </summary>
        public bool Tap()
        {
            if (_submit == null)
            {
                return false;
            }

            var result = _submit(NextTapValue(Value));
            if (result != DropSubmitResult.Accepted && result != DropSubmitResult.Pending)
            {
                return false;
            }

            AudioCueCatalog.TryPlay(AudioCueIds.ToyBell);
            RefreshVisuals();
            return true;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Tap();
        }

        /// <summary>Deterministic clock seam — out-of-band pulse + value polling.</summary>
        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds <= 0f)
            {
                return;
            }

            if (_renderedValue != Value)
            {
                RefreshVisuals();
            }

            if (_tapRing == null)
            {
                return;
            }

            if (IsInGreen)
            {
                return; // RefreshVisuals already hid the ring
            }

            _pulseElapsed += deltaSeconds;
            var wave = 0.5f + 0.5f * Mathf.Sin(_pulseElapsed * (2f * Mathf.PI / ToyHintPulse.PulseSeconds));
            var color = _accent;
            color.a = Mathf.Lerp(0.12f, 0.32f, wave);
            _tapRing.color = color;
        }

        private void Update()
        {
            if (AutoTick)
            {
                Tick(Time.deltaTime);
            }
        }

        private void BuildVisuals()
        {
            CreateShape(TrackName, CampusWorldSprites.Square, new Vector3(0f, 0.12f, 0f),
                new Vector3(TrackWidth, 0.09f, 1f), TrackInk, ToyInteractionKit.ZoneSortingOrder + 2);

            var bandCenter = (ToyPatternRules.MeterGreenMin + ToyPatternRules.MeterGreenMax) * 0.5f;
            CreateShape(GreenBandName, CampusWorldSprites.Square,
                new Vector3(ValueToX(bandCenter), 0.12f, 0f),
                new Vector3(TrackWidth * (ToyPatternRules.MeterGreenMax - ToyPatternRules.MeterGreenMin) / (float)ToyPatternRules.MeterMax, 0.17f, 1f),
                BandGreen, ToyInteractionKit.ZoneSortingOrder + 3);

            _needle = CreateShape(NeedleName, CampusWorldSprites.Square, new Vector3(ValueToX(Value), 0.12f, 0f),
                new Vector3(0.06f, 0.3f, 1f), NeedleInk, ToyInteractionKit.ZoneSortingOrder + 4);

            var ringStart = _accent;
            ringStart.a = 0.2f;
            _tapRing = CreateShape(TapRingName, CampusWorldSprites.Circle, Vector3.zero,
                new Vector3(1.45f, 1.2f, 1f), ringStart, ToyInteractionKit.ZoneSortingOrder - 3);

            PartyStationRenderer.AddWorldLabel(transform, CheckLabelName, "In the green!",
                new Vector3(0f, 0.42f, 0f), 1.5f, ToyInteractionKit.ZoneSortingOrder + 41);
            var label = transform.Find(CheckLabelName);
            _checkLabel = label != null ? label.gameObject : null;
        }

        private void RefreshVisuals()
        {
            _renderedValue = Value;

            if (_needle != null)
            {
                _needle.transform.localPosition = new Vector3(ValueToX(_renderedValue), 0.12f, 0f);
            }

            var inGreen = IsInGreen;
            if (_checkLabel != null)
            {
                _checkLabel.SetActive(inGreen);
            }

            if (_tapRing != null)
            {
                _tapRing.gameObject.SetActive(!inGreen);
            }
        }

        private static float ValueToX(float value)
        {
            return (Mathf.Clamp(value, ToyPatternRules.MeterMin, ToyPatternRules.MeterMax) / ToyPatternRules.MeterMax - 0.5f) * TrackWidth;
        }

        private SpriteRenderer CreateShape(string shapeName, Sprite sprite, Vector3 localPosition, Vector3 localScale, Color color, int sortingOrder)
        {
            var shape = new GameObject(shapeName, typeof(SpriteRenderer));
            shape.transform.SetParent(transform, false);
            shape.transform.localPosition = localPosition;
            shape.transform.localScale = localScale;
            var renderer = shape.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return renderer;
        }
    }

    /// <summary>
    /// Design-review #3 TracePath stop: one tappable waypoint on the route.
    /// Tapping submits this waypoint through the station's drop seam; the rules
    /// accept it only when it is the next stop (out-of-order taps bounce gently,
    /// never a fail). The pointer handler is a thin wrapper over <see cref="Tap"/>,
    /// the seam the tests drive (house idiom, mirrors StationMeterWidget).
    /// </summary>
    public class StationWaypoint : MonoBehaviour, IPointerClickHandler
    {
        public const string ReachedMarkName = "TraceStopReached";

        private string _objectId;
        private Color _accent;
        private Func<DropSubmitResult> _submit;
        private bool _reached;

        public string ObjectId => _objectId;
        public bool Reached => _reached;

        public void Configure(string objectId, Color accent, Func<DropSubmitResult> submit)
        {
            _objectId = objectId;
            _accent = accent;
            _submit = submit;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Tap();
        }

        /// <summary>THE waypoint seam (pointer + tests share it). True when the stop was accepted.</summary>
        public bool Tap()
        {
            if (_submit == null || _reached)
            {
                return false;
            }

            var result = _submit();
            if (result != DropSubmitResult.Accepted && result != DropSubmitResult.Pending)
            {
                return false; // out-of-order tap: the rules bounced it, gently
            }

            _reached = true;
            AudioCueCatalog.TryPlay(AudioCueIds.ToyBell);
            MarkReached();
            return true;
        }

        private void MarkReached()
        {
            var pieceRenderer = GetComponent<SpriteRenderer>();
            var mark = new GameObject(ReachedMarkName, typeof(SpriteRenderer));
            mark.transform.SetParent(transform, false);
            mark.transform.localScale = new Vector3(0.42f, 0.42f, 1f);
            var renderer = mark.GetComponent<SpriteRenderer>();
            renderer.sprite = CampusWorldSprites.Circle;
            renderer.color = _accent;
            renderer.sortingOrder = (pieceRenderer != null ? pieceRenderer.sortingOrder : ToyInteractionKit.ZoneSortingOrder) + 5;
        }
    }

    /// <summary>
    /// Design-review #3 ShootTarget launcher: one pull-back-and-release toy on
    /// the launch pad. Dragging the toy back from its pad origin loads aim +
    /// power; releasing flings it the opposite way — a shot that lands within
    /// the goal radius submits through the station's drop seam, a short/wide
    /// shot bounces gently back to the pad (never a fail). The flight model is
    /// deliberately simple and pure (landing = origin − pull), so the launch
    /// decision is deterministic and the tests drive the same <see cref="Launch"/>
    /// seam the pointer does (house idiom, mirrors <see cref="StationWaypoint.Tap"/>
    /// and StationMeterWidget). The spatial aim skill lives here; the rules only
    /// validate the toy onto the shared goal.
    /// </summary>
    public class StationLauncher : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public const string AimGuideName = "LaunchAimGuide";
        public const string ScoredMarkName = "LaunchScored";

        /// <summary>Max pull-back distance (world units) — clamps a wild yank.</summary>
        public const float MaxPull = 6f;

        private string _objectId;
        private Color _accent;
        private Vector3 _originLocal;
        private Vector3 _goalLocal;
        private float _goalRadius;
        private Func<DropSubmitResult> _submit;
        private bool _scored;
        private SpriteRenderer _aimGuide;

        public string ObjectId => _objectId;
        public bool Scored => _scored;

        /// <summary>
        /// The exact pull that lands a shot dead-center in the goal — drives the
        /// golden/test path so a launch is a guaranteed hit without a camera.
        /// </summary>
        public Vector2 PerfectPull => (Vector2)(_originLocal - _goalLocal);

        public void Configure(string objectId, Color accent, Vector3 originLocal, Vector3 goalLocal, float goalRadius, Func<DropSubmitResult> submit)
        {
            _objectId = objectId;
            _accent = accent;
            _originLocal = originLocal;
            _goalLocal = goalLocal;
            _goalRadius = goalRadius;
            _submit = submit;
        }

        /// <summary>Where a shot with this pull-back lands (pure: opposite the pull, same distance).</summary>
        public Vector2 LandingFor(Vector2 pull)
        {
            return (Vector2)_originLocal - pull;
        }

        public bool IsHit(Vector2 pull)
        {
            return Vector2.Distance(LandingFor(pull), _goalLocal) <= _goalRadius;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_scored)
            {
                return;
            }

            AudioCueCatalog.TryPlay(AudioCueIds.DragPickup);
            EnsureAimGuide();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_scored)
            {
                return;
            }

            var pull = PullFromPointer(eventData);
            transform.localPosition = _originLocal - (Vector3)pull;
            UpdateAimGuide(pull);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_scored)
            {
                return;
            }

            var pull = PullFromPointer(eventData);
            ClearAimGuide();
            Launch(pull);
        }

        /// <summary>
        /// THE launch seam (pointer + tests share it). Returns true when the shot
        /// landed in the goal AND the rules accepted it; a miss or a bounced
        /// submit returns the toy to the pad, gently.
        /// </summary>
        public bool Launch(Vector2 pull)
        {
            if (_submit == null || _scored)
            {
                return false;
            }

            if (!IsHit(pull))
            {
                ReturnToPad();
                AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
                return false; // short/wide shot: bounce back, never a fail
            }

            var result = _submit();
            if (result != DropSubmitResult.Accepted && result != DropSubmitResult.Pending)
            {
                ReturnToPad();
                return false;
            }

            _scored = true;
            transform.localPosition = _goalLocal;
            AudioCueCatalog.TryPlay(AudioCueIds.ToyBell);
            MarkScored();
            return true;
        }

        private Vector2 PullFromPointer(PointerEventData eventData)
        {
            var camera = Camera.main;
            if (camera == null || transform.parent == null)
            {
                return Vector2.zero;
            }

            var screen = new Vector3(eventData.position.x, eventData.position.y, -camera.transform.position.z);
            var world = camera.ScreenToWorldPoint(screen);
            var local = transform.parent.InverseTransformPoint(world);
            var pull = (Vector2)(_originLocal - local);
            return Vector2.ClampMagnitude(pull, MaxPull);
        }

        private void EnsureAimGuide()
        {
            if (_aimGuide != null || transform.parent == null)
            {
                return;
            }

            var guide = new GameObject(AimGuideName, typeof(SpriteRenderer));
            guide.transform.SetParent(transform.parent, false);
            _aimGuide = guide.GetComponent<SpriteRenderer>();
            _aimGuide.sprite = CampusWorldSprites.Square;
            var color = _accent;
            color.a = 0.4f;
            _aimGuide.color = color;
            _aimGuide.sortingOrder = ToyInteractionKit.ZoneSortingOrder - 2;
        }

        private void UpdateAimGuide(Vector2 pull)
        {
            if (_aimGuide == null)
            {
                return;
            }

            var landing = LandingFor(pull);
            var a = (Vector2)_originLocal;
            var delta = landing - a;
            var length = delta.magnitude;
            if (length < 0.001f)
            {
                _aimGuide.enabled = false;
                return;
            }

            _aimGuide.enabled = true;
            var t = _aimGuide.transform;
            t.localPosition = (Vector3)((a + landing) * 0.5f);
            t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
            t.localScale = new Vector3(length, 0.06f, 1f);
        }

        private void ClearAimGuide()
        {
            if (_aimGuide != null)
            {
                Destroy(_aimGuide.gameObject);
                _aimGuide = null;
            }
        }

        private void ReturnToPad()
        {
            transform.localPosition = _originLocal;
        }

        private void MarkScored()
        {
            var pieceRenderer = GetComponent<SpriteRenderer>();
            var mark = new GameObject(ScoredMarkName, typeof(SpriteRenderer));
            mark.transform.SetParent(transform, false);
            mark.transform.localScale = new Vector3(1.25f, 1.25f, 1f);
            var renderer = mark.GetComponent<SpriteRenderer>();
            renderer.sprite = CampusWorldSprites.Circle;
            var glow = _accent;
            glow.a = 0.5f;
            renderer.color = glow;
            renderer.sortingOrder = (pieceRenderer != null ? pieceRenderer.sortingOrder : ToyInteractionKit.PieceSortingOrder) - 1;
        }
    }

    /// <summary>
    /// Design-review #3 DeduceAnswer card: one tappable candidate on the board.
    /// Tapping submits this candidate's cross target through the station's drop
    /// seam; the rules accept a FALSE candidate (it crosses out) and reject the
    /// true answer (no cross zone -> gentle bounce, "that one's true, keep it!").
    /// The pointer handler is a thin wrapper over <see cref="Tap"/>, the seam the
    /// tests drive (house idiom, mirrors StationWaypoint.Tap()). The card is
    /// answer-agnostic — it draws an X on accept and shakes on reject; whichever
    /// card stays uncrossed is the deduced answer.
    /// </summary>
    public class StationCandidate : MonoBehaviour, IPointerClickHandler
    {
        public const string CrossMarkName = "DeduceCrossMark";

        private string _objectId;
        private Color _accent;
        private Vector2 _cardSize;
        private Func<DropSubmitResult> _submit;
        private bool _crossed;

        public string ObjectId => _objectId;
        public bool Crossed => _crossed;

        public void Configure(string objectId, Color accent, Vector2 cardSize, Func<DropSubmitResult> submit)
        {
            _objectId = objectId;
            _accent = accent;
            _cardSize = cardSize;
            _submit = submit;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Tap();
        }

        /// <summary>THE candidate seam (pointer + tests share it). True when the card was crossed out.</summary>
        public bool Tap()
        {
            if (_submit == null || _crossed)
            {
                return false;
            }

            var result = _submit();
            if (result != DropSubmitResult.Accepted && result != DropSubmitResult.Pending)
            {
                // The true answer (or a locked board): bounce gently, keep it.
                AudioCueCatalog.TryPlay(AudioCueIds.DropReject);
                return false;
            }

            _crossed = true;
            AudioCueCatalog.TryPlay(AudioCueIds.ToyBell);
            MarkCrossed();
            return true;
        }

        private void MarkCrossed()
        {
            // Two crossed bars over the card (shape cue, not color-only) + a dim.
            var faceRenderer = GetComponentInChildren<SpriteRenderer>();
            if (faceRenderer != null)
            {
                var dim = faceRenderer.color;
                dim.a *= 0.5f;
                faceRenderer.color = dim;
            }

            var sortingOrder = (faceRenderer != null ? faceRenderer.sortingOrder : ToyInteractionKit.ZoneSortingOrder) + 6;
            var diagonal = Mathf.Sqrt(_cardSize.x * _cardSize.x + _cardSize.y * _cardSize.y);
            var angle = Mathf.Atan2(_cardSize.y, _cardSize.x) * Mathf.Rad2Deg;
            AddCrossBar(diagonal, angle, sortingOrder);
            AddCrossBar(diagonal, -angle, sortingOrder);
        }

        private void AddCrossBar(float length, float angle, int sortingOrder)
        {
            var bar = new GameObject(CrossMarkName, typeof(SpriteRenderer));
            bar.transform.SetParent(transform, false);
            bar.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            bar.transform.localScale = new Vector3(length, 0.16f, 1f);
            var renderer = bar.GetComponent<SpriteRenderer>();
            renderer.sprite = CampusWorldSprites.Square;
            renderer.color = _accent;
            renderer.sortingOrder = sortingOrder;
        }
    }
}
