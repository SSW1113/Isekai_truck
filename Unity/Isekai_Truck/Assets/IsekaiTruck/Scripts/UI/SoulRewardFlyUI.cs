using System;
using IsekaiTruck.Camera;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class SoulRewardFlyUI : MonoBehaviour
    {
        [SerializeField] private Canvas targetCanvas;
        [SerializeField] private RectTransform effectRoot;
        [SerializeField] private RectTransform soulTarget;
        [SerializeField] private Image orbTemplate;
        [SerializeField, Range(1, 32)] private int poolSize = 12;
        [SerializeField] private GameUIController gameUIController;
        [SerializeField, Min(0f)] private float spawnDelay = 0.24f;
        [SerializeField, Min(0.05f)] private float flightDuration = 0.36f;
        [SerializeField, Min(0f)] private float arcHeight = 90f;

        private OrbState[] orbStates;
        private Image[] orbImages;
        private UnityEngine.Camera worldCamera;
        private int nextOrbIndex;
        private bool isInitialized;

        public void Initialize(CameraController cameraController)
        {
            if (isInitialized)
            {
                return;
            }

            worldCamera = cameraController.TargetCamera;
            orbImages = new Image[poolSize];
            orbStates = new OrbState[poolSize];
            for (int i = 0; i < orbImages.Length; i++)
            {
                Image image = i == 0 ? orbTemplate : Instantiate(orbTemplate, effectRoot, false);
                image.name = $"Soul Orb {i + 1}";
                orbImages[i] = image;
                orbImages[i].raycastTarget = false;
                orbImages[i].enabled = false;
                orbStates[i].RectTransform = orbImages[i].rectTransform;
            }

            nextOrbIndex = 0;
            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            for (int i = 0; i < orbStates.Length; i++)
            {
                if (!orbStates[i].IsActive)
                {
                    continue;
                }

                UpdateOrb(i, deltaTime);
            }
        }

        public bool Play(Vector3 worldPosition, int soulAmount)
        {
            if (!isInitialized || soulAmount <= 0 || worldCamera == null || effectRoot == null || soulTarget == null || orbStates.Length == 0)
            {
                return false;
            }

            int orbIndex = FindAvailableOrb();
            if (orbIndex < 0)
            {
                OrbState aggregateState = orbStates[nextOrbIndex];
                aggregateState.SoulAmount += soulAmount;
                orbStates[nextOrbIndex] = aggregateState;
                return true;
            }

            Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition + Vector3.up * 0.8f);
            UnityEngine.Camera canvasCamera = targetCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : targetCanvas.worldCamera;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, screenPosition, canvasCamera, out Vector2 startPosition))
            {
                return false;
            }

            Vector2 targetScreenPosition = RectTransformUtility.WorldToScreenPoint(canvasCamera, soulTarget.position);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(effectRoot, targetScreenPosition, canvasCamera, out Vector2 endPosition))
            {
                return false;
            }

            RectTransform orb = orbStates[orbIndex].RectTransform;
            orb.anchoredPosition = startPosition;
            orb.localScale = Vector3.zero;
            orbImages[orbIndex].enabled = false;
            orbStates[orbIndex].StartPosition = startPosition;
            orbStates[orbIndex].EndPosition = endPosition;
            orbStates[orbIndex].ControlPosition = (startPosition + endPosition) * 0.5f + Vector2.up * arcHeight;
            orbStates[orbIndex].SoulAmount = soulAmount;
            orbStates[orbIndex].Elapsed = 0f;
            orbStates[orbIndex].IsActive = true;
            nextOrbIndex = (orbIndex + 1) % orbStates.Length;
            return true;
        }

        private int FindAvailableOrb()
        {
            for (int i = 0; i < orbStates.Length; i++)
            {
                int index = (nextOrbIndex + i) % orbStates.Length;
                if (!orbStates[index].IsActive)
                {
                    return index;
                }
            }

            return -1;
        }

        private void UpdateOrb(int index, float deltaTime)
        {
            OrbState state = orbStates[index];
            state.Elapsed += Mathf.Max(0f, deltaTime);
            if (state.Elapsed < spawnDelay)
            {
                orbStates[index] = state;
                return;
            }

            orbImages[index].enabled = true;
            float progress = Mathf.Clamp01((state.Elapsed - spawnDelay) / flightDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            float inverseProgress = 1f - easedProgress;
            state.RectTransform.anchoredPosition = inverseProgress * inverseProgress * state.StartPosition
                + 2f * inverseProgress * easedProgress * state.ControlPosition
                + easedProgress * easedProgress * state.EndPosition;

            float scale = progress < 0.2f
                ? Mathf.Lerp(0f, 1.15f, progress / 0.2f)
                : Mathf.Lerp(1.15f, 0.82f, (progress - 0.2f) / 0.8f);
            state.RectTransform.localScale = Vector3.one * scale;
            orbStates[index] = state;

            if (progress >= 1f)
            {
                CompleteOrb(index);
            }
        }

        private void CompleteOrb(int index)
        {
            int soulAmount = orbStates[index].SoulAmount;
            orbStates[index].IsActive = false;
            orbStates[index].SoulAmount = 0;
            orbImages[index].enabled = false;
            orbStates[index].RectTransform.localScale = Vector3.zero;
            gameUIController?.ReleaseDeferredSoul(soulAmount);
        }

        private void OnDisable()
        {
            if (orbStates == null)
            {
                return;
            }

            for (int i = 0; i < orbStates.Length; i++)
            {
                if (orbStates[i].IsActive)
                {
                    CompleteOrb(i);
                }
            }
        }

        [Serializable]
        private struct OrbState
        {
            public RectTransform RectTransform;
            public Vector2 StartPosition;
            public Vector2 ControlPosition;
            public Vector2 EndPosition;
            public float Elapsed;
            public int SoulAmount;
            public bool IsActive;
        }

#if UNITY_EDITOR
        public void SetReferences(Canvas canvas, RectTransform root, RectTransform target, Image template, GameUIController gameUI)
        {
            targetCanvas = canvas;
            effectRoot = root;
            soulTarget = target;
            orbTemplate = template;
            poolSize = 12;
            gameUIController = gameUI;
            spawnDelay = 0.24f;
            flightDuration = 0.36f;
            arcHeight = 90f;
        }
#endif
    }
}
