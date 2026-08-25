using UnityEngine;

namespace IsekaiTruck.Visuals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BillboardSpriteView : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;

        private float rollDegrees;

        private void Awake()
        {
            ResolveCamera();
        }

        private void LateUpdate()
        {
            UpdateFacing();
        }

        public void UpdateFacing()
        {
            ResolveCamera();
            if (targetCamera != null)
            {
                transform.rotation = targetCamera.transform.rotation * Quaternion.Euler(0f, 0f, rollDegrees);
            }
        }

        public void SetRoll(float degrees)
        {
            rollDegrees = degrees;
        }

        private void ResolveCamera()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }
        }

#if UNITY_EDITOR
        public void SetTargetCamera(UnityEngine.Camera cameraTarget)
        {
            targetCamera = cameraTarget;
        }
#endif
    }
}
