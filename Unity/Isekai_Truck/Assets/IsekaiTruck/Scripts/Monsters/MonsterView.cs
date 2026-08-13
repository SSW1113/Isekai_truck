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
        [SerializeField] private bool disableRootMotion = true;
        [SerializeField] private string isFleeingParameter = "IsFleeing";
        [SerializeField] private string moveSpeedParameter = "MoveSpeed";

        private int isFleeingParameterId;
        private int moveSpeedParameterId;
        private bool hasIsFleeingParameter;
        private bool hasMoveSpeedParameter;

        public Transform VisualRoot => visualRoot;

        public void Initialize(Color color)
        {
            visualRoot = visualRoot == null ? transform : visualRoot;
            animator = animator == null ? GetComponentInChildren<Animator>(true) : animator;

            if (disableColliders)
            {
                DisableColliders();
            }

            if (applyDefinitionColor)
            {
                ApplyColor(color);
            }

            InitializeAnimator();
        }

        public void SetMovement(Vector3 direction, float moveSpeed, bool isFleeing)
        {
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
