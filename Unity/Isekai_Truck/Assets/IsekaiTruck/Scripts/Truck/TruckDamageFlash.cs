using UnityEngine;

namespace IsekaiTruck.Truck
{
    [DisallowMultipleComponent]
    public sealed class TruckDamageFlash : MonoBehaviour
    {
        private Renderer[] renderers;
        private bool[] initialEnabledStates;
        private float flashInterval;
        private float flashRemaining;
        private bool isVisible = true;
        private bool isFlashing;

        public bool IsFlashing => isFlashing;

        public void Initialize(float interval)
        {
            flashInterval = Mathf.Max(0.01f, interval);
            renderers = GetComponentsInChildren<Renderer>(true);
            initialEnabledStates = new bool[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
            {
                initialEnabledStates[i] = renderers[i].enabled;
            }

            StopFlashing();
        }

        public void StartFlashing()
        {
            isFlashing = true;
            flashRemaining = 0f;
            isVisible = true;
            SetVisible(true);
        }

        public void UpdateFlash(float deltaTime)
        {
            if (!isFlashing)
            {
                return;
            }

            flashRemaining -= Mathf.Max(0f, deltaTime);
            while (flashRemaining <= 0f)
            {
                flashRemaining += flashInterval;
                isVisible = !isVisible;
            }

            SetVisible(isVisible);
        }

        public void StopFlashing()
        {
            isFlashing = false;
            flashRemaining = 0f;
            isVisible = true;
            SetVisible(true);
        }

        private void SetVisible(bool shouldShow)
        {
            if (renderers == null || initialEnabledStates == null)
            {
                return;
            }

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].enabled = initialEnabledStates[i] && shouldShow;
                }
            }
        }

        private void OnDisable()
        {
            StopFlashing();
        }
    }
}
