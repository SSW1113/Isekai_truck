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

        private Vector3 previousPosition;

        private void Awake()
        {
            ResolveReferences();
            previousPosition = transform.position;
            directionalSpriteAnimator?.Initialize();
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
            previousPosition = currentPosition;
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

#if UNITY_EDITOR
        public void Configure(TruckController controller, DirectionalSpriteAnimator spriteAnimator)
        {
            truckController = controller;
            directionalSpriteAnimator = spriteAnimator;
        }
#endif
    }
}
