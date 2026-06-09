using UnityEngine;

namespace CareerQuest
{
    public class PlayerInputRouter : MonoBehaviour
    {
        [SerializeField] private PlayerControlScheme controlScheme = PlayerControlScheme.SoloKeyboardMouse;

        public PlayerControlScheme ControlScheme
        {
            get => controlScheme;
            set => controlScheme = value;
        }

        public PlayerControlPreset CurrentPreset => controlScheme switch
        {
            PlayerControlScheme.SplitKeyboardP1 => PlayerControlPreset.P1(),
            PlayerControlScheme.SplitKeyboardP2 => PlayerControlPreset.P2(),
            _ => PlayerControlPreset.Solo()
        };

        public Vector2 ReadMove()
        {
            var preset = CurrentPreset;
            var move = Vector2.zero;

            if (Input.GetKey(preset.Left))
            {
                move.x -= 1f;
            }

            if (Input.GetKey(preset.Right))
            {
                move.x += 1f;
            }

            if (Input.GetKey(preset.Down))
            {
                move.y -= 1f;
            }

            if (Input.GetKey(preset.Up))
            {
                move.y += 1f;
            }

            return move.sqrMagnitude > 1f ? move.normalized : move;
        }

        public bool ReadConfirmDown()
        {
            var preset = CurrentPreset;
            return Input.GetKeyDown(preset.Confirm) || (controlScheme == PlayerControlScheme.SoloKeyboardMouse && Input.GetMouseButtonDown(0));
        }

        public bool UsesMouse()
        {
            return controlScheme == PlayerControlScheme.SoloKeyboardMouse;
        }
    }
}
