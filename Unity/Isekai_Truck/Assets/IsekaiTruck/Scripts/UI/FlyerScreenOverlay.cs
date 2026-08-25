using IsekaiTruck.Core;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Canvas))]
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class FlyerScreenOverlay : MonoBehaviour
    {
        [SerializeField] private RectTransform viewportRoot;
        [SerializeField] private Image flyerImage;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0.01f)] private float totalDuration = 5f;
        [SerializeField, Min(0.01f)] private float buildupDuration = 0.8f;
        [SerializeField, Min(0.01f)] private float fadeDuration = 1.5f;

        private UnityEngine.Camera targetCamera;
        private GameManager gameManager;
        private Rect currentViewport = new Rect(-1f, -1f, -1f, -1f);
        private float elapsed;
        private int frameIndex;
        private bool isComplete;
        private bool hasResolvedGameManager;

        public int FrameCount => frames?.Length ?? 0;
        public int CurrentFrameIndex => frameIndex;
        public float CurrentAlpha => canvasGroup != null ? canvasGroup.alpha : 0f;
        public float TotalDuration => totalDuration;
        public float BuildupDuration => buildupDuration;
        public float FadeDuration => fadeDuration;

        private void Awake()
        {
            ResolveReferences();
            ResetOverlay();
        }

        private void OnEnable()
        {
            ResetOverlay();
        }

        private void Update()
        {
            bool isPaused = Time.timeScale <= 0f || (gameManager != null && gameManager.IsMenuPaused);
            Advance(Time.unscaledDeltaTime, isPaused);
        }

        public void Advance(float deltaTime)
        {
            Advance(deltaTime, false);
        }

        public void Advance(float deltaTime, bool isPaused)
        {
            if (isComplete)
            {
                return;
            }

            ResolveReferences();
            ApplyViewport();
            if (isPaused)
            {
                return;
            }

            elapsed = Mathf.Min(elapsed + Mathf.Max(0f, deltaTime), totalDuration);
            UpdateFrame();
            UpdateAlpha();

            if (elapsed >= totalDuration)
            {
                Complete();
            }
        }

        private void ResolveReferences()
        {
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
            }

            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (!hasResolvedGameManager)
            {
                gameManager = FindFirstObjectByType<GameManager>();
                hasResolvedGameManager = true;
            }
        }

        private void ResetOverlay()
        {
            ResolveReferences();
            elapsed = 0f;
            frameIndex = 0;
            isComplete = false;
            currentViewport = new Rect(-1f, -1f, -1f, -1f);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }

            ApplyFrame();
            ApplyViewport();
        }

        private void UpdateFrame()
        {
            if (frames == null || frames.Length == 0)
            {
                return;
            }

            float safeBuildupDuration = Mathf.Max(0.01f, buildupDuration);
            float progress = Mathf.Clamp01(elapsed / safeBuildupDuration);
            int nextFrameIndex = Mathf.Min(
                Mathf.FloorToInt(progress * frames.Length),
                frames.Length - 1);
            if (frameIndex == nextFrameIndex)
            {
                return;
            }

            frameIndex = nextFrameIndex;
            ApplyFrame();
        }

        private void ApplyFrame()
        {
            if (flyerImage != null && frames != null && frames.Length > 0)
            {
                flyerImage.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            }
        }

        private void UpdateAlpha()
        {
            if (canvasGroup == null)
            {
                return;
            }

            float safeTotalDuration = Mathf.Max(0.01f, totalDuration);
            float safeFadeDuration = Mathf.Min(Mathf.Max(0.01f, fadeDuration), safeTotalDuration);
            float fadeStart = safeTotalDuration - safeFadeDuration;
            canvasGroup.alpha = elapsed <= fadeStart
                ? 1f
                : 1f - Mathf.InverseLerp(fadeStart, safeTotalDuration, elapsed);
        }

        private void ApplyViewport()
        {
            if (viewportRoot == null)
            {
                return;
            }

            Rect viewport = targetCamera != null ? targetCamera.rect : new Rect(0f, 0f, 1f, 1f);
            if (viewport == currentViewport)
            {
                return;
            }

            currentViewport = viewport;
            viewportRoot.anchorMin = viewport.min;
            viewportRoot.anchorMax = viewport.max;
            viewportRoot.offsetMin = Vector2.zero;
            viewportRoot.offsetMax = Vector2.zero;
        }

        private void Complete()
        {
            isComplete = true;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        public void Configure(
            RectTransform targetViewportRoot,
            Image targetImage,
            CanvasGroup targetCanvasGroup,
            Sprite[] animationFrames,
            float duration,
            float buildDuration,
            float alphaFadeDuration)
        {
            viewportRoot = targetViewportRoot;
            flyerImage = targetImage;
            canvasGroup = targetCanvasGroup;
            frames = animationFrames != null ? (Sprite[])animationFrames.Clone() : System.Array.Empty<Sprite>();
            totalDuration = Mathf.Max(0.01f, duration);
            buildupDuration = Mathf.Clamp(buildDuration, 0.01f, totalDuration);
            fadeDuration = Mathf.Clamp(alphaFadeDuration, 0.01f, totalDuration);
            ResetOverlay();
        }
#endif
    }
}
