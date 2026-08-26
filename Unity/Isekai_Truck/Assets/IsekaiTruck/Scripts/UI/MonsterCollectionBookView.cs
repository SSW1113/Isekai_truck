using System.Collections;
using IsekaiTruck.Collection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class MonsterCollectionBookView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup overlayCanvasGroup;
        [SerializeField] private RectTransform coverRoot;
        [SerializeField] private RectTransform pagesRoot;
        [SerializeField] private CanvasGroup pagesCanvasGroup;
        [SerializeField] private CanvasGroup cardGridCanvasGroup;
        [SerializeField] private RectTransform cardGridRoot;
        [SerializeField] private RectTransform animationLayer;
        [SerializeField] private RectTransform previewCard;
        [SerializeField] private Image previewPortrait;
        [SerializeField] private TMP_Text previewNameText;
        [SerializeField] private RectTransform scrollRoot;
        [SerializeField] private CanvasGroup scrollCanvasGroup;
        [SerializeField] private TMP_Text detailNameText;
        [SerializeField] private TMP_Text detailBodyText;
        [SerializeField, Min(0.05f)] private float coverDuration = 0.18f;
        [SerializeField, Min(0.05f)] private float pageDuration = 0.32f;
        [SerializeField, Min(0.05f)] private float cardMoveDuration = 0.22f;
        [SerializeField, Min(0.05f)] private float scrollDuration = 0.30f;

        private readonly Vector2 centerPosition = new Vector2(0f, -12f);
        private readonly Vector2 centerSize = new Vector2(300f, 360f);
        private readonly Vector2 detailCardPosition = new Vector2(-355f, -18f);
        private readonly Vector2 detailCardSize = new Vector2(250f, 320f);
        private MonsterCollectionCardView activeCard;

        public bool HasSelection => activeCard != null;

        public IEnumerator PlayOpen()
        {
            PrepareClosed();
            yield return FadeCanvasGroup(overlayCanvasGroup, 0f, 1f, coverDuration);

            coverRoot.gameObject.SetActive(true);
            yield return ScaleX(coverRoot, 1f, 0.06f, coverDuration);
            coverRoot.gameObject.SetActive(false);

            pagesRoot.gameObject.SetActive(true);
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(cardGridRoot);
            yield return ScaleXAndFade(pagesRoot, pagesCanvasGroup, 0.06f, 1f, 0f, 1f, pageDuration);
        }

        public IEnumerator PlayClose()
        {
            if (activeCard != null)
            {
                yield return CollapseSelection();
            }

            yield return ScaleXAndFade(pagesRoot, pagesCanvasGroup, 1f, 0.06f, 1f, 0f, pageDuration * 0.8f);
            pagesRoot.gameObject.SetActive(false);
            coverRoot.gameObject.SetActive(true);
            yield return ScaleX(coverRoot, 0.06f, 1f, coverDuration);
            yield return FadeCanvasGroup(overlayCanvasGroup, 1f, 0f, coverDuration);
        }

        public IEnumerator PlayCloseSelection()
        {
            if (activeCard != null)
            {
                yield return CollapseSelection();
            }
        }

        public IEnumerator PlaySelection(MonsterCollectionCardView card, MonsterCollectionEntry entry, string detailBody)
        {
            if (activeCard != null)
            {
                yield return CollapseSelection();
            }

            activeCard = card;
            activeCard.SetSelected(true);
            ConfigurePreview(entry);

            Vector2 sourcePosition = GetCardLocalPosition(card.CardRect);
            Vector2 sourceSize = card.CardRect.rect.size;
            previewCard.anchoredPosition = sourcePosition;
            previewCard.sizeDelta = sourceSize;
            previewCard.localScale = Vector3.one;
            previewCard.gameObject.SetActive(true);
            activeCard.SetFocusHidden(true);

            yield return FadeCanvasGroup(cardGridCanvasGroup, 1f, 0.28f, cardMoveDuration * 0.7f);
            yield return AnimatePreview(sourcePosition, centerPosition, sourceSize, centerSize, cardMoveDuration);
            yield return WaitUnscaled(0.08f);
            yield return AnimatePreview(centerPosition, detailCardPosition, centerSize, detailCardSize, cardMoveDuration);

            detailNameText.text = entry.DisplayName;
            detailBodyText.text = detailBody;
            scrollRoot.gameObject.SetActive(true);
            scrollRoot.localScale = new Vector3(0f, 1f, 1f);
            scrollCanvasGroup.alpha = 0f;
            yield return ScaleXAndFade(scrollRoot, scrollCanvasGroup, 0f, 1f, 0f, 1f, scrollDuration);
        }

        public void PrepareClosed()
        {
            RestoreActiveCard();
            overlayCanvasGroup.alpha = 0f;
            coverRoot.gameObject.SetActive(true);
            coverRoot.localScale = Vector3.one;
            pagesRoot.gameObject.SetActive(false);
            pagesRoot.localScale = new Vector3(0.06f, 1f, 1f);
            pagesCanvasGroup.alpha = 0f;
            cardGridCanvasGroup.alpha = 1f;
            previewCard.gameObject.SetActive(false);
            scrollRoot.gameObject.SetActive(false);
            scrollRoot.localScale = new Vector3(0f, 1f, 1f);
            scrollCanvasGroup.alpha = 0f;
        }

        private IEnumerator CollapseSelection()
        {
            yield return ScaleXAndFade(scrollRoot, scrollCanvasGroup, 1f, 0f, 1f, 0f, scrollDuration * 0.75f);
            scrollRoot.gameObject.SetActive(false);
            yield return AnimatePreview(detailCardPosition, centerPosition, detailCardSize, centerSize, cardMoveDuration * 0.75f);

            Vector2 targetPosition = GetCardLocalPosition(activeCard.CardRect);
            Vector2 targetSize = activeCard.CardRect.rect.size;
            yield return AnimatePreview(centerPosition, targetPosition, centerSize, targetSize, cardMoveDuration * 0.75f);
            RestoreActiveCard();
            yield return FadeCanvasGroup(cardGridCanvasGroup, cardGridCanvasGroup.alpha, 1f, cardMoveDuration * 0.6f);
        }

        private void RestoreActiveCard()
        {
            if (activeCard != null)
            {
                activeCard.SetFocusHidden(false);
                activeCard.SetSelected(false);
                activeCard = null;
            }

            previewCard.gameObject.SetActive(false);
        }

        private void ConfigurePreview(MonsterCollectionEntry entry)
        {
            previewPortrait.sprite = entry.Portrait;
            previewPortrait.enabled = entry.Portrait != null;
            previewNameText.text = entry.DisplayName;
        }

        private Vector2 GetCardLocalPosition(RectTransform cardRect)
        {
            Vector3 worldCenter = cardRect.TransformPoint(cardRect.rect.center);
            return animationLayer.InverseTransformPoint(worldCenter);
        }

        private IEnumerator AnimatePreview(
            Vector2 startPosition,
            Vector2 endPosition,
            Vector2 startSize,
            Vector2 endSize,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseInOut(Mathf.Clamp01(elapsed / duration));
                previewCard.anchoredPosition = Vector2.LerpUnclamped(startPosition, endPosition, progress);
                previewCard.sizeDelta = Vector2.LerpUnclamped(startSize, endSize, progress);
                yield return null;
            }

            previewCard.anchoredPosition = endPosition;
            previewCard.sizeDelta = endSize;
        }

        private static IEnumerator ScaleX(RectTransform target, float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseInOut(Mathf.Clamp01(elapsed / duration));
                target.localScale = new Vector3(Mathf.LerpUnclamped(start, end, progress), 1f, 1f);
                yield return null;
            }

            target.localScale = new Vector3(end, 1f, 1f);
        }

        private static IEnumerator ScaleXAndFade(
            RectTransform target,
            CanvasGroup canvasGroup,
            float startScale,
            float endScale,
            float startAlpha,
            float endAlpha,
            float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = EaseInOut(Mathf.Clamp01(elapsed / duration));
                target.localScale = new Vector3(Mathf.LerpUnclamped(startScale, endScale, progress), 1f, 1f);
                canvasGroup.alpha = Mathf.LerpUnclamped(startAlpha, endAlpha, progress);
                yield return null;
            }

            target.localScale = new Vector3(endScale, 1f, 1f);
            canvasGroup.alpha = endAlpha;
        }

        private static IEnumerator FadeCanvasGroup(CanvasGroup canvasGroup, float start, float end, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);
                canvasGroup.alpha = Mathf.Lerp(start, end, progress);
                yield return null;
            }

            canvasGroup.alpha = end;
        }

        private static IEnumerator WaitUnscaled(float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        private static float EaseInOut(float value)
        {
            return value * value * (3f - 2f * value);
        }

#if UNITY_EDITOR
        public void SetReferences(
            CanvasGroup targetOverlayCanvasGroup,
            RectTransform targetCoverRoot,
            RectTransform targetPagesRoot,
            CanvasGroup targetPagesCanvasGroup,
            CanvasGroup targetCardGridCanvasGroup,
            RectTransform targetCardGridRoot,
            RectTransform targetAnimationLayer,
            RectTransform targetPreviewCard,
            Image targetPreviewPortrait,
            TMP_Text targetPreviewNameText,
            RectTransform targetScrollRoot,
            CanvasGroup targetScrollCanvasGroup,
            TMP_Text targetDetailNameText,
            TMP_Text targetDetailBodyText)
        {
            overlayCanvasGroup = targetOverlayCanvasGroup;
            coverRoot = targetCoverRoot;
            pagesRoot = targetPagesRoot;
            pagesCanvasGroup = targetPagesCanvasGroup;
            cardGridCanvasGroup = targetCardGridCanvasGroup;
            cardGridRoot = targetCardGridRoot;
            animationLayer = targetAnimationLayer;
            previewCard = targetPreviewCard;
            previewPortrait = targetPreviewPortrait;
            previewNameText = targetPreviewNameText;
            scrollRoot = targetScrollRoot;
            scrollCanvasGroup = targetScrollCanvasGroup;
            detailNameText = targetDetailNameText;
            detailBodyText = targetDetailBodyText;
        }
#endif
    }
}
