using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class GoddessSpeechBubble : MonoBehaviour
    {
        [SerializeField] private RectTransform bubbleRect;
        [SerializeField] private TMP_Text messageText;
        [SerializeField] private RectTransform widthReference;
        [SerializeField] private string initialMessage = "여신이 지켜보고 있습니다";
        [SerializeField] private Vector2 minBubbleSize = new Vector2(96f, 58f);
        [SerializeField] private Vector2 maxBubbleSize = new Vector2(330f, 150f);
        [SerializeField, Range(0.1f, 1f)] private float maxWidthRatio = 0.8f;
        [SerializeField] private Vector2 contentPadding = new Vector2(28f, 24f);
        [SerializeField, Min(0.01f)] private float characterInterval = 0.045f;
        [SerializeField, Min(1)] private int longMessageThreshold = 22;
        [SerializeField, Min(1)] private int longMessageChunkSize = 2;
        [SerializeField, Min(0.1f)] private float resizeSpeed = 14f;
        [SerializeField, Range(0.5f, 1f)] private float startScale = 0.9f;
        [SerializeField, Range(1f, 1.1f)] private float bounceScale = 1.03f;
        [SerializeField, Min(0f)] private float bounceDuration = 0.16f;
        [SerializeField, Min(0f)] private float endHoldDuration = 1f;

        private Coroutine speechRoutine;
        private Vector3 restingScale = Vector3.one;

        private void Awake()
        {
            restingScale = bubbleRect != null ? bubbleRect.localScale : Vector3.one;
        }

        private void Start()
        {
            if (speechRoutine == null && !string.IsNullOrEmpty(initialMessage))
            {
                ShowMessage(initialMessage);
            }
            else if (speechRoutine == null && bubbleRect != null)
            {
                bubbleRect.gameObject.SetActive(false);
            }
        }

        private void OnDisable()
        {
            if (speechRoutine != null)
            {
                StopCoroutine(speechRoutine);
                speechRoutine = null;
            }

            if (bubbleRect != null)
            {
                bubbleRect.localScale = restingScale;
                bubbleRect.gameObject.SetActive(false);
            }
        }

        public void SetReferences(
            RectTransform targetBubble,
            TMP_Text targetText,
            RectTransform targetWidthReference,
            string message
        )
        {
            bubbleRect = targetBubble;
            messageText = targetText;
            widthReference = targetWidthReference;
            initialMessage = message;
        }

        public void ShowMessage(string message)
        {
            if (bubbleRect == null || messageText == null)
            {
                return;
            }

            initialMessage = message ?? string.Empty;
            bubbleRect.gameObject.SetActive(true);
            if (!Application.isPlaying || !isActiveAndEnabled)
            {
                messageText.text = initialMessage;
                bubbleRect.sizeDelta = CalculateBubbleSize(initialMessage);
                return;
            }

            if (speechRoutine != null)
            {
                StopCoroutine(speechRoutine);
            }

            speechRoutine = StartCoroutine(PlaySpeech(initialMessage));
        }

        private IEnumerator PlaySpeech(string message)
        {
            int[] characterIndices = StringInfo.ParseCombiningCharacters(message);
            int chunkSize = characterIndices.Length > longMessageThreshold ? longMessageChunkSize : 1;
            bubbleRect.sizeDelta = minBubbleSize;
            bubbleRect.localScale = restingScale * startScale;
            messageText.text = string.Empty;

            for (int visibleCount = 0; visibleCount < characterIndices.Length;)
            {
                visibleCount = Mathf.Min(visibleCount + chunkSize, characterIndices.Length);
                int stringLength = visibleCount < characterIndices.Length ? characterIndices[visibleCount] : message.Length;
                messageText.text = message.Substring(0, stringLength);

                Vector2 targetSize = CalculateBubbleSize(messageText.text);
                float progress = characterIndices.Length > 0 ? (float)visibleCount / characterIndices.Length : 1f;
                float waitDuration = characterInterval * Mathf.Lerp(1f, 0.72f, progress);
                float elapsed = 0f;

                while (elapsed < waitDuration)
                {
                    float deltaTime = Time.unscaledDeltaTime;
                    elapsed += deltaTime;
                    ResizeTowards(targetSize, deltaTime);
                    bubbleRect.localScale = Vector3.Lerp(restingScale * startScale, restingScale, progress);
                    yield return null;
                }
            }

            Vector2 finalSize = CalculateBubbleSize(message);
            while ((bubbleRect.sizeDelta - finalSize).sqrMagnitude > 0.25f)
            {
                ResizeTowards(finalSize, Time.unscaledDeltaTime);
                yield return null;
            }

            bubbleRect.sizeDelta = finalSize;
            bubbleRect.localScale = restingScale;
            yield return PlayBounce();

            float holdTime = 0f;
            while (holdTime < endHoldDuration)
            {
                holdTime += Time.unscaledDeltaTime;
                yield return null;
            }

            speechRoutine = null;
            bubbleRect.gameObject.SetActive(false);
        }

        private IEnumerator PlayBounce()
        {
            if (bounceDuration <= 0f)
            {
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < bounceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float progress = Mathf.Clamp01(elapsed / bounceDuration);
                float pulse = Mathf.Sin(progress * Mathf.PI) * (bounceScale - 1f);
                bubbleRect.localScale = restingScale * (1f + pulse);
                yield return null;
            }

            bubbleRect.localScale = restingScale;
        }

        private Vector2 CalculateBubbleSize(string message)
        {
            if (string.IsNullOrEmpty(message))
            {
                return minBubbleSize;
            }

            TextWrappingModes previousWrappingMode = messageText.textWrappingMode;
            messageText.textWrappingMode = TextWrappingModes.NoWrap;
            Vector2 unwrappedSize = messageText.GetPreferredValues(message, 0f, 0f);
            messageText.textWrappingMode = previousWrappingMode;

            float availableWidth = widthReference != null && widthReference.rect.width > 0f
                ? widthReference.rect.width * maxWidthRatio
                : maxBubbleSize.x;
            float maxWidth = Mathf.Max(minBubbleSize.x, Mathf.Min(maxBubbleSize.x, availableWidth));
            float width = Mathf.Clamp(unwrappedSize.x + contentPadding.x, minBubbleSize.x, maxWidth);
            float contentWidth = Mathf.Max(1f, width - contentPadding.x);
            Vector2 wrappedSize = messageText.GetPreferredValues(message, contentWidth, 0f);
            float height = Mathf.Clamp(wrappedSize.y + contentPadding.y, minBubbleSize.y, maxBubbleSize.y);
            return new Vector2(width, height);
        }

        private void ResizeTowards(Vector2 targetSize, float deltaTime)
        {
            float blend = 1f - Mathf.Exp(-resizeSpeed * Mathf.Max(deltaTime, 0f));
            bubbleRect.sizeDelta = Vector2.Lerp(bubbleRect.sizeDelta, targetSize, blend);
        }
    }
}
