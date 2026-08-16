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

        private GameConfig.EnemySettings settings;
        private EnemyManager enemyManager;
        private UnityEngine.Camera targetCamera;
        private Transform truck;
        private float blinkElapsed;

        public bool IsWarningVisible => gameObject.activeSelf;
        public Vector2 IconPosition => warningIcon.anchoredPosition;

        public void Initialize(GameConfig gameConfig, EnemyManager manager, UnityEngine.Camera cameraTarget, Transform truckTransform)
        {
            settings = gameConfig.Enemy;
            enemyManager = manager;
            targetCamera = cameraTarget;
            truck = truckTransform;
            warningGroup.interactable = false;
            warningGroup.blocksRaycasts = false;
            Hide();
        }

        public void UpdateWarning(float deltaTime)
        {
            if (!TryGetNearestOffscreenDirection(out Vector2 direction))
            {
                Hide();
                return;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            warningIcon.anchoredPosition = CalculateEdgePosition(direction);
            blinkElapsed += Mathf.Max(0f, deltaTime);
            float interval = settings.WarningBlinkInterval;
            float phase = Mathf.Repeat(blinkElapsed, interval * 2f);
            warningGroup.alpha = phase < interval ? 1f : 0.35f;
        }

        public void Hide()
        {
            blinkElapsed = 0f;
            warningGroup.alpha = 1f;
            gameObject.SetActive(false);
        }

        private bool TryGetNearestOffscreenDirection(out Vector2 nearestDirection)
        {
            nearestDirection = Vector2.zero;
            float warningDistanceSquared = settings.OffscreenWarningDistance * settings.OffscreenWarningDistance;
            float nearestDistanceSquared = float.MaxValue;
            IReadOnlyList<EnemyController> enemies = enemyManager.Enemies;

            for (int i = 0; i < enemies.Count; i++)
            {
                EnemyController enemy = enemies[i];
                Vector3 offset = enemy.transform.position - truck.position;
                float distanceSquared = offset.x * offset.x + offset.z * offset.z;
                if (distanceSquared > warningDistanceSquared || distanceSquared >= nearestDistanceSquared)
                {
                    continue;
                }

                Vector3 viewportPosition = targetCamera.WorldToViewportPoint(enemy.transform.position);
                bool isInFront = viewportPosition.z > 0f;
                bool isOnScreen = isInFront
                    && viewportPosition.x >= 0f && viewportPosition.x <= 1f
                    && viewportPosition.y >= 0f && viewportPosition.y <= 1f;
                if (isOnScreen)
                {
                    continue;
                }

                if (!isInFront)
                {
                    viewportPosition.x = 1f - viewportPosition.x;
                    viewportPosition.y = 1f - viewportPosition.y;
                }

                Vector2 direction = new Vector2(viewportPosition.x - 0.5f, viewportPosition.y - 0.5f);
                if (direction.sqrMagnitude <= 0.0001f)
                {
                    continue;
                }

                nearestDistanceSquared = distanceSquared;
                nearestDirection = direction;
            }

            return nearestDistanceSquared < float.MaxValue;
        }

        private Vector2 CalculateEdgePosition(Vector2 direction)
        {
            Rect rect = warningArea.rect;
            float halfWidth = Mathf.Max(0f, rect.width * 0.5f - edgePadding);
            float halfHeight = Mathf.Max(0f, rect.height * 0.5f - edgePadding);
            float xScale = Mathf.Abs(direction.x) > 0.0001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float yScale = Mathf.Abs(direction.y) > 0.0001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            return direction * Mathf.Min(xScale, yScale);
        }

#if UNITY_EDITOR
        public void SetReferences(RectTransform targetWarningArea, CanvasGroup targetWarningGroup, RectTransform targetWarningIcon, float targetEdgePadding)
        {
            warningArea = targetWarningArea;
            warningGroup = targetWarningGroup;
            warningIcon = targetWarningIcon;
            edgePadding = targetEdgePadding;
        }
#endif
    }
}
