using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class UIFeedbackEffect : MonoBehaviour
    {
        [SerializeField, Min(0.05f)] private float duration = 0.28f;
        [SerializeField, Range(0.01f, 0.3f)] private float scaleStrength = 0.1f;

        private Vector3 restingScale;
        private float elapsedTime;
        private bool isPlaying;

        private void Awake()
        {
            restingScale = transform.localScale;
        }

        private void Update()
        {
            if (!isPlaying)
            {
                return;
            }

            elapsedTime += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsedTime / duration);
            float scale = 1f + Mathf.Sin(progress * Mathf.PI) * scaleStrength;
            transform.localScale = restingScale * scale;

            if (progress >= 1f)
            {
                StopEffect();
            }
        }

        private void OnDisable()
        {
            StopEffect();
        }

        public void Play()
        {
            if (restingScale == Vector3.zero)
            {
                restingScale = transform.localScale;
            }

            elapsedTime = 0f;
            isPlaying = true;
        }

#if UNITY_EDITOR
        public void Configure(float effectDuration, float effectScaleStrength)
        {
            duration = Mathf.Max(0.05f, effectDuration);
            scaleStrength = Mathf.Clamp(effectScaleStrength, 0.01f, 0.3f);
        }
#endif

        private void StopEffect()
        {
            isPlaying = false;
            elapsedTime = 0f;
            transform.localScale = restingScale;
        }
    }
}
