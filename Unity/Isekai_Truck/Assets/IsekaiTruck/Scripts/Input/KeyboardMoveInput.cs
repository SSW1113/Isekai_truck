using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace IsekaiTruck.Input
{
    [DisallowMultipleComponent]
    public sealed class KeyboardMoveInput : MonoBehaviour
    {
        private Vector2 move;
        private bool isInputEnabled = true;

        public Vector2 Move => move;

        public event Action<Vector2> InputChanged;

        private void Update()
        {
            if (!isInputEnabled)
            {
                return;
            }

            Vector2 nextMove = ReadMove();
            if (nextMove == move)
            {
                return;
            }

            move = nextMove;
            InputChanged?.Invoke(move);
        }

        public void SetInputEnabled(bool isEnabled)
        {
            if (isInputEnabled == isEnabled)
            {
                return;
            }

            isInputEnabled = isEnabled;
            if (!isEnabled && move != Vector2.zero)
            {
                move = Vector2.zero;
                InputChanged?.Invoke(move);
            }
        }

        private static Vector2 ReadMove()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return Vector2.zero;
            }

            float horizontal = (keyboard.dKey.isPressed ? 1f : 0f) - (keyboard.aKey.isPressed ? 1f : 0f);
            float vertical = (keyboard.sKey.isPressed ? 1f : 0f) - (keyboard.wKey.isPressed ? 1f : 0f);
            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }

#if UNITY_EDITOR
        public void SetMoveForVerification(Vector2 verificationMove)
        {
            move = Vector2.ClampMagnitude(verificationMove, 1f);
            InputChanged?.Invoke(move);
        }
#endif
    }
}
