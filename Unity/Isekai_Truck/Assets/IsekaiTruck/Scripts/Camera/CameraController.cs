using IsekaiTruck.Config;
using UnityEngine;

namespace IsekaiTruck.Camera
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UnityEngine.Camera))]
    public sealed class CameraController : MonoBehaviour
    {
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("Damage Feedback")]
        [SerializeField, Min(0f)] private float damageShakeDuration = 0.12f;
        [SerializeField, Min(0f)] private float damageShakeAmplitude = 0.14f;

        private GameConfig.CameraSettings settings;
        private Transform target;
        private float referenceFrameRate;
        private float currentZoomMultiplier = 1f;
        private int currentScreenWidth = -1;
        private int currentScreenHeight = -1;
        private float blessingViewMultiplier = 1f;
        private Vector3 followPosition;
        private float damageShakeRemaining;

        public UnityEngine.Camera TargetCamera => targetCamera;
        public Rect ViewportRect { get; private set; } = new Rect(0f, 0f, 1f, 1f);

        public void Initialize(GameConfig gameConfig, Transform followTarget)
        {
            settings = gameConfig.Camera;
            referenceFrameRate = gameConfig.ReferenceFrameRate;
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
            followPosition = transform.position;
            damageShakeRemaining = 0f;
        }

        public float UpdateCamera(float deltaTime)
        {
            UpdateViewport();

            float truckScale = target.localScale.x;
            float growth = Mathf.Max(0f, truckScale - settings.ZoomStartScale);
            float targetZoomMultiplier = Mathf.Min(1f + growth * settings.ZoomStrength, settings.MaxZoomMultiplier);
            targetZoomMultiplier = Mathf.Min(targetZoomMultiplier * blessingViewMultiplier, settings.MaxZoomMultiplier);
            float frameScale = Mathf.Max(deltaTime, 0f) * referenceFrameRate;
            float followFactor = GetFrameAdjustedFactor(settings.FollowSpeed, frameScale);

            currentZoomMultiplier += (targetZoomMultiplier - currentZoomMultiplier) * followFactor;

            Vector3 targetPosition = target.position + settings.Offset * currentZoomMultiplier;
            followPosition += (targetPosition - followPosition) * followFactor;
            transform.position = followPosition + CalculateDamageShakeOffset();

            return currentZoomMultiplier;
        }

        public void PlayDamageShake()
        {
            damageShakeRemaining = Mathf.Max(damageShakeRemaining, damageShakeDuration);
        }

        public void SetBlessingViewMultiplier(float viewMultiplier)
        {
            blessingViewMultiplier = Mathf.Max(1f, viewMultiplier);
        }

        public void RefreshViewport()
        {
            UpdateViewport();
        }

        private Vector3 CalculateDamageShakeOffset()
        {
            if (damageShakeRemaining <= 0f || damageShakeDuration <= 0f || damageShakeAmplitude <= 0f)
            {
                damageShakeRemaining = 0f;
                return Vector3.zero;
            }

            damageShakeRemaining = Mathf.Max(0f, damageShakeRemaining - Time.unscaledDeltaTime);
            float elapsed = damageShakeDuration - damageShakeRemaining;
            float strength = damageShakeRemaining / damageShakeDuration;
            float scaledAmplitude = damageShakeAmplitude * Mathf.Max(1f, currentZoomMultiplier);
            float horizontal = Mathf.Sin(elapsed * 115f) * scaledAmplitude * strength;
            float vertical = Mathf.Sin(elapsed * 157f) * scaledAmplitude * 0.35f * strength;
            return transform.right * horizontal + transform.up * vertical;
        }

        private static float GetFrameAdjustedFactor(float perFrameFactor, float frameScale)
        {
            if (perFrameFactor <= 0f || frameScale <= 0f)
            {
                return 0f;
            }

            if (perFrameFactor >= 1f)
            {
                return 1f;
            }

            return 1f - Mathf.Pow(1f - perFrameFactor, frameScale);
        }

        public static Rect CalculateViewportRect(float screenAspect, float targetAspect)
        {
            return CalculateViewportRect(screenAspect, targetAspect, 0.5f);
        }

        public static Rect CalculateViewportRect(float screenAspect, float targetAspect, float horizontalCenter)
        {
            if (screenAspect > targetAspect)
            {
                float width = targetAspect / screenAspect;
                float x = Mathf.Clamp(horizontalCenter - width * 0.5f, 0f, 1f - width);
                return new Rect(x, 0f, width, 1f);
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
            ViewportRect = CalculateViewportRect(screenAspect, settings.ViewportAspect, settings.ViewportHorizontalCenter);
            targetCamera.rect = ViewportRect;
            targetCamera.aspect = settings.ViewportAspect;
        }
    }
}
