using TMPro;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class SpeedHUDView : MonoBehaviour
    {
        [SerializeField] private RectTransform visualRoot;
        [SerializeField] private TMP_Text speedText;
        [SerializeField, Range(1f, 2f)] private float maximumScale = 1.5f;
        [SerializeField, Min(1f)] private float scaleFollowSpeed = 10f;
        [SerializeField, Range(0.01f, 0.25f)] private float pulseStrength = 0.09f;
        [SerializeField, Min(0.05f)] private float pulseDuration = 0.16f;
        [SerializeField, Range(0.02f, 0.5f)] private float pulseStep = 0.1f;

        private float targetSpeedKmh;
        private float targetRatio;
        private float displayedScale = 1f;
        private float pulseElapsed = float.MaxValue;
        private int reachedPulseStep;

        private void Awake()
        {
            if (visualRoot == null)
            {
                visualRoot = (RectTransform)transform;
            }

            displayedScale = visualRoot.localScale.x;
        }

        private void Update()
        {
            float deltaTime = Time.unscaledDeltaTime;
            float targetScale = Mathf.Lerp(1f, maximumScale, targetRatio);
            float blend = 1f - Mathf.Exp(-scaleFollowSpeed * deltaTime);
            displayedScale = Mathf.Lerp(displayedScale, targetScale, blend);

            float pulseScale = 1f;
            if (pulseElapsed < pulseDuration)
            {
                pulseElapsed += deltaTime;
                float progress = Mathf.Clamp01(pulseElapsed / pulseDuration);
                pulseScale += Mathf.Sin(progress * Mathf.PI) * pulseStrength;
            }

            visualRoot.localScale = Vector3.one * Mathf.Min(maximumScale, displayedScale * pulseScale);
        }

        public void SetSpeed(float speedKmh, float speedRatio)
        {
            targetSpeedKmh = Mathf.Max(0f, speedKmh);
            targetRatio = Mathf.Clamp01(speedRatio);
            speedText.text = $"{Mathf.RoundToInt(targetSpeedKmh)} km/h";

            int currentStep = Mathf.FloorToInt(targetRatio / pulseStep);
            if (currentStep > reachedPulseStep && targetRatio < 1f)
            {
                pulseElapsed = 0f;
            }

            reachedPulseStep = currentStep;
        }

#if UNITY_EDITOR
        public void SetReferences(RectTransform targetVisualRoot, TMP_Text targetSpeedText)
        {
            visualRoot = targetVisualRoot;
            speedText = targetSpeedText;
        }
#endif
    }
}
