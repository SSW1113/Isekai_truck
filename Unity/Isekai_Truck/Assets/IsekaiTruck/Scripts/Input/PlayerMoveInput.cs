using UnityEngine;

namespace IsekaiTruck.Input
{
    [DisallowMultipleComponent]
    public sealed class PlayerMoveInput : MonoBehaviour
    {
        [SerializeField] private JoystickInput joystickInput;
        [SerializeField] private KeyboardMoveInput keyboardInput;

        private ActiveInput activeInput;
        private bool isSubscribed;

        public Vector2 Move
        {
            get
            {
                if (joystickInput == null || !joystickInput.IsInputEnabled)
                {
                    return Vector2.zero;
                }

                return activeInput switch
                {
                    ActiveInput.Joystick => joystickInput.Move,
                    ActiveInput.Keyboard => keyboardInput.Move,
                    _ => Vector2.zero
                };
            }
        }

        private void Awake()
        {
            Subscribe();
        }

        private void HandleJoystickInputChanged(Vector2 move)
        {
            activeInput = ActiveInput.Joystick;
        }

        private void HandleKeyboardInputChanged(Vector2 move)
        {
            activeInput = ActiveInput.Keyboard;
        }

        private void HandleInputEnabledChanged(bool isEnabled)
        {
            activeInput = ActiveInput.None;
            keyboardInput.SetInputEnabled(isEnabled);
        }

        private void Subscribe()
        {
            if (isSubscribed || joystickInput == null || keyboardInput == null)
            {
                return;
            }

            joystickInput.InputChanged += HandleJoystickInputChanged;
            joystickInput.InputEnabledChanged += HandleInputEnabledChanged;
            keyboardInput.InputChanged += HandleKeyboardInputChanged;
            keyboardInput.SetInputEnabled(joystickInput.IsInputEnabled);
            isSubscribed = true;
        }

        private void Unsubscribe()
        {
            if (!isSubscribed)
            {
                return;
            }

            joystickInput.InputChanged -= HandleJoystickInputChanged;
            joystickInput.InputEnabledChanged -= HandleInputEnabledChanged;
            keyboardInput.InputChanged -= HandleKeyboardInputChanged;
            isSubscribed = false;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

#if UNITY_EDITOR
        public void SetReferences(JoystickInput targetJoystickInput, KeyboardMoveInput targetKeyboardInput)
        {
            Unsubscribe();
            joystickInput = targetJoystickInput;
            keyboardInput = targetKeyboardInput;
            activeInput = ActiveInput.None;
            Subscribe();
        }
#endif

        private enum ActiveInput
        {
            None,
            Joystick,
            Keyboard
        }
    }
}
