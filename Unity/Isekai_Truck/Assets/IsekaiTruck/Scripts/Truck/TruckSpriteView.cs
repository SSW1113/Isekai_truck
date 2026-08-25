using IsekaiTruck.Visuals;
using UnityEngine;

namespace IsekaiTruck.Truck
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TruckController))]
    public sealed class TruckSpriteView : MonoBehaviour
    {
        [SerializeField] private TruckController truckController;
        [SerializeField] private DirectionalSpriteAnimator directionalSpriteAnimator;

        [Header("Impact Feedback")]
        [SerializeField, Min(0.01f)] private float impactDuration = 0.16f;
        [SerializeField, Range(0.8f, 1f)] private float squashScale = 0.94f;
        [SerializeField, Range(1f, 1.1f)] private float reboundScale = 1.03f;

        private Vector3 previousPosition;
        private Transform impactVisual;
        private Vector3 impactBaseScale;
        private float impactRemaining;

        private void Awake()
        {
            ResolveReferences();
            previousPosition = transform.position;
            directionalSpriteAnimator?.Initialize();
            CacheImpactVisual();
        }

        private void OnEnable()
        {
            previousPosition = transform.position;
        }

        private void LateUpdate()
        {
            ResolveReferences();

            Vector3 currentPosition = transform.position;
            Vector3 movement = Vector3.ProjectOnPlane(currentPosition - previousPosition, Vector3.up);
            float moveSpeed = Time.deltaTime > 0f ? movement.magnitude / Time.deltaTime : 0f;
            Vector3 direction = movement.sqrMagnitude > 0.000001f ? movement.normalized : transform.forward;
            directionalSpriteAnimator?.SetMovement(direction, moveSpeed);
            UpdateImpactFeedback();
            previousPosition = currentPosition;
        }

        public void PlayImpactFeedback()
        {
            CacheImpactVisual();
            impactRemaining = impactDuration;
        }

        private void ResolveReferences()
        {
            if (truckController == null)
            {
                truckController = GetComponent<TruckController>();
            }

            if (directionalSpriteAnimator == null)
            {
                directionalSpriteAnimator = GetComponentInChildren<DirectionalSpriteAnimator>(true);
            }
        }

        private void CacheImpactVisual()
        {
            Transform resolvedVisual = directionalSpriteAnimator != null ? directionalSpriteAnimator.transform : null;
            if (resolvedVisual == null || resolvedVisual == impactVisual)
            {
                return;
            }

            impactVisual = resolvedVisual;
            impactBaseScale = impactVisual.localScale;
        }

        private void UpdateImpactFeedback()
        {
            if (impactVisual == null || impactRemaining <= 0f)
            {
                return;
            }

            impactRemaining = Mathf.Max(0f, impactRemaining - Time.deltaTime);
            float progress = 1f - impactRemaining / impactDuration;
            float verticalScale;

            if (progress < 0.28f)
            {
                verticalScale = Mathf.Lerp(1f, squashScale, progress / 0.28f);
            }
            else if (progress < 0.62f)
            {
                verticalScale = Mathf.Lerp(squashScale, reboundScale, (progress - 0.28f) / 0.34f);
            }
            else
            {
                verticalScale = Mathf.Lerp(reboundScale, 1f, (progress - 0.62f) / 0.38f);
            }

            impactVisual.localScale = new Vector3(
                impactBaseScale.x * (2f - verticalScale),
                impactBaseScale.y * verticalScale,
                impactBaseScale.z
            );

            if (impactRemaining <= 0f)
            {
                impactVisual.localScale = impactBaseScale;
            }
        }

#if UNITY_EDITOR
        public void Configure(TruckController controller, DirectionalSpriteAnimator spriteAnimator)
        {
            truckController = controller;
            directionalSpriteAnimator = spriteAnimator;
        }
#endif
    }
}
