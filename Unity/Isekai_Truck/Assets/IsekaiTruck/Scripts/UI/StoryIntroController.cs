using System;
using System.Collections;
using IsekaiTruck.Input;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class StoryIntroController : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform comicRoot;
        [SerializeField] private Image impactFlash;
        [SerializeField] private Button inputButton;
        [SerializeField] private Text promptText;
        [SerializeField] private StoryPanel[] panels;
        [SerializeField, Min(0.05f)] private float closeDuration = 0.22f;
        [SerializeField, Min(0f)] private float impactShakeStrength = 12f;

        private JoystickInput joystickInput;
        private Coroutine transitionRoutine;
        private Vector2 comicRestingPosition;
        private float previousTimeScale = 1f;
        private int revealedPanelCount;
        private bool isInitialized;
        private bool isOpen;
        private bool isTransitioning;
        private bool hasPausedTime;

        public event Action Completed;

        public bool IsOpen => isOpen;
        public int RevealedPanelCount => revealedPanelCount;
        public int PanelCount => panels != null ? panels.Length : 0;

        public void Initialize(JoystickInput input)
        {
            if (isInitialized)
            {
                return;
            }

            ValidateReferences();
            joystickInput = input;
            inputButton.onClick.AddListener(Advance);
            comicRestingPosition = comicRoot.anchoredPosition;

            for (int i = 0; i < panels.Length; i++)
            {
                panels[i].CaptureRestingState();
                panels[i].Hide();
            }

            revealedPanelCount = 0;
            isInitialized = true;
            isOpen = true;
            isTransitioning = false;
            overlayCanvasGroup.alpha = 1f;
            impactFlash.color = WithAlpha(impactFlash.color, 0f);
            promptText.text = "클릭하여 이야기를 시작하세요";
            transform.SetAsLastSibling();
            overlay.SetActive(true);
            inputButton.interactable = true;
            joystickInput.SetInputEnabled(false);
            PauseGameTime();
        }

        private void Advance()
        {
            if (!isOpen || isTransitioning)
            {
                return;
            }

            if (revealedPanelCount >= panels.Length)
            {
                transitionRoutine = StartCoroutine(CompleteIntro());
                return;
            }

            transitionRoutine = StartCoroutine(RevealPanel(panels[revealedPanelCount]));
        }

        private IEnumerator RevealPanel(StoryPanel panel)
        {
            isTransitioning = true;
            inputButton.interactable = false;
            panel.ShowAtStart();

            if (panel.Entrance == PanelEntrance.Impact)
            {
                yield return AnimateImpact(panel);
            }
            else
            {
                yield return AnimateSoftEntrance(panel);
            }

            panel.ApplyRestingState();
            revealedPanelCount++;
            RefreshPrompt();
            isTransitioning = false;
            inputButton.interactable = true;
            transitionRoutine = null;
        }

        private IEnumerator AnimateSoftEntrance(StoryPanel panel)
        {
            float elapsed = 0f;
            while (elapsed < panel.Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseOut(Mathf.Clamp01(elapsed / panel.Duration));
                panel.CanvasGroup.alpha = progress;
                panel.PanelRoot.anchoredPosition = panel.RestingPosition + Vector2.LerpUnclamped(panel.StartOffset, Vector2.zero, progress);
                yield return null;
            }
        }

        private IEnumerator AnimateImpact(StoryPanel panel)
        {
            float elapsed = 0f;
            Color flashColor = impactFlash.color;
            while (elapsed < panel.Duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float linearProgress = Mathf.Clamp01(elapsed / panel.Duration);
                float progress = EaseOut(linearProgress);
                float remaining = 1f - linearProgress;

                panel.CanvasGroup.alpha = Mathf.Clamp01(linearProgress * 6f);
                panel.PanelRoot.localScale = panel.RestingScale * Mathf.LerpUnclamped(1.42f, 1f, progress);
                comicRoot.anchoredPosition = comicRestingPosition + CalculateImpactOffset(elapsed, remaining);
                impactFlash.color = WithAlpha(flashColor, Mathf.Min(0.62f, remaining * 1.4f));
                yield return null;
            }

            comicRoot.anchoredPosition = comicRestingPosition;
            impactFlash.color = WithAlpha(flashColor, 0f);
        }

        private IEnumerator CompleteIntro()
        {
            isTransitioning = true;
            inputButton.interactable = false;
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseOut(Mathf.Clamp01(elapsed / closeDuration));
                overlayCanvasGroup.alpha = 1f - progress;
                yield return null;
            }

            overlayCanvasGroup.alpha = 0f;
            overlay.SetActive(false);
            isOpen = false;
            isTransitioning = false;
            RestoreGameTime();
            joystickInput.SetInputEnabled(true);
            transitionRoutine = null;
            Completed?.Invoke();
        }

        private void RefreshPrompt()
        {
            promptText.text = revealedPanelCount < panels.Length
                ? $"클릭하여 다음 장면 보기  {revealedPanelCount} / {panels.Length}"
                : "한 번 더 클릭하여 게임 안내 보기";
        }

        private Vector2 CalculateImpactOffset(float elapsed, float remaining)
        {
            return new Vector2(
                Mathf.Sin(elapsed * 118f),
                Mathf.Cos(elapsed * 97f)
            ) * (impactShakeStrength * remaining);
        }

        private void PauseGameTime()
        {
            if (hasPausedTime)
            {
                return;
            }

            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            hasPausedTime = true;
        }

        private void RestoreGameTime()
        {
            if (!hasPausedTime)
            {
                return;
            }

            Time.timeScale = previousTimeScale;
            hasPausedTime = false;
        }

        private void ValidateReferences()
        {
            if (overlay == null || overlayCanvasGroup == null || comicRoot == null || impactFlash == null ||
                inputButton == null || promptText == null || panels == null || panels.Length != 6)
            {
                throw new MissingReferenceException("스토리 인트로 참조가 올바르게 구성되지 않았습니다.");
            }

            for (int i = 0; i < panels.Length; i++)
            {
                if (!panels[i].IsValid)
                {
                    throw new MissingReferenceException($"스토리 인트로 {i + 1}번 패널 참조가 올바르지 않습니다.");
                }
            }
        }

        private void OnDestroy()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (inputButton != null)
            {
                inputButton.onClick.RemoveListener(Advance);
            }

            RestoreGameTime();
        }

        private static float EaseOut(float value)
        {
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

#if UNITY_EDITOR
        public void SetReferences(
            GameObject targetOverlay,
            CanvasGroup targetOverlayCanvasGroup,
            RectTransform targetComicRoot,
            Image targetImpactFlash,
            Button targetInputButton,
            Text targetPromptText,
            StoryPanel[] targetPanels)
        {
            overlay = targetOverlay;
            overlayCanvasGroup = targetOverlayCanvasGroup;
            comicRoot = targetComicRoot;
            impactFlash = targetImpactFlash;
            inputButton = targetInputButton;
            promptText = targetPromptText;
            panels = targetPanels;
        }
#endif

        public enum PanelEntrance
        {
            Slide,
            Fade,
            Impact
        }

        [Serializable]
        public sealed class StoryPanel
        {
            [SerializeField] private RectTransform panelRoot;
            [SerializeField] private CanvasGroup canvasGroup;
            [SerializeField] private PanelEntrance entrance;
            [SerializeField] private Vector2 startOffset;
            [SerializeField, Min(0.05f)] private float duration = 0.24f;

            private Vector2 restingPosition;
            private Vector3 restingScale;

            public RectTransform PanelRoot => panelRoot;
            public CanvasGroup CanvasGroup => canvasGroup;
            public PanelEntrance Entrance => entrance;
            public Vector2 StartOffset => startOffset;
            public float Duration => duration;
            public Vector2 RestingPosition => restingPosition;
            public Vector3 RestingScale => restingScale;
            public bool IsValid => panelRoot != null && canvasGroup != null;

            public StoryPanel(
                RectTransform targetPanelRoot,
                CanvasGroup targetCanvasGroup,
                PanelEntrance targetEntrance,
                Vector2 targetStartOffset,
                float targetDuration)
            {
                panelRoot = targetPanelRoot;
                canvasGroup = targetCanvasGroup;
                entrance = targetEntrance;
                startOffset = targetStartOffset;
                duration = targetDuration;
            }

            public void CaptureRestingState()
            {
                restingPosition = panelRoot.anchoredPosition;
                restingScale = panelRoot.localScale;
            }

            public void Hide()
            {
                panelRoot.gameObject.SetActive(false);
                canvasGroup.alpha = 0f;
            }

            public void ShowAtStart()
            {
                panelRoot.gameObject.SetActive(true);
                canvasGroup.alpha = 0f;
                panelRoot.anchoredPosition = restingPosition + startOffset;
                panelRoot.localScale = entrance == PanelEntrance.Impact ? restingScale * 1.42f : restingScale;
            }

            public void ApplyRestingState()
            {
                canvasGroup.alpha = 1f;
                panelRoot.anchoredPosition = restingPosition;
                panelRoot.localScale = restingScale;
            }
        }
    }
}
