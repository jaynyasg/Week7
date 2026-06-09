using UnityEngine;

namespace CareerQuest
{
    public enum PlayerControlScheme
    {
        SoloKeyboardMouse,
        SplitKeyboardP1,
        SplitKeyboardP2
    }

    public readonly struct PlayerControlPreset
    {
        public PlayerControlScheme Scheme { get; }
        public string Label { get; }
        public KeyCode Up { get; }
        public KeyCode Down { get; }
        public KeyCode Left { get; }
        public KeyCode Right { get; }
        public KeyCode Confirm { get; }

        public PlayerControlPreset(
            PlayerControlScheme scheme,
            string label,
            KeyCode up,
            KeyCode down,
            KeyCode left,
            KeyCode right,
            KeyCode confirm)
        {
            Scheme = scheme;
            Label = label;
            Up = up;
            Down = down;
            Left = left;
            Right = right;
            Confirm = confirm;
        }

        public static PlayerControlPreset Solo()
        {
            return new PlayerControlPreset(PlayerControlScheme.SoloKeyboardMouse, "Solo: WASD + mouse", KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.Mouse0);
        }

        public static PlayerControlPreset P1()
        {
            return new PlayerControlPreset(PlayerControlScheme.SplitKeyboardP1, "P1: WASD + F", KeyCode.W, KeyCode.S, KeyCode.A, KeyCode.D, KeyCode.F);
        }

        public static PlayerControlPreset P2()
        {
            return new PlayerControlPreset(PlayerControlScheme.SplitKeyboardP2, "P2: IJKL + Enter", KeyCode.I, KeyCode.K, KeyCode.J, KeyCode.L, KeyCode.Return);
        }
    }
}
