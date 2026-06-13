using System;

namespace CareerQuest
{
    /// <summary>
    /// U9 (R19): the session-only classroom access settings — local, resettable,
    /// never persisted and never child data (KTD12). One object is the single
    /// source of truth for the access toggles; <see cref="CareerQuestApp"/> and
    /// the facilitator/pause controls write it, and the gameplay surfaces read it
    /// to soften motion/audio while preserving completion clarity.
    ///
    /// Threading model (the prompt's "flag path"):
    /// - <see cref="QuietMode"/> is reduced-motion + quiet-audio together (one
    ///   classroom toggle, design doc: "reduced-motion / quiet-classroom mode").
    /// - High-level held components get the flag pushed by CareerQuestApp on
    ///   <see cref="Changed"/> (CameraDirector.ReducedMotion, the accessory
    ///   spotlight's QuietMode).
    /// - Low-level components built by static factories with no session ref
    ///   (SceneWipe via RoomVeilController, AudioCueCatalog) read the static
    ///   <see cref="ReducedMotionActive"/> / <see cref="QuietAudioActive"/>
    ///   ambient gate, which this object mirrors whenever it changes. The static
    ///   gate is deterministic and test-resettable (<see cref="ResetStatics"/>).
    ///
    /// Completion clarity is never sacrificed: quiet mode suppresses flourish
    /// (camera tweens, the wipe lift animation, looping ambience, spotlight
    /// pulse) but keeps the state changes that make completion read — the room
    /// still covers/reveals, the result copy still shows, completion cues still
    /// fire (just at reduced intensity).
    /// </summary>
    public sealed class ClassroomAccessSettings
    {
        private bool _quietMode;
        private bool _pointerFirst = true;
        private bool _nonColorCues = true;
        private bool _earlyReaderCopy;

        /// <summary>Fired whenever any toggle changes (CareerQuestApp re-pushes the flag path).</summary>
        public event Action Changed;

        /// <summary>
        /// Reduced-motion + quiet-audio classroom mode. When true: camera
        /// flourish tweens snap, scene wipes skip their lift, looping ambience
        /// softens to a quiet floor, and the spotlight holds its pulse — while
        /// completion clarity is preserved.
        /// </summary>
        public bool QuietMode
        {
            get => _quietMode;
            set
            {
                if (_quietMode == value)
                {
                    return;
                }

                _quietMode = value;
                PushStatics();
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Pointer-first completion path (design doc: single pointer drag/click,
        /// no keyboard-only precision). Always on by default — kept as an
        /// explicit flag so facilitator controls/tests can assert the contract.
        /// </summary>
        public bool PointerFirst
        {
            get => _pointerFirst;
            set
            {
                if (_pointerFirst == value)
                {
                    return;
                }

                _pointerFirst = value;
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Non-color-only cues for match/sort/route decisions (shape + label +
        /// position alongside color). On by default; the toggle lets a facilitator
        /// force the secondary signals always-visible.
        /// </summary>
        public bool NonColorCues
        {
            get => _nonColorCues;
            set
            {
                if (_nonColorCues == value)
                {
                    return;
                }

                _nonColorCues = value;
                Changed?.Invoke();
            }
        }

        /// <summary>Early-reader copy toggle (shorter/simpler kid-facing lines where offered).</summary>
        public bool EarlyReaderCopy
        {
            get => _earlyReaderCopy;
            set
            {
                if (_earlyReaderCopy == value)
                {
                    return;
                }

                _earlyReaderCopy = value;
                Changed?.Invoke();
            }
        }

        /// <summary>
        /// Session reset (facilitator "return to defaults" / app teardown):
        /// clears every access toggle back to the calm-safe defaults and
        /// re-pushes the static gate. Session-only — touches no earned state.
        /// </summary>
        public void Reset()
        {
            _quietMode = false;
            _pointerFirst = true;
            _nonColorCues = true;
            _earlyReaderCopy = false;
            PushStatics();
            Changed?.Invoke();
        }

        private void PushStatics()
        {
            ReducedMotionActive = _quietMode;
            QuietAudioActive = _quietMode;
        }

        // ------------------------------------------------------------------
        // Static ambient gate — read by the static/factory-built surfaces
        // (SceneWipe, AudioCueCatalog) that have no session reference.
        // ------------------------------------------------------------------

        /// <summary>True when the active settings request reduced motion (SceneWipe reads this).</summary>
        public static bool ReducedMotionActive { get; private set; }

        /// <summary>True when the active settings request quiet audio (AudioCueCatalog reads this).</summary>
        public static bool QuietAudioActive { get; private set; }

        /// <summary>
        /// Test/teardown reset for the process-wide static gate so a quiet-mode
        /// test never leaks reduced motion into a later suite (mirrors the
        /// FirstRunGuideBeat.ResetSessionFlag idiom).
        /// </summary>
        public static void ResetStatics()
        {
            ReducedMotionActive = false;
            QuietAudioActive = false;
        }
    }
}
