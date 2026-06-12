using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CareerQuest
{
    /// <summary>
    /// U13 (P20) Escape pause menu — the baseline professional shell.
    ///
    /// Contracts:
    /// - UI-overlay only. NEVER touches Time.timeScale: sessions are networked
    ///   and a host "pause" must not freeze or desync the 2P partner. The world
    ///   keeps running behind the modal card.
    /// - Modal opt-in: like the ceremony overlay, the full-screen wash sets
    ///   raycastTarget = true so the screen beneath (drag pieces, doors, HUD
    ///   buttons) is unreachable while the menu is up (UiBuilder panels default
    ///   non-blocking per the U6 policy).
    /// - Volume sliders write PlayerPrefs (device settings only — R23: no
    ///   child data) and drive the AudioDirector tier setters live; persisted
    ///   values load on boot in AudioDirector.Awake.
    /// - Fullscreen toggles through the standard Screen APIs.
    ///   <see cref="FullscreenRequested"/> is the batchmode-safe test seam:
    ///   headless players accept the property write without a display change.
    /// - Exit to Title routes through CareerQuestApp.ExitToTitle (ceremony/
    ///   cinematic cancel + network shutdown + world re-route) — never a raw
    ///   scene reload.
    ///
    /// The overlay mounts as a child of the canvas root, so any route change
    /// (ResetRoot clears the root) destroys it; <see cref="IsOpen"/> reads the
    /// live object and self-heals to closed.
    /// </summary>
    public class PauseMenuController : MonoBehaviour
    {
        public const string OverlayName = "PauseMenuOverlay";
        public const string CardName = "PauseMenuCard";
        public const string ResumeButtonName = "PauseResumeButton";
        public const string SfxSliderName = "PauseSfxSlider";
        public const string MusicSliderName = "PauseMusicSlider";
        public const string FullscreenButtonName = "PauseFullscreenButton";
        public const string ExitToTitleButtonName = "PauseExitToTitleButton";

        private static readonly Color DimWash = new(0.08f, 0.11f, 0.22f, 0.6f);
        private static readonly Color Paper = new(1f, 0.969f, 0.878f);
        private static readonly Color PaperShadow = new(0.851f, 0.714f, 0.435f);
        private static readonly Color Ink = new(0.098f, 0.196f, 0.235f);
        private static readonly Color WorkshopTeal = new(0.055f, 0.42f, 0.435f);

        private CareerQuestApp _app;
        private AudioDirector _audio;
        private GameObject _overlay;
        private Slider _sfxSlider;
        private Slider _musicSlider;
        private TextMeshProUGUI _fullscreenLabel;

        /// <summary>House attach idiom — CareerQuestApp.Awake routes here.</summary>
        public static PauseMenuController AttachTo(GameObject host)
        {
            var controller = host.GetComponent<PauseMenuController>();
            if (controller == null)
            {
                controller = host.AddComponent<PauseMenuController>();
            }

            return controller;
        }

        public void Bind(CareerQuestApp app, AudioDirector audio)
        {
            _app = app;
            _audio = audio;
        }

        /// <summary>Live-object read: a route change that cleared the canvas root self-heals to closed.</summary>
        public bool IsOpen => _overlay != null;

        /// <summary>
        /// Batchmode-safe fullscreen seam: tests assert this property write
        /// path, never the actual display change.
        /// </summary>
        public bool FullscreenRequested { get; private set; }

        private void Awake()
        {
            FullscreenRequested = Screen.fullScreen;
        }

        /// <summary>
        /// Builds the modal paper-card menu under <paramref name="root"/>.
        /// Idempotent: opening while open is a no-op.
        /// </summary>
        public void Open(RectTransform root)
        {
            if (IsOpen || root == null)
            {
                return;
            }

            var overlayRect = UiBuilder.FullPanel(root, OverlayName, DimWash);
            _overlay = overlayRect.gameObject;
            // Modal opt-in (ceremony-overlay pattern): the wash blocks pointer
            // raycasts so nothing beneath is clickable while paused.
            _overlay.GetComponent<Image>().raycastTarget = true;

            var card = UiBuilder.Panel(overlayRect, CardName, Paper);
            UiBuilder.Place(card, 0f, 0f, 560f, 520f);

            var shadow = UiBuilder.Panel(overlayRect, $"{CardName}Shadow", new Color(PaperShadow.r, PaperShadow.g, PaperShadow.b, 0.65f));
            UiBuilder.Place(shadow, 6f, -8f, 560f, 520f);
            shadow.SetSiblingIndex(card.GetSiblingIndex()); // card stays on top

            var stripe = UiBuilder.Panel(card, "PauseMenuStripe", WorkshopTeal);
            UiBuilder.Place(stripe, 0f, 238f, 560f, 10f);

            var title = UiBuilder.Text(card, "PauseMenuTitle", "Paused", TypeStyles.ScreenTitle, TextAnchor.MiddleCenter, Ink, TypeRole.Display, TypeWeight.Bold);
            UiBuilder.Place(title.rectTransform, 0f, 192f, 480f, 48f);

            var resume = UiBuilder.Button(card, ResumeButtonName, "Resume", Close);
            UiBuilder.Place(resume.GetComponent<RectTransform>(), 0f, 126f, 240f, 56f); // ≥160x56 primary
            QuestStageUi.StylePrimaryButton(resume);

            _sfxSlider = MountVolumeRow(card, "Sounds", SfxSliderName, 44f, CurrentSfxVolume, SetSfxVolume);
            _musicSlider = MountVolumeRow(card, "Music", MusicSliderName, -46f, CurrentMusicVolume, SetMusicVolume);

            var fullscreen = UiBuilder.Button(card, FullscreenButtonName, FullscreenLabel(), ToggleFullscreen);
            UiBuilder.Place(fullscreen.GetComponent<RectTransform>(), 0f, -134f, 280f, 56f);
            QuestStageUi.StyleSecondaryButton(fullscreen);
            _fullscreenLabel = fullscreen.GetComponentInChildren<TextMeshProUGUI>();

            var exit = UiBuilder.Button(card, ExitToTitleButtonName, "Exit to Title", ExitToTitle);
            UiBuilder.Place(exit.GetComponent<RectTransform>(), 0f, -206f, 240f, 56f);
            QuestStageUi.StyleSecondaryButton(exit);
        }

        public void Close()
        {
            if (_overlay != null)
            {
                Destroy(_overlay);
            }

            _overlay = null;
            _sfxSlider = null;
            _musicSlider = null;
            _fullscreenLabel = null;
        }

        // ------------------------------------------------------------------
        // Settings seams (UI controls and PlayMode tests share these paths)
        // ------------------------------------------------------------------

        /// <summary>SFX tier: live AudioDirector apply + PlayerPrefs persist (device pref, R23).</summary>
        public void SetSfxVolume(float value)
        {
            var clamped = Mathf.Clamp01(value);
            Audio.SfxVolume = clamped;
            PlayerPrefs.SetFloat(AudioDirector.SfxVolumePrefKey, clamped);
            PlayerPrefs.Save();
            if (_sfxSlider != null) // Unity-aware null: a route change may have destroyed the menu
            {
                _sfxSlider.SetValueWithoutNotify(clamped);
            }
        }

        /// <summary>Music tier: independent of SFX (P20 tiers respond independently).</summary>
        public void SetMusicVolume(float value)
        {
            var clamped = Mathf.Clamp01(value);
            Audio.MusicVolume = clamped;
            PlayerPrefs.SetFloat(AudioDirector.MusicVolumePrefKey, clamped);
            PlayerPrefs.Save();
            if (_musicSlider != null)
            {
                _musicSlider.SetValueWithoutNotify(clamped);
            }
        }

        public void ToggleFullscreen()
        {
            SetFullscreen(!FullscreenRequested);
        }

        /// <summary>
        /// Standard screen APIs only. Entering fullscreen targets the desktop
        /// resolution in borderless FullScreenWindow (the kid-safe mode — no
        /// display mode switch); leaving restores the 1280x720 design window.
        /// Guards cover batchmode where the display reports no size.
        /// </summary>
        public void SetFullscreen(bool fullscreen)
        {
            FullscreenRequested = fullscreen;

            if (fullscreen)
            {
                var width = Display.main != null && Display.main.systemWidth > 0 ? Display.main.systemWidth : Screen.width;
                var height = Display.main != null && Display.main.systemHeight > 0 ? Display.main.systemHeight : Screen.height;
                if (width > 0 && height > 0)
                {
                    Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
                }
                else
                {
                    Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
                }
            }
            else
            {
                Screen.SetResolution(1280, 720, FullScreenMode.Windowed);
            }

            if (_fullscreenLabel != null)
            {
                _fullscreenLabel.text = FullscreenLabel();
            }
        }

        /// <summary>
        /// Routes through the app's single teardown path (ceremony/cinematic
        /// cancel, network shutdown when connected, world re-route to the entry
        /// title) — never a raw scene reload.
        /// </summary>
        public void ExitToTitle()
        {
            Close();
            if (_app != null)
            {
                _app.ExitToTitle();
            }
        }

        // ------------------------------------------------------------------
        // Internals
        // ------------------------------------------------------------------

        private AudioDirector Audio => _audio != null ? _audio : _audio = AudioDirector.Ensure();

        private static float CurrentSfxVolume => AudioDirector.Instance != null
            ? AudioDirector.Instance.SfxVolume
            : Mathf.Clamp01(PlayerPrefs.GetFloat(AudioDirector.SfxVolumePrefKey, 1f));

        private static float CurrentMusicVolume => AudioDirector.Instance != null
            ? AudioDirector.Instance.MusicVolume
            : Mathf.Clamp01(PlayerPrefs.GetFloat(AudioDirector.MusicVolumePrefKey, 1f));

        private string FullscreenLabel()
        {
            return FullscreenRequested ? "Fullscreen: On" : "Fullscreen: Off";
        }

        private Slider MountVolumeRow(RectTransform card, string label, string sliderName, float y, float initial, System.Action<float> onChanged)
        {
            var rowLabel = UiBuilder.Text(card, $"{sliderName}Label", label, 20, TextAnchor.MiddleLeft, Ink, TypeRole.Body, TypeWeight.SemiBold);
            UiBuilder.Place(rowLabel.rectTransform, -160f, y, 130f, 32f);

            var slider = UiBuilder.Slider(card, sliderName, initial, onChanged);
            UiBuilder.Place(slider.GetComponent<RectTransform>(), 60f, y, 300f, 44f); // kid-large hit area
            return slider;
        }
    }
}
