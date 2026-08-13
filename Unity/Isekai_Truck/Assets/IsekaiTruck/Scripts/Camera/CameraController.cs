using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Camera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;

        private GameConfig.CameraSettings settings;
        private Transform target;
        private float currentZoomMultiplier = 1f;
        private int currentScreenWidth = -1;
        private int currentScreenHeight = -1;

        public UnityEngine.Camera TargetCamera => targetCamera;
        public Rect ViewportRect { get; private set; } = new Rect(0f, 0f, 1f, 1f);

        public void Initialize(GameConfig gameConfig, Transform followTarget)
        {
            settings = gameConfig.Camera;
            target = followTarget;

            if (targetCamera == null)
            {
                targetCamera = GetComponent<UnityEngine.Camera>();
            }

            targetCamera.fieldOfView = settings.FieldOfView;
            targetCamera.nearClipPlane = settings.NearClipPlane;
            targetCamera.farClipPlane = settings.FarClipPlane;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = gameConfig.World.FogColor;

            UpdateViewport();
            transform.position = settings.Offset;
            transform.LookAt(settings.LookTarget);
        }

        public float UpdateCamera()
        {
            UpdateViewport();

            float truckScale = target.localScale.x;
            float growth = Mathf.Max(0f, truckScale - settings.ZoomStartScale);
            float targetZoomMultiplier = Mathf.Min(1f + growth * settings.ZoomStrength, settings.MaxZoomMultiplier);

            currentZoomMultiplier += (targetZoomMultiplier - currentZoomMultiplier) * settings.FollowSpeed;

            Vector3 targetPosition = target.position + settings.Offset * currentZoomMultiplier;
            transform.position += (targetPosition - transform.position) * settings.FollowSpeed;

            return currentZoomMultiplier;
        }

        public static Rect CalculateViewportRect(float screenAspect, float targetAspect)
        {
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                return new Rect((1f - width) / 2f, 0f, width, 1f);
            }

            float height = screenAspect / targetAspect;
            return new Rect(0f, (1f - height) / 2f, 1f, height);
        }

        private void UpdateViewport()
        {
            if (Screen.width == currentScreenWidth && Screen.height == currentScreenHeight)
            {
                return;
            }

            currentScreenWidth = Mathf.Max(Screen.width, 1);
            currentScreenHeight = Mathf.Max(Screen.height, 1);

            float screenAspect = (float)currentScreenWidth / currentScreenHeight;
            ViewportRect = CalculateViewportRect(screenAspect, settings.ViewportAspect);
            targetCamera.rect = ViewportRect;
            targetCamera.aspect = settings.ViewportAspect;
        }
    }
}
