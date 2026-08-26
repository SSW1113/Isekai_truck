using IsekaiTruck.Camera;
using TMPro;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class RewardGainPopupUI : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform effectRoot;
        [SerializeField] private TextMeshProUGUI popupTemplate;
        [SerializeField, Range(1, 24)] private int poolSize = 10;
        [SerializeField, Min(0.1f)] private float duration = 0.72f;
        [SerializeField, Min(0f)] private float riseDistance = 92f;
        [SerializeField, Min(0f)] private float truckHeightOffset = 1.8f;

        private PopupState[] popupStates;
        private TextMeshProUGUI[] popupTexts;
        private UnityEngine.Camera worldCamera;
        private Transform truck;
        private int nextPopupIndex;
        private bool isInitialized;

        public void Initialize(CameraController cameraController, Transform truckTransform)
        {
            if (isInitialized)
            {
                return;
            }

            worldCamera = cameraController.TargetCamera;
            truck = truckTransform;
            popupStates = new PopupState[poolSize];
            popupTexts = new TextMeshProUGUI[poolSize];

            for (int i = 0; i < poolSize; i++)
            {
                TextMeshProUGUI popup = i == 0 ? popupTemplate : Instantiate(popupTemplate, effectRoot, false);
                popup.name = $"Reward Popup {i + 1}";
                popup.raycastTarget = false;
                popup.gameObject.SetActive(false);
                popupTexts[i] = popup;
                popupStates[i].RectTransform = popup.rectTransform;
                popupStates[i].CanvasGroup = popup.GetComponent<CanvasGroup>();
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            for (int i = 0; i < popupStates.Length; i++)
            {
                if (!popupStates[i].IsActive)
                {
                    continue;
                }

                UpdatePopup(i, deltaTime);
            }
        }

        public bool Play(int expAmount, int soulAmount)
        {
            if (!isInitialized || truck == null || worldCamera == null || expAmount <= 0 && soulAmount <= 0)
            {
                return false;
            }

            int index = FindPopup();
            Vector2 screenPosition = worldCamera.WorldToScreenPoint(truck.position + Vector3.up * truckHeightOffset);
            UnityEngine.Camera canvasCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPosition, canvasCamera, out Vector2 localPosition))
            {
                return false;
            }

            float horizontalOffset = ((index % 3) - 1) * 34f;
            PopupState state = popupStates[index];
            state.StartPosition = localPosition + new Vector2(horizontalOffset, 18f);
            state.Elapsed = 0f;
            state.IsActive = true;
            state.RectTransform.anchoredPosition = state.StartPosition;
            state.RectTransform.localScale = Vector3.zero;
            state.CanvasGroup.alpha = 1f;
            popupStates[index] = state;

            popupTexts[index].text = BuildText(expAmount, soulAmount);
            popupTexts[index].gameObject.SetActive(true);
            nextPopupIndex = (index + 1) % popupStates.Length;
            return true;
        }

        private int FindPopup()
        {
            for (int i = 0; i < popupStates.Length; i++)
            {
                int index = (nextPopupIndex + i) % popupStates.Length;
                if (!popupStates[index].IsActive)
                {
                    return index;
                }
            }

            CompletePopup(nextPopupIndex);
            return nextPopupIndex;
        }

        private void UpdatePopup(int index, float deltaTime)
        {
            PopupState state = popupStates[index];
            state.Elapsed += deltaTime;
            float progress = Mathf.Clamp01(state.Elapsed / duration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            state.RectTransform.anchoredPosition = state.StartPosition + Vector2.up * (riseDistance * easedProgress);

            float scale = progress < 0.2f
                ? Mathf.Lerp(0f, 1.18f, progress / 0.2f)
                : Mathf.Lerp(1.18f, 1f, (progress - 0.2f) / 0.8f);
            state.RectTransform.localScale = Vector3.one * scale;
            state.CanvasGroup.alpha = progress < 0.58f ? 1f : 1f - Mathf.InverseLerp(0.58f, 1f, progress);
            popupStates[index] = state;

            if (progress >= 1f)
            {
                CompletePopup(index);
            }
        }

        private void CompletePopup(int index)
        {
            PopupState state = popupStates[index];
            state.IsActive = false;
            state.Elapsed = 0f;
            state.CanvasGroup.alpha = 0f;
            state.RectTransform.localScale = Vector3.zero;
            popupStates[index] = state;
            popupTexts[index].gameObject.SetActive(false);
        }

        private static string BuildText(int expAmount, int soulAmount)
        {
            if (expAmount > 0 && soulAmount > 0)
            {
                return $"<color=#F5C5DC>EXP +{expAmount}</color>  <color=#FFD36B>영혼 +{soulAmount}</color>";
            }

            return expAmount > 0
                ? $"<color=#F5C5DC>EXP +{expAmount}</color>"
                : $"<color=#FFD36B>영혼 +{soulAmount}</color>";
        }

#if UNITY_EDITOR
        public void SetReferences(Canvas canvas, RectTransform root, TextMeshProUGUI template)
        {
            targetCanvas = canvas;
            effectRoot = root;
            popupTemplate = template;
            poolSize = 10;
        }
#endif

        private struct PopupState
        {
            public RectTransform RectTransform;
            public CanvasGroup CanvasGroup;
            public Vector2 StartPosition;
            public float Elapsed;
            public bool IsActive;
        }
    }
}
