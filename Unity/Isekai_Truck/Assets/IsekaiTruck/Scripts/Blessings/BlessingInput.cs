using UnityEngine;
using UnityEngine.InputSystem;

namespace IsekaiTruck.Blessings
{
    [DisallowMultipleComponent]
    public sealed class BlessingInput : MonoBehaviour
    {
        private BlessingEffectSystem effectSystem;

        public void Initialize(BlessingEffectSystem effects)
        {
            effectSystem = effects;
        }

        public void ReadInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame || keyboard.numpad1Key.wasPressedThisFrame) effectSystem.TryActivate(0);
            if (keyboard.digit2Key.wasPressedThisFrame || keyboard.numpad2Key.wasPressedThisFrame) effectSystem.TryActivate(1);
            if (keyboard.digit3Key.wasPressedThisFrame || keyboard.numpad3Key.wasPressedThisFrame) effectSystem.TryActivate(2);
        }
    }
}
