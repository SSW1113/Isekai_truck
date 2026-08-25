using UnityEngine;

namespace IsekaiTruck.Visuals
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SpriteSequenceEffect : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private Sprite[] frames = System.Array.Empty<Sprite>();
        [SerializeField, Min(0f)] private float framesPerSecond = 12f;
        [SerializeField] private bool destroyOnComplete = true;

        private int frameIndex;
        private float frameTimer;
        private bool isComplete;

        public int FrameCount => frames?.Length ?? 0;
        public float FramesPerSecond => framesPerSecond;
        public float Duration => framesPerSecond > 0f ? FrameCount / framesPerSecond : 0f;
        public bool DestroyOnComplete => destroyOnComplete;

        private void Awake()
        {
            ResolveRenderer();
            ResetSequence();
        }

        private void OnEnable()
        {
            ResetSequence();
        }

        private void Update()
        {
            if (isComplete || framesPerSecond <= 0f || frames == null || frames.Length == 0)
            {
                return;
            }

            float frameDuration = 1f / framesPerSecond;
            frameTimer += Mathf.Max(0f, Time.deltaTime);
            if (frameTimer < frameDuration)
            {
                return;
            }

            int frameAdvance = Mathf.FloorToInt(frameTimer / frameDuration);
            frameTimer -= frameAdvance * frameDuration;
            int nextFrameIndex = frameIndex + frameAdvance;
            if (nextFrameIndex >= frames.Length)
            {
                frameIndex = frames.Length - 1;
                ApplySprite();
                Complete();
                return;
            }

            frameIndex = nextFrameIndex;
            ApplySprite();
        }

        private void ResolveRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponent<SpriteRenderer>();
            }
        }

        private void ResetSequence()
        {
            ResolveRenderer();
            frameIndex = 0;
            frameTimer = 0f;
            isComplete = false;
            ApplySprite();
        }

        private void ApplySprite()
        {
            if (spriteRenderer != null && frames != null && frames.Length > 0)
            {
                spriteRenderer.sprite = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
            }
        }

        private void Complete()
        {
            isComplete = true;
            if (!destroyOnComplete)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                DestroyImmediate(gameObject);
                return;
            }
#endif
            Destroy(gameObject);
        }

#if UNITY_EDITOR
        public void Configure(
            SpriteRenderer targetRenderer,
            Sprite[] animationFrames,
            float animationFramesPerSecond,
            bool shouldDestroyOnComplete)
        {
            spriteRenderer = targetRenderer;
            frames = animationFrames != null ? (Sprite[])animationFrames.Clone() : System.Array.Empty<Sprite>();
            framesPerSecond = Mathf.Max(0f, animationFramesPerSecond);
            destroyOnComplete = shouldDestroyOnComplete;
            ResetSequence();
        }
#endif
    }
}
