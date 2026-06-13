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
                if (piece == null || !IsPlaceholderToySprite(definition.SpriteKey))
                {
                    continue;
                }

                var renderer = piece.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.color = TokenColorFor(accent, index);
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
}
