using UnityEngine;

namespace IsekaiTruck.World
{
    [DisallowMultipleComponent]
    public sealed class WorldSpriteDepthOrder : MonoBehaviour
    {
        private const float OrderPerWorldUnit = 100f;

        [SerializeField] private SpriteRenderer targetRenderer;
        [SerializeField] private int orderOffset;

        public void Refresh(UnityEngine.Camera targetCamera)
        {
            if (targetRenderer == null || targetCamera == null)
            {
                return;
            }

            Vector3 planarForward = Vector3.ProjectOnPlane(targetCamera.transform.forward, Vector3.up);
            if (planarForward.sqrMagnitude <= 0.0001f)
            {
                planarForward = Vector3.forward;
            }

            planarForward.Normalize();
            float depth = Vector3.Dot(transform.position - targetCamera.transform.position, planarForward);
            int depthOrder = -Mathf.RoundToInt(depth * OrderPerWorldUnit) + orderOffset;
            targetRenderer.sortingOrder = Mathf.Clamp(depthOrder, short.MinValue + 1, short.MaxValue - 1);
        }

#if UNITY_EDITOR
        public void Configure(SpriteRenderer rendererTarget, int offset)
        {
            targetRenderer = rendererTarget;
            orderOffset = offset;
        }
#endif
    }
}
