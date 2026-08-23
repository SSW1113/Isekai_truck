using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class CartoonButtonPressEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform pressTarget;
        [SerializeField] private RectTransform accentTarget;
        [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.04f;
        [SerializeField, Range(0.9f, 1f)] private float pressedScale = 0.97f;
        [SerializeField, Range(1f, 1.15f)] private float accentHoverScale = 1f;
        [SerializeField, Range(-15f, 15f)] private float accentHoverRotation;
        [SerializeField, Min(0f)] private float hoverOffset = 1.5f;
        [SerializeField, Min(1f)] private float transitionSpeed = 18f;

        private Button button;
        private Vector2 restingPosition;
        private Vector3 restingScale = Vector3.one;
        private Vector3 accentRestingScale = Vector3.one;
        private Quaternion accentRestingRotation = Quaternion.identity;
        private bool isHovered;
        private bool isPressed;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (pressTarget != null)
            {
                restingPosition = pressTarget.anchoredPosition;
                restingScale = pressTarget.localScale;
            }

            if (accentTarget != null)
            {
                accentRestingScale = accentTarget.localScale;
                accentRestingRotation = accentTarget.localRotation;
            }
        }

        private void Update()
        {
            if (pressTarget == null)
            {
                return;
            }

            bool canAnimate = button != null && button.interactable;
            float targetScale = !canAnimate ? 1f : isPressed ? pressedScale : isHovered ? hoverScale : 1f;
            float targetOffset = canAnimate && isHovered && !isPressed ? hoverOffset : 0f;
            float blend = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);

            pressTarget.localScale = Vector3.Lerp(pressTarget.localScale, restingScale * targetScale, blend);
            pressTarget.anchoredPosition = Vector2.Lerp(pressTarget.anchoredPosition, restingPosition + Vector2.up * targetOffset, blend);

            if (accentTarget != null)
            {
                float targetAccentScale = canAnimate && isHovered ? accentHoverScale : 1f;
                float targetRotation = canAnimate && isHovered ? accentHoverRotation : 0f;
                accentTarget.localScale = Vector3.Lerp(accentTarget.localScale, accentRestingScale * targetAccentScale, blend);
                accentTarget.localRotation = Quaternion.Lerp(
                    accentTarget.localRotation,
                    accentRestingRotation * Quaternion.Euler(0f, 0f, targetRotation),
                    blend
                );
            }
        }

        private void OnDisable()
        {
            isHovered = false;
            isPressed = false;
            ResetVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.interactable)
            {
                isPressed = true;
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            isPressed = false;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            isPressed = false;
        }

#if UNITY_EDITOR
        public void SetTarget(RectTransform target)
        {
            pressTarget = target;
            restingPosition = target != null ? target.anchoredPosition : Vector2.zero;
            restingScale = target != null ? target.localScale : Vector3.one;
        }

        public void Configure(
            RectTransform target,
            RectTransform accent,
            float targetHoverScale,
            float targetPressedScale,
            float targetHoverOffset,
            float targetAccentHoverScale = 1f,
            float targetAccentHoverRotation = 0f
        )
        {
            SetTarget(target);
            accentTarget = accent;
            hoverScale = targetHoverScale;
            pressedScale = targetPressedScale;
            hoverOffset = targetHoverOffset;
            accentHoverScale = targetAccentHoverScale;
            accentHoverRotation = targetAccentHoverRotation;

            if (accentTarget != null)
            {
                accentRestingScale = accentTarget.localScale;
                accentRestingRotation = accentTarget.localRotation;
            }
        }
#endif

        private void ResetVisuals()
        {
            if (pressTarget != null)
            {
                pressTarget.anchoredPosition = restingPosition;
                pressTarget.localScale = restingScale;
            }

            if (accentTarget != null)
            {
                accentTarget.localScale = accentRestingScale;
                accentTarget.localRotation = accentRestingRotation;
            }
        }
    }
}
