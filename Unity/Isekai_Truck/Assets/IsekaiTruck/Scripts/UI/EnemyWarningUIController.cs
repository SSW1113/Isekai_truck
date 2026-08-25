using System.Collections.Generic;
using IsekaiTruck.Config;
using IsekaiTruck.Enemies;
using UnityEngine;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class EnemyWarningUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform warningArea;
        [SerializeField] private CanvasGroup warningGroup;
        [SerializeField] private RectTransform warningIcon;
        [SerializeField, Min(0f)] private float edgePadding = 48f;
        [SerializeField, Min(0f)] private float topEdgePadding = 180f;

        private GameConfig.EnemySettings settings;
        private EnemyManager enemyManager;
        private UnityEngine.Camera targetCamera;
        private Transform truck;
        private RectTransform[] warningIcons;
        private float blinkElapsed;
        private int visibleWarningCount;

        public bool IsWarningVisible => gameObject.activeSelf;
        public Vector2 IconPosition => warningIcon.anchoredPosition;
        public int VisibleWarningCount => visibleWarningCount;

        public void Initialize(GameConfig gameConfig, EnemyManager manager, UnityEngine.Camera cameraTarget, Transform truckTransform)
        {
            settings = gameConfig.Enemy;
            enemyManager = manager;
            targetCamera = cameraTarget;
            truck = truckTransform;
            warningGroup.interactable = false;
            warningGroup.blocksRaycasts = false;
            CreateIconPool(gameConfig);
            Hide();
        }

        public void UpdateWarning(float deltaTime)
        {
            visibleWarningCount = 0;
            IReadOnlyList<EnemyController> enemies = enemyManager.Enemies;
            for (int i = 0; i < enemies.Count && visibleWarningCount < warningIcons.Length; i++)
            {
                if (!TryGetOffscreenDirection(enemies[i], out Vector2 direction))
                {
                    continue;
                }

                RectTransform icon = warningIcons[visibleWarningCount++];
                icon.anchoredPosition = CalculateEdgePosition(direction);
                icon.gameObject.SetActive(true);
            }

            for (int i = visibleWarningCount; i < warningIcons.Length; i++)
            {
                warningIcons[i].gameObject.SetActive(false);
            }

            if (visibleWarningCount == 0)
            {
                Hide();
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            blinkElapsed += Mathf.Max(0f, deltaTime);
            float interval = settings.WarningBlinkInterval;
            float phase = Mathf.Repeat(blinkElapsed, interval * 2f);
            warningGroup.alpha = phase < interval ? 1f : 0.35f;
        }

        public void Hide()
        {
            blinkElapsed = 0f;
            visibleWarningCount = 0;
            warningGroup.alpha = 1f;
            if (warningIcons != null)
            {
                for (int i = 0; i < warningIcons.Length; i++)
                {
                    warningIcons[i].gameObject.SetActive(false);
                }
            }
            gameObject.SetActive(false);
        }

        public Vector2 GetIconPosition(int index)
        {
            return index >= 0 && index < visibleWarningCount
                ? warningIcons[index].anchoredPosition
                : Vector2.zero;
        }

        private void CreateIconPool(GameConfig gameConfig)
        {
            int targetCount = Mathf.Max(
                1,
                Mathf.Max(
                    gameConfig.Enemy.MinimumCountForTesting,
                    gameConfig.Wanted.MaxLevel * gameConfig.Enemy.CountPerWantedLevel));
            warningIcons = new RectTransform[targetCount];
            warningIcons[0] = warningIcon;

            for (int i = 1; i < warningIcons.Length; i++)
            {
                RectTransform icon = Instantiate(warningIcon, warningArea, false);
                icon.name = $"Warning Icon {i + 1}";
                warningIcons[i] = icon;
            }
        }

        private bool TryGetOffscreenDirection(EnemyController enemy, out Vector2 direction)
        {
            direction = Vector2.zero;
            float warningDistanceSquared = settings.OffscreenWarningDistance * settings.OffscreenWarningDistance;
            Vector3 offset = enemy.transform.position - truck.position;
            float distanceSquared = offset.x * offset.x + offset.z * offset.z;
            if (distanceSquared > warningDistanceSquared)
            {
                return false;
            }

            Vector3 viewportPosition = targetCamera.WorldToViewportPoint(enemy.transform.position);
            bool isInFront = viewportPosition.z > 0f;
            bool isOnScreen = isInFront
                && viewportPosition.x >= 0f && viewportPosition.x <= 1f
                && viewportPosition.y >= 0f && viewportPosition.y <= 1f;
            if (isOnScreen)
            {
                return false;
            }

            if (!isInFront)
            {
                viewportPosition.x = 1f - viewportPosition.x;
                viewportPosition.y = 1f - viewportPosition.y;
            }

            direction = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
            return direction.sqrMagnitude > 0.0001f;
        }

        private Vector2 CalculateEdgePosition(Vector2 direction)
        {
            Rect rect = warningArea.rect;
            float halfWidth = Mathf.Max(0f, rect.width * 0.5f - edgePadding);
            float halfHeight = Mathf.Max(0f, rect.height * 0.5f - edgePadding);
            float xScale = Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float yScale = Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            Vector2 position = direction * Mathf.Min(xScale, yScale);
            if (position.y > 0f)
            {
                float topLimit = Mathf.Max(0f, rect.height * 0.5f - topEdgePadding);
                position.y = Mathf.Min(position.y, topLimit);
            }

            return position;
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetWarningArea,
            CanvasGroup targetWarningGroup,
            RectTransform targetWarningIcon,
            float targetEdgePadding,
            float targetTopEdgePadding
        )
        {
            warningArea = targetWarningArea;
            warningGroup = targetWarningGroup;
            warningIcon = targetWarningIcon;
            edgePadding = targetEdgePadding;
            topEdgePadding = targetTopEdgePadding;
        }
#endif
    }
}
