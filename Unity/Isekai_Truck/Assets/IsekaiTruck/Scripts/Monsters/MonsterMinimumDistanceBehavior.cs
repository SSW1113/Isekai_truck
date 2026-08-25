using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterMinimumDistanceBehavior : MonsterMovementBehavior
    {
        [Header("Minimum Truck Distance")]
        [SerializeField, Min(0f)] private float minimumDistance = 5f;

        private Vector3 lastSeparationDirection;
        private bool hasSeparationDirection;

        public float MinimumDistance => minimumDistance;

        protected override void OnInitialized()
        {
            lastSeparationDirection = Vector3.zero;
            hasSeparationDirection = false;
        }

        protected override bool TryUpdateMovement(MonsterMovementContext context)
        {
            float safeMinimumDistance = Mathf.Max(0f, minimumDistance);
            if (safeMinimumDistance <= 0f)
            {
                return false;
            }

            Vector3 separation = transform.position - Truck.position;
            separation.y = 0f;
            float squaredDistance = separation.sqrMagnitude;
            if (squaredDistance > 0.000001f)
            {
                lastSeparationDirection = separation / Mathf.Sqrt(squaredDistance);
                hasSeparationDirection = true;
            }

            if (squaredDistance >= safeMinimumDistance * safeMinimumDistance)
            {
                return false;
            }

            Vector3 separationDirection = hasSeparationDirection
                ? lastSeparationDirection
                : ResolveFallbackDirection();
            Vector3 constrainedPosition = Truck.position + separationDirection * safeMinimumDistance;
            constrainedPosition.y = transform.position.y;
            transform.position = constrainedPosition;
            SetMovementVisual(separationDirection, Type.Speed, true);
            return true;
        }

        private Vector3 ResolveFallbackDirection()
        {
            Vector3 fallbackDirection = -Vector3.ProjectOnPlane(Truck.forward, Vector3.up);
            if (fallbackDirection.sqrMagnitude <= 0.000001f)
            {
                fallbackDirection = Vector3.forward;
            }

            lastSeparationDirection = fallbackDirection.normalized;
            hasSeparationDirection = true;
            return lastSeparationDirection;
        }
    }
}
