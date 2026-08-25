using System;
using System.Collections;
using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class SystemGuidePopup : MonoBehaviour
    {
        [SerializeField] private GameObject overlay;
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private RectTransform panelRoot;
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private CanvasGroup panelCanvasGroup;
        [SerializeField] private RectTransform contentRoot;
        [SerializeField] private CanvasGroup contentCanvasGroup;
        [SerializeField] private Text categoryText;
        [SerializeField] private Text titleText;
        [SerializeField] private Text summaryText;
        [SerializeField] private GameObject[] tipCards;
        [SerializeField] private Text[] tipNumberTexts;
        [SerializeField] private Text[] tipTitleTexts;
        [SerializeField] private Text[] tipBodyTexts;
        [SerializeField] private Text pageIndicatorText;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text nextButtonText;
        [SerializeField] private GuidePage[] pages;
        [SerializeField, Min(0.05f)] private float openDuration = 0.22f;
        [SerializeField, Min(0.05f)] private float pageTransitionDuration = 0.14f;

        private JoystickInput joystickInput;
        private Coroutine transitionRoutine;
        private int currentPageIndex;
        private float previousTimeScale = 1f;
        private Vector3 panelRestingScale = Vector3.one;
        private bool isInitialized;
        private bool isOpen;
        private bool isTransitioning;
        private bool hasPausedTime;

        public bool IsOpen => isOpen;
        public int CurrentPageIndex => currentPageIndex;
        public int PageCount => pages != null ? pages.Length : 0;

        public void Initialize(JoystickInput input, CameraController cameraController)
        {
            if (isInitialized)
            {
                return;
            }

            ValidateReferences();
            joystickInput = input;
            previousButton.onClick.AddListener(ShowPreviousPage);
            nextButton.onClick.AddListener(ShowNextPage);
            SetViewport(cameraController.ViewportRect);

            currentPageIndex = 0;
            isInitialized = true;
            isOpen = true;
            isTransitioning = true;
            transform.SetAsLastSibling();
            overlay.SetActive(true);
            panelRestingScale = panelRoot.localScale;
            joystickInput.SetInputEnabled(false);
            PauseGameTime();
            ApplyPage();
            transitionRoutine = StartCoroutine(PlayOpen());
        }

        public void SetViewport(Rect viewport)
        {
            gameArea.anchorMin = viewport.min;
            gameArea.anchorMax = viewport.max;
            gameArea.offsetMin = Vector2.zero;
            gameArea.offsetMax = Vector2.zero;
        }

        private void ShowPreviousPage()
        {
            if (!isOpen || isTransitioning || currentPageIndex <= 0)
            {
                return;
            }

            transitionRoutine = StartCoroutine(ChangePage(currentPageIndex - 1, -1f));
        }

        private void ShowNextPage()
        {
            if (!isOpen || isTransitioning)
            {
                return;
            }

            if (currentPageIndex >= pages.Length - 1)
            {
                transitionRoutine = StartCoroutine(CompleteGuide());
                return;
            }

            transitionRoutine = StartCoroutine(ChangePage(currentPageIndex + 1, 1f));
        }

        private IEnumerator PlayOpen()
        {
            overlayCanvasGroup.alpha = 0f;
            panelCanvasGroup.alpha = 0f;
            panelRoot.localScale = panelRestingScale * 0.96f;
            SetNavigationInteractable(false);

            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseOut(Mathf.Clamp01(elapsed / openDuration));
                overlayCanvasGroup.alpha = progress;
                panelCanvasGroup.alpha = progress;
                panelRoot.localScale = panelRestingScale * Mathf.LerpUnclamped(0.96f, 1f, progress);
                yield return null;
            }

            overlayCanvasGroup.alpha = 1f;
            panelCanvasGroup.alpha = 1f;
            panelRoot.localScale = panelRestingScale;
            isTransitioning = false;
            RefreshNavigation();
            transitionRoutine = null;
        }

        private IEnumerator ChangePage(int targetPageIndex, float direction)
        {
            isTransitioning = true;
            SetNavigationInteractable(false);
            float halfDuration = pageTransitionDuration * 0.5f;
            yield return AnimateContent(1f, 0f, 0f, -24f * direction, halfDuration);

            currentPageIndex = targetPageIndex;
            ApplyPage();
            yield return AnimateContent(0f, 1f, 24f * direction, 0f, halfDuration);

            isTransitioning = false;
            RefreshNavigation();
            transitionRoutine = null;
        }

        private IEnumerator CompleteGuide()
        {
            isTransitioning = true;
            SetNavigationInteractable(false);
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / openDuration);
                overlayCanvasGroup.alpha = 1f - progress;
                panelCanvasGroup.alpha = 1f - progress;
                panelRoot.localScale = panelRestingScale * Mathf.LerpUnclamped(1f, 0.98f, progress);
                yield return null;
            }

            overlay.SetActive(false);
            isOpen = false;
            isTransitioning = false;
            RestoreGameTime();
            joystickInput.SetInputEnabled(true);
            transitionRoutine = null;
        }

        private IEnumerator AnimateContent(float startAlpha, float endAlpha, float startX, float endX, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseOut(Mathf.Clamp01(elapsed / duration));
                contentCanvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, progress);
                contentRoot.anchoredPosition = new Vector2(Mathf.LerpUnclamped(startX, endX, progress), 0f);
                yield return null;
            }

            contentCanvasGroup.alpha = endAlpha;
            contentRoot.anchoredPosition = new Vector2(endX, 0f);
        }

        private void ApplyPage()
        {
            GuidePage page = pages[currentPageIndex];
            categoryText.text = page.Category;
            titleText.text = page.Title;
            summaryText.text = page.Summary;
            pageIndicatorText.text = $"{currentPageIndex + 1} / {pages.Length}";

            for (int i = 0; i < tipCards.Length; i++)
            {
                bool hasTip = page.Tips != null && i < page.Tips.Length;
                tipCards[i].SetActive(hasTip);
                if (!hasTip)
                {
                    continue;
                }

                tipNumberTexts[i].text = $"0{i + 1}";
                tipTitleTexts[i].text = page.Tips[i].Title;
                tipBodyTexts[i].text = page.Tips[i].Body;
            }

            contentCanvasGroup.alpha = 1f;
            contentRoot.anchoredPosition = Vector2.zero;
            nextButtonText.text = currentPageIndex == pages.Length - 1 ? "게임 시작" : "다음 →";
        }

        private void RefreshNavigation()
        {
            previousButton.interactable = currentPageIndex > 0;
            nextButton.interactable = true;
        }

        private void SetNavigationInteractable(bool isInteractable)
        {
            previousButton.interactable = isInteractable && currentPageIndex > 0;
            nextButton.interactable = isInteractable;
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
            int tipSlotCount = tipCards != null ? tipCards.Length : 0;
            if (overlay == null || gameArea == null || panelRoot == null || overlayCanvasGroup == null ||
                panelCanvasGroup == null || contentRoot == null || contentCanvasGroup == null || categoryText == null ||
                titleText == null || summaryText == null || pageIndicatorText == null || previousButton == null ||
                nextButton == null || nextButtonText == null || pages == null || pages.Length == 0 || tipSlotCount == 0 ||
                tipNumberTexts == null || tipNumberTexts.Length != tipSlotCount || tipTitleTexts == null ||
                tipTitleTexts.Length != tipSlotCount || tipBodyTexts == null || tipBodyTexts.Length != tipSlotCount)
            {
                throw new MissingReferenceException("시스템 안내 팝업 참조가 올바르게 구성되지 않았습니다.");
            }

            for (int i = 0; i < pages.Length; i++)
            {
                if (pages[i] == null || pages[i].Tips == null || pages[i].Tips.Length == 0 || pages[i].Tips.Length > tipSlotCount)
                {
                    throw new InvalidOperationException($"시스템 안내 {i + 1}페이지의 내용이 올바르지 않습니다.");
                }
            }
        }

        private void OnDestroy()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (previousButton != null) previousButton.onClick.RemoveListener(ShowPreviousPage);
            if (nextButton != null) nextButton.onClick.RemoveListener(ShowNextPage);
            RestoreGameTime();
        }

        private static float EaseOut(float value)
        {
            float inverse = 1f - value;
            return 1f - inverse * inverse * inverse;
        }

#if UNITY_EDITOR
        public void SetReferences(
            GameObject targetOverlay,
            RectTransform targetGameArea,
            RectTransform targetPanelRoot,
            CanvasGroup targetOverlayCanvasGroup,
            CanvasGroup targetPanelCanvasGroup,
            RectTransform targetContentRoot,
            CanvasGroup targetContentCanvasGroup,
            Text targetCategoryText,
            Text targetTitleText,
            Text targetSummaryText,
            GameObject[] targetTipCards,
            Text[] targetTipNumberTexts,
            Text[] targetTipTitleTexts,
            Text[] targetTipBodyTexts,
            Text targetPageIndicatorText,
            Button targetPreviousButton,
            Button targetNextButton,
            Text targetNextButtonText,
            GuidePage[] targetPages)
        {
            overlay = targetOverlay;
            gameArea = targetGameArea;
            panelRoot = targetPanelRoot;
            overlayCanvasGroup = targetOverlayCanvasGroup;
            panelCanvasGroup = targetPanelCanvasGroup;
            contentRoot = targetContentRoot;
            contentCanvasGroup = targetContentCanvasGroup;
            categoryText = targetCategoryText;
            titleText = targetTitleText;
            summaryText = targetSummaryText;
            tipCards = targetTipCards;
            tipNumberTexts = targetTipNumberTexts;
            tipTitleTexts = targetTipTitleTexts;
            tipBodyTexts = targetTipBodyTexts;
            pageIndicatorText = targetPageIndicatorText;
            previousButton = targetPreviousButton;
            nextButton = targetNextButton;
            nextButtonText = targetNextButtonText;
            pages = targetPages;
        }
#endif

        [Serializable]
        public sealed class GuidePage
        {
            [SerializeField] private string category;
            [SerializeField] private string title;
            [SerializeField, TextArea] private string summary;
            [SerializeField] private GuideTip[] tips;

            public string Category => category;
            public string Title => title;
            public string Summary => summary;
            public GuideTip[] Tips => tips;

            public GuidePage(string pageCategory, string pageTitle, string pageSummary, GuideTip[] pageTips)
            {
                category = pageCategory;
                title = pageTitle;
                summary = pageSummary;
                tips = pageTips;
            }
        }

        [Serializable]
        public sealed class GuideTip
        {
            [SerializeField] private string title;
            [SerializeField, TextArea] private string body;

            public string Title => title;
            public string Body => body;

            public GuideTip(string tipTitle, string tipBody)
            {
                title = tipTitle;
                body = tipBody;
            }
        }
    }
}
