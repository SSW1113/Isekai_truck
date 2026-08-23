using IsekaiTruck.Visuals;
using UnityEngine;

namespace IsekaiTruck.Monsters
{
    [DisallowMultipleComponent]
    public sealed class MonsterView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Model")]
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool faceMoveDirection = true;
        [SerializeField] private bool applyDefinitionColor = true;
        [SerializeField] private bool disableColliders = true;

        [Header("Animation")]
        [SerializeField] private Animator animator;
        [SerializeField] private DirectionalSpriteAnimator directionalSpriteAnimator;
        [SerializeField] private bool disableRootMotion = true;
        [SerializeField] private string isFleeingParameter = "IsFleeing";
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";

        [Header("Defeat Feedback")]
        [SerializeField, Min(0.05f)] private float defeatDuration = 0.3f;
        [SerializeField, Min(0f)] private float knockbackDistance = 2.2f;
        [SerializeField, Min(0f)] private float jumpHeight = 0.7f;
        [SerializeField, Range(1f, 1.5f)] private float popScale = 1.15f;
        [SerializeField, Range(0f, 180f)] private float spinDegrees = 28f;

        private int isFleeingParameterId;
        private int moveSpeedParameterId;
        private bool hasIsFleeingParameter;
        private bool hasMoveSpeedParameter;
        private BillboardSpriteView billboardSpriteView;
        private SpriteRenderer[] spriteRenderers;
        private Color[] initialSpriteColors;
        private Vector3 defeatStartPosition;
        private Vector3 defeatDirection;
        private Vector3 defeatInitialScale;
        private Quaternion defeatInitialRotation;
        private float defeatElapsed;
        private bool isDefeating;

        public Transform VisualRoot => visualRoot;

        public void Initialize(Color color)
        {
            visualRoot = visualRoot == null ? transform : visualRoot;
            animator = animator == null ? GetComponentInChildren<Animator>(true) : animator;
            directionalSpriteAnimator = directionalSpriteAnimator == null ? GetComponentInChildren<DirectionalSpriteAnimator>(true) : directionalSpriteAnimator;

            if (disableColliders)
            {
                DisableColliders();
            }

            if (applyDefinitionColor)
            {
                ApplyColor(color);
            }

            InitializeAnimator();
            directionalSpriteAnimator?.Initialize();
            billboardSpriteView = visualRoot.GetComponentInChildren<BillboardSpriteView>(true);
            CacheSpriteColors();
            isDefeating = false;
            defeatElapsed = 0f;
        }

        private void Update()
        {
            if (!isDefeating)
            {
                return;
            }

            defeatElapsed = Mathf.Min(defeatElapsed + Time.deltaTime, defeatDuration);
            float progress = defeatDuration > 0f ? defeatElapsed / defeatDuration : 1f;
            float easedMovement = 1f - (1f - progress) * (1f - progress);
            float jump = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = defeatStartPosition + defeatDirection * (knockbackDistance * easedMovement) + Vector3.up * jump;

            float scale = EvaluateDefeatScale(progress);
            visualRoot.localScale = defeatInitialScale * scale;
            float roll = spinDegrees * easedMovement;
            if (billboardSpriteView != null)
            {
                billboardSpriteView.SetRoll(roll);
            }
            else
            {
                visualRoot.localRotation = defeatInitialRotation * Quaternion.Euler(0f, roll, roll * 0.35f);
            }

            float alpha = 1f - Mathf.InverseLerp(0.62f, 1f, progress);
            SetSpriteAlpha(alpha);
        }

        public void SetMovement(Vector3 direction, float moveSpeed, bool isFleeing)
        {
            directionalSpriteAnimator?.SetMovement(direction, moveSpeed);

            if (faceMoveDirection && direction.sqrMagnitude > 0.000001f)
            {
                visualRoot.rotation = Quaternion.LookRotation(direction, Vector3.up);
            }

            if (animator == null)
            {
                return;
            }

            if (hasIsFleeingParameter)
            {
                animator.SetBool(isFleeingParameterId, isFleeing);
            }

            if (hasMoveSpeedParameter)
            {
                animator.SetFloat(moveSpeedParameterId, moveSpeed);
            }
        }

        public void SetPaused(bool isPaused)
        {
            directionalSpriteAnimator?.SetPaused(isPaused);

            if (animator != null)
            {
                animator.speed = isPaused ? 0f : 1f;
            }
        }

        public float PlayDefeat(Vector3 direction)
        {
            if (isDefeating)
            {
                return Mathf.Max(0f, defeatDuration - defeatElapsed);
            }

            Vector3 planarDirection = Vector3.ProjectOnPlane(direction, Vector3.up);
            defeatDirection = planarDirection.sqrMagnitude > 0.000001f ? planarDirection.normalized : transform.forward;
            defeatStartPosition = transform.position;
            defeatInitialScale = visualRoot.localScale;
            defeatInitialRotation = visualRoot.localRotation;
            defeatElapsed = 0f;
            isDefeating = true;
            directionalSpriteAnimator?.SetMovement(Vector3.zero, 0f);
            directionalSpriteAnimator?.SetPaused(true);
            if (animator != null)
            {
                animator.speed = 0f;
            }

            return defeatDuration;
        }

        private void InitializeAnimator()
        {
            if (animator == null)
            {
                return;
            }

            if (disableRootMotion)
            {
                animator.applyRootMotion = false;
            }

            isFleeingParameterId = Animator.StringToHash(isFleeingParameter);
            moveSpeedParameterId = Animator.StringToHash(moveSpeedParameter);
            AnimatorControllerParameter[] parameters = animator.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.nameHash == isFleeingParameterId && parameter.type == AnimatorControllerParameterType.Bool)
                {
                    hasIsFleeingParameter = true;
                }

                if (parameter.nameHash == moveSpeedParameterId && parameter.type == AnimatorControllerParameterType.Float)
                {
                    hasMoveSpeedParameter = true;
                }
            }

            if (hasIsFleeingParameter)
            {
                animator.SetBool(isFleeingParameterId, false);
            }

            if (hasMoveSpeedParameter)
            {
                animator.SetFloat(moveSpeedParameterId, 0f);
            }
        }

        private void ApplyColor(Color color)
        {
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                Material sharedMaterial = targetRenderer.sharedMaterial;

                if (sharedMaterial == null)
                {
                    continue;
                }

                MaterialPropertyBlock properties = new MaterialPropertyBlock();
                targetRenderer.GetPropertyBlock(properties);

                if (sharedMaterial.HasProperty(BaseColorId))
                {
                    properties.SetColor(BaseColorId, color);
                }

                if (sharedMaterial.HasProperty(ColorId))
                {
                    properties.SetColor(ColorId, color);
                }

                targetRenderer.SetPropertyBlock(properties);
            }
        }

        private float EvaluateDefeatScale(float progress)
        {
            if (progress < 0.18f)
            {
                return Mathf.Lerp(1f, popScale, progress / 0.18f);
            }

            if (progress < 0.72f)
            {
                return Mathf.Lerp(popScale, 0.8f, (progress - 0.18f) / 0.54f);
            }

            return Mathf.Lerp(0.8f, 0f, (progress - 0.72f) / 0.28f);
        }

        private void CacheSpriteColors()
        {
            spriteRenderers = visualRoot.GetComponentsInChildren<SpriteRenderer>(true);
            initialSpriteColors = new Color[spriteRenderers.Length];
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                initialSpriteColors[i] = spriteRenderers[i].color;
            }
        }

        private void SetSpriteAlpha(float alpha)
        {
            for (int i = 0; i < spriteRenderers.Length; i++)
            {
                Color color = initialSpriteColors[i];
                color.a *= Mathf.Clamp01(alpha);
                spriteRenderers[i].color = color;
            }
        }

        private void DisableColliders()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }
        }

#if UNITY_EDITOR
        public void SetVisualRoot(Transform target)
        {
            visualRoot = target;
        }
#endif
    }
}
