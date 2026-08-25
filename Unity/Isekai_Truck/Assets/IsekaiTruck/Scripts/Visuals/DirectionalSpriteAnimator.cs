using UnityEngine;

namespace IsekaiTruck.Visuals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class DirectionalSpriteAnimator : MonoBehaviour
    {
        public const int DirectionCount = 8;
        public const int SourceDirectionCount = 5;
        public const int DefaultFramesPerDirection = 12;
        public const int ExpectedFrameCount = SourceDirectionCount * DefaultFramesPerDirection;

        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] directionFrames = new Sprite[ExpectedFrameCount];
        [SerializeField, Min(1)] private int framesPerDirection = DefaultFramesPerDirection;
        [SerializeField, Min(0f)] private float framesPerSecond = 12f;
        [SerializeField] private UnityEngine.Camera targetCamera;

        private int directionIndex;
        private int frameIndex;
        private float frameTimer;
        private bool isMoving;
        private bool isPaused;

        private void Awake()
        {
            ResolveReferences();
            ApplySprite();
        }

        private void OnEnable()
        {
            ResetAnimation();
        }

        private void LateUpdate()
        {
            if (isPaused || !isMoving || framesPerSecond <= 0f)
            {
                return;
            }

            float frameDuration = 1f / framesPerSecond;
            frameTimer += Time.deltaTime;
            if (frameTimer < frameDuration)
            {
                return;
            }

            int frameAdvance = Mathf.FloorToInt(frameTimer / frameDuration);
            frameTimer -= frameAdvance * frameDuration;
            frameIndex = (frameIndex + frameAdvance) % framesPerDirection;
            ApplySprite();
        }

        public void Initialize()
        {
            ResolveReferences();
            ResetAnimation();
        }

        public void SetMovement(Vector3 worldDirection, float moveSpeed)
        {
            ResolveReferences();

            bool shouldMove = moveSpeed > 0.0001f && worldDirection.sqrMagnitude > 0.000001f;
            if (worldDirection.sqrMagnitude > 0.000001f)
            {
                int nextDirectionIndex = ResolveDirectionIndex(worldDirection);
                if (nextDirectionIndex != directionIndex)
                {
                    directionIndex = nextDirectionIndex;
                    ApplySprite();
                }
            }

            if (isMoving == shouldMove)
            {
                return;
            }

            isMoving = shouldMove;
            if (!isMoving)
            {
                frameIndex = 0;
                frameTimer = 0f;
                ApplySprite();
            }
        }

        public void SetPaused(bool shouldPause)
        {
            isPaused = shouldPause;
        }

        private int ResolveDirectionIndex(Vector3 worldDirection)
        {
            Vector3 planarDirection = Vector3.ProjectOnPlane(worldDirection, Vector3.up).normalized;
            Vector3 screenRight = Vector3.right;
            Vector3 screenUp = Vector3.forward;

            if (targetCamera != null)
            {
                Vector3 cameraRight = Vector3.ProjectOnPlane(targetCamera.transform.right, Vector3.up);
                Vector3 cameraUp = Vector3.ProjectOnPlane(targetCamera.transform.up, Vector3.up);

                if (cameraRight.sqrMagnitude > 0.000001f)
                {
                    screenRight = cameraRight.normalized;
                }

                if (cameraUp.sqrMagnitude > 0.000001f)
                {
                    screenUp = cameraUp.normalized;
                }
            }

            float screenX = Vector3.Dot(planarDirection, screenRight);
            float screenY = Vector3.Dot(planarDirection, screenUp);
            float angle = Mathf.Atan2(screenX, -screenY) * Mathf.Rad2Deg;
            if (angle < 0f)
            {
                angle += 360f;
            }

            return Mathf.RoundToInt(angle / 45f) % DirectionCount;
        }

        private void ResolveSourceDirection(out int sourceDirectionIndex, out bool shouldFlipX)
        {
            if (directionIndex <= 4)
            {
                sourceDirectionIndex = directionIndex;
                shouldFlipX = false;
                return;
            }

            sourceDirectionIndex = DirectionCount - directionIndex;
            shouldFlipX = true;
        }

        private void ResolveReferences()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }

            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }
        }

        private void ResetAnimation()
        {
            directionIndex = 0;
            frameIndex = 0;
            frameTimer = 0f;
            isMoving = false;
            isPaused = false;
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (spriteRenderer == null || framesPerDirection <= 0 || directionFrames == null || directionFrames.Length != SourceDirectionCount * framesPerDirection)
            {
                return;
            }

            ResolveSourceDirection(out int sourceDirectionIndex, out bool shouldFlipX);
            Sprite sprite = directionFrames[sourceDirectionIndex * framesPerDirection + frameIndex];
            if (sprite != null && spriteRenderer.sprite != sprite)
            {
                spriteRenderer.sprite = sprite;
            }

            spriteRenderer.flipX = shouldFlipX;
        }

#if UNITY_EDITOR
        public void Configure(SpriteRenderer targetRenderer, Sprite[] frames, float animationFramesPerSecond)
        {
            Configure(targetRenderer, frames, DefaultFramesPerDirection, animationFramesPerSecond);
        }

        public void Configure(SpriteRenderer targetRenderer, Sprite[] frames, int animationFramesPerDirection, float animationFramesPerSecond)
        {
            spriteRenderer = targetRenderer;
            framesPerDirection = Mathf.Max(1, animationFramesPerDirection);
            directionFrames = frames != null ? (Sprite[])frames.Clone() : new Sprite[SourceDirectionCount * framesPerDirection];
            framesPerSecond = Mathf.Max(0f, animationFramesPerSecond);
            ResetAnimation();
        }

        public void SetTargetCamera(UnityEngine.Camera cameraTarget)
        {
            targetCamera = cameraTarget;
        }
#endif
    }
}
